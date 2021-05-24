using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace JL.Splitting
{    /// <summary>
     /// Splits animated meshes using an infinite plane
     /// </summary>
    public class MeshSplitterSkinned : MeshSplitterBase
    {
        protected List<int> bonesPos, bonesNeg;
        protected List<Transform> bonesCut;

        protected float rootBoneDot;

        protected Transform rootBone;
        protected Transform[] originalBones;
#if ENABLE_MONO
        protected Vector4[] bonesMatrixRows;
#else
        protected Matrix4x4[] bonesLocalToWorldMatrices;
#endif
        /// <summary>
        /// Property to easily access which bones were cut after a split.
        /// </summary>
        public List<Transform> BonesCut => bonesCut;

        public MeshSplitterSkinned() : base() { }

        /// <param name="capUVMin">Scales the cap's UV coordinates to ensure no value is smaller.</param>
        /// <param name="capUVMax">Scales the cap's UV coordinates to ensure no value is greater.</param>
        public MeshSplitterSkinned(Vector2 capUVMin, Vector2 capUVMax)
            : base(capUVMin, capUVMax) { }

        #region Mesh Splitting Functions
        /// <summary>
        /// Splits a skinned mesh by an infinite plane.
        /// </summary>
        /// <param name="meshRenderer">The SkinnedMeshRenderer which references the mesh that needs to be split.</param>
        /// <param name="slicePlane">The plane used to split the mesh.</param>
        /// <returns>MeshPlitData that can be used to construct the resulting meshes. Returns null if the mesh was not split.</returns>
        public MeshSplitData SplitSkinnedMesh(SkinnedMeshRenderer meshRenderer, PointPlane slicePlane)
        {
            Mesh targetMesh = meshRenderer.sharedMesh;
            if (targetMesh == null)
                throw new Exception($"MeshFilter of object \"{meshRenderer.gameObject}\" does not have a mesh.");
            if (meshRenderer.sharedMesh.isReadable == false)
                throw new Exception($"Mesh \"{meshRenderer.sharedMesh.name}\" is not readable. Read/Write must be enabled in the import settings to perform splitting.");

            if (meshesBeingSplit.Contains(targetMesh)) return null;

            AllocateMemorySkinned(meshRenderer.transform, meshRenderer, slicePlane);

            CalculateWorldPositionsSkinned();
            CalculateBoneSides();
            CalculateSides();
            CheckForIntersections();

            //All triangles are on the same side of the plane, meaning the mesh was not split.
            if (trisPos.Count == 0 || trisNeg.Count == 0)
                return null;

            Vector3 normal = Quaternion.Inverse(ownRotation) * plane.normal;
            CreateCaps(newEdges, normal);

            MeshData posData = CreateMeshData(trisPos.RawArray, subMeshesPos);
            MeshData negData = CreateMeshData(trisNeg.RawArray, subMeshesNeg);

            return new MeshSplitData(posData, negData);
        }
        /// <summary>
        /// Splits a skinned mesh by an infinite plane asynchronous.
        /// </summary>
        /// <param name="meshRenderer">The SkinnedMeshRenderer which references the mesh that needs to be split.</param>
        /// <param name="slicePlane">The plane used to split the mesh.</param>
        /// <param name="callback">Fired on the main thread when the plitting is finished. The MeshSplitData will be null if the mesh was not split.</param>
        public void SplitSkinnedMeshAsync(SkinnedMeshRenderer meshRenderer, PointPlane slicePlane, Action<MeshSplitData> callback = null)
        {
            Mesh targetMesh = meshRenderer.sharedMesh;
            if (targetMesh == null)
                throw new Exception($"MeshFilter of object \"{meshRenderer.gameObject}\" does not have a mesh.");
            if (meshRenderer.sharedMesh.isReadable == false)
                throw new Exception($"Mesh \"{meshRenderer.sharedMesh.name}\" is not readable. Read/Write must be enabled in the import settings to perform splitting.");

            if (meshesBeingSplit.Contains(targetMesh)) return;
            meshesBeingSplit.Add(targetMesh);

            AllocateMemorySkinned(meshRenderer.transform, meshRenderer, slicePlane);
            CalculateBoneSides(); //can't be done async because of get_position;

            var syncContext = SynchronizationContext.Current;
            Task splitTask = Task.Run(SplitAsync);

            void SplitAsync()
            {
                try
                {
                    CalculateWorldPositionsSkinned();
                    CalculateSides();
                    CheckForIntersections();

                    //All triangles are on the same side of the plane, meaning the mesh was not split.
                    if (trisPos.Count == 0 || trisNeg.Count == 0)
                    {
                        syncContext.Post(_ => callback?.Invoke(null), null);
                        return;
                    }

                    Vector3 normal = Quaternion.Inverse(ownRotation) * plane.normal;
                    CreateCaps(newEdges, normal);

                    MeshData posData = CreateMeshData(trisPos.RawArray, subMeshesPos);
                    MeshData negData = CreateMeshData(trisNeg.RawArray, subMeshesNeg);

                    syncContext.Post(_ => callback?.Invoke(new MeshSplitData(posData, negData)), null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    meshesBeingSplit.Remove(targetMesh);
                }
            }
        }
        /// <summary>
        /// Allocates the memory needed to perform the splitting of a skinned mesh.
        /// </summary>
        protected void AllocateMemorySkinned(Transform meshTransform, SkinnedMeshRenderer skinnedMeshRenderer, PointPlane slicePlane)
        {
            originalBones = skinnedMeshRenderer.bones;
            rootBone = skinnedMeshRenderer.rootBone;

#if ENABLE_MONO
            bonesMatrixRows = new Vector4[originalBones.Length * 4];
            for (int i = 0, counter = 0; i < originalBones.Length; i++)
            {
                for (int j = 0; j < 4; j++, counter++)
                {
                    bonesMatrixRows[counter] = originalBones[i].localToWorldMatrix.GetRow(j);
                }
            }
#else
            bonesLocalToWorldMatrices = new Matrix4x4[originalBones.Length];
            for (int i = 0; i < originalBones.Length; i++)
            {
                bonesLocalToWorldMatrices[i] = originalBones[i].localToWorldMatrix;
            }
#endif

            bonesPos = new List<int>(originalBones.Length);
            bonesNeg = new List<int>(originalBones.Length);
            bonesCut = new List<Transform>(originalBones.Length / 4);

            AllocateMemory(meshTransform, skinnedMeshRenderer.sharedMesh, slicePlane);
        }
        /// <summary>
        /// Calculates the world position for each vertex of the target mesh.
        /// </summary>
        private void CalculateWorldPositionsSkinned()
        {
            Matrix4x4[] boneToWorldMatrices = GetBoneToWorldMatrices();

            for (int i = 0; i < originalVerticesCount; i++)
            {
                if (originalBoneWeights[i].weight0 > 0)
                    worldSpaceVertices[i] = boneToWorldMatrices[originalBoneWeights[i].boneIndex0].MultiplyPoint3x4(originalVertices[i]) * originalBoneWeights[i].weight0;
                if (originalBoneWeights[i].weight1 > 0)
                    worldSpaceVertices[i] += boneToWorldMatrices[originalBoneWeights[i].boneIndex1].MultiplyPoint3x4(originalVertices[i]) * originalBoneWeights[i].weight1;
                if (originalBoneWeights[i].weight2 > 0)
                    worldSpaceVertices[i] += boneToWorldMatrices[originalBoneWeights[i].boneIndex2].MultiplyPoint3x4(originalVertices[i]) * originalBoneWeights[i].weight2;
                if (originalBoneWeights[i].weight3 > 0)
                    worldSpaceVertices[i] += boneToWorldMatrices[originalBoneWeights[i].boneIndex3].MultiplyPoint3x4(originalVertices[i]) * originalBoneWeights[i].weight3;
            }

        }
        /// <summary>
        /// Calculates matrices which transform vertices from local space to world space.
        /// </summary>
        /// <returns></returns>
        private Matrix4x4[] GetBoneToWorldMatrices()
        {
            int boneCount = originalBones.Length;
            Matrix4x4[] boneToWorldMatrices = new Matrix4x4[boneCount];
#if ENABLE_MONO
            //Unity's matrix multiplication is very slow in Mono. Wrote it out because of this.
            Vector4[] bindposeMatrix = new Vector4[4]; //matrix as an array of verctor4's
            float[][] boneToWorld = new float[4][] { new float[4], new float[4], new float[4], new float[4] };

            int counter = 0;
            for (int i = 0; i < boneCount; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    bindposeMatrix[j] = originalBindPoses[i].GetRow(j);
                }

                for (int y = 0; y < 4; y++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        boneToWorld[j][y] = 0;
                        for (int k = 0; k < 4; k++)
                        {
                            boneToWorld[j][y] += bonesMatrixRows[counter][k] * bindposeMatrix[k][j];
                        }
                    }
                    counter++;
                }

                boneToWorldMatrices[i] = new Matrix4x4(new Vector4(boneToWorld[0][0], boneToWorld[0][1], boneToWorld[0][2], boneToWorld[0][3]),
                    new Vector4(boneToWorld[1][0], boneToWorld[1][1], boneToWorld[1][2], boneToWorld[1][3]),
                    new Vector4(boneToWorld[2][0], boneToWorld[2][1], boneToWorld[2][2], boneToWorld[2][3]),
                    new Vector4(boneToWorld[3][0], boneToWorld[3][1], boneToWorld[3][2], boneToWorld[3][3]));
            }
#else
        for (int i = 0; i < boneCount; i++)
        {
            boneToWorldMatrices[i] = bonesLocalToWorldMatrices[i] * originalBindPoses[i];
        }
#endif
            return boneToWorldMatrices;
        }
        /// <summary>
        /// Calculates on which side of the plane each bone of the target MeshRenderer lies.
        /// </summary>
        private void CalculateBoneSides()
        {
            rootBoneDot = plane.normal.x * (rootBone.position.x - plane.point.x) +
                        plane.normal.y * (rootBone.position.y - plane.point.y) +
                        plane.normal.z * (rootBone.position.z - plane.point.z);

            for (int i = 0; i < originalBones.Length; i++)
            {
                bool boneOnPosSide = plane.normal.x * (originalBones[i].position.x - plane.point.x) +
                    plane.normal.y * (originalBones[i].position.y - plane.point.y) +
                    plane.normal.z * (originalBones[i].position.z - plane.point.z) >= 0;

                if (boneOnPosSide)
                    bonesPos.Add(i);
                else
                    bonesNeg.Add(i);

                //check if bone is cut
                if (originalBones[i].childCount > 0)
                {
                    Transform child = originalBones[i].GetChild(0);

                    bool childOnPosSide = plane.normal.x * (child.position.x - plane.point.x) +
                        plane.normal.y * (child.position.y - plane.point.y) +
                        plane.normal.z * (child.position.z - plane.point.z) >= 0;

                    if (boneOnPosSide != childOnPosSide)
                    {
                        bonesCut.Add(originalBones[i]);
                    }
                }
            }
        }
        #endregion

        #region Helper Functions
        //NOTE: These helper functions were created for the MeshSplitter/Splittable workflow. 
        //They use the result of the splitting algorithm to create new objects from the original ones.

        /// <summary>
        /// Copies the original target gameobject and replaces the meshes of both MeshRenderers with the result of a split.
        /// </summary>
        /// <param name="rootObject">The object which will be cloned.</param>
        /// <param name="meshSplitData">The data calculated be a splitting sequence.</param>
        /// <param name="capMaterial">The material that is applied to the cap created by the plane splitting. </param>
        /// <param name="destroyOriginalMeshWhenSplit">When a mesh is created through code it should be destroyed to avoid memory leaks. </param>
        /// <param name="splitForce">An optional force which moves the resulting objects away from the splitting plane.</param>
        /// <param name="ragdollBones">If any of these bones are cut, the resulting objects will turn into ragdolls.</param>
        /// <returns>The split root objects a well as the objects holding the new meshes.</returns>
        public SplitResult CreateSplitObjectsSkinned(GameObject rootObject, MeshSplitData meshSplitData,
            Material capMaterial, bool destroyOriginalMeshWhenSplit, float splitForce = 0, Transform[] ragdollBones = null)
        {
            GameObject posObject, negObject;

            if (ragdollBones != null && AnyBoneCut(ragdollBones))
            {
                posObject = GameObject.Instantiate(rootObject, rootObject.transform.parent);
                negObject = rootObject;

                EnableRagdoll(posObject);
                EnableRagdoll(negObject);
            }
            else
            {
                if (rootBoneDot < 0)
                {
                    posObject = GameObject.Instantiate(rootObject, rootObject.transform.parent);
                    negObject = rootObject;

                    EnableRagdoll(posObject);
                }
                else
                {
                    posObject = rootObject;
                    negObject = GameObject.Instantiate(rootObject, rootObject.transform.parent);

                    EnableRagdoll(negObject);
                }
            }

            SkinnedMeshRenderer renderer0 = posObject.GetComponentInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer renderer1 = negObject.GetComponentInChildren<SkinnedMeshRenderer>();

            RemoveUnusedMaterials(renderer0, meshSplitData.posMeshData.unfilteredSubmeshes, capMaterial);
            RemoveUnusedMaterials(renderer1, meshSplitData.negMeshData.unfilteredSubmeshes, capMaterial);

            renderer0.sharedMesh = meshSplitData.posMeshData.CreateMesh(originalMesh.name);
            renderer1.sharedMesh = meshSplitData.negMeshData.CreateMesh(originalMesh.name);

            DisableCutBonesCollisions(renderer0.bones, bonesNeg);
            DisableCutBonesCollisions(renderer1.bones, bonesPos);

            TryGeneratePhysics(posObject, negObject, meshSplitData.posMeshData.Mesh, meshSplitData.negMeshData.Mesh, splitForce);

            //This is done to prevent memory leaks.
            //It's ok to destroy these meshes next time they are split, assuming these MeshRenderers will be the only ones using them.
            posObject.GetComponent<SplittableSkinned>().DestroyOriginalMeshWhenSplit = true;
            negObject.GetComponent<SplittableSkinned>().DestroyOriginalMeshWhenSplit = true;

            if (destroyOriginalMeshWhenSplit)
            {
                GameObject.Destroy(originalMesh);
            }

            return new SplitResult(posObject, negObject, renderer0.gameObject, renderer1.gameObject);
        }

        /// <summary>
        /// Turns an animated object into a ragdoll.
        /// </summary>
        /// <param name="targetObject"></param>
        private void EnableRagdoll(GameObject targetObject)
        {
            GameObject.Destroy(targetObject.GetComponent<Collider>());
            GameObject.Destroy(targetObject.GetComponent<Rigidbody>());

            if (targetObject.TryGetComponent(out Animator animator)) animator.enabled = false;
            targetObject.layer = 0;

            EnableRagdollRecursive(targetObject.transform);
        }

        private void EnableRagdollRecursive(Transform targetTransform)
        {
            int childCount = targetTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = targetTransform.GetChild(i);
                EnableRagdollRecursive(child);

                if (child.TryGetComponent(out Rigidbody rigidbody))
                {
                    rigidbody.isKinematic = false;
                    rigidbody.useGravity = true;
                }
            }
        }

        private void DisableCutBonesCollisions(Transform[] boneArray, List<int> indexArray)
        {
            for (int i = 0; i < indexArray.Count; i++)
            {
                Transform bone = boneArray[indexArray[i]];
                if (bone.TryGetComponent(out Collider collider))
                    collider.enabled = false;
            }
        }

        /// <summary>
        /// Checks if any of the provided bones intersects with the splitting plane.
        /// </summary>
        /// <param name="ragdollBones">Array of bones to check.</param>
        /// <returns></returns>
        public bool AnyBoneCut(Transform[] ragdollBones)
        {
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                if (bonesCut.Contains(ragdollBones[i]))
                    return true;
            }

            return false;
        }

        #endregion
    }

}