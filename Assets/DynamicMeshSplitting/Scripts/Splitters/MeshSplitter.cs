using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace JL.Splitting
{
    /// <summary>
    /// Splits non-animated meshes using an infinite plane
    /// </summary>
    public class MeshSplitter : MeshSplitterBase
    {
        public MeshSplitter() : base() { }

        /// <param name="capUVMin">Scales the cap's UV coordinates to ensure no value is smaller.</param>
        /// <param name="capUVMax">Scales the cap's UV coordinates to ensure no value is greater.</param>
        public MeshSplitter(Vector2 capUVMin, Vector2 capUVMax)
        : base(capUVMin, capUVMax) { }

        #region Mesh Splitting Functions
        /// <summary>
        /// Splits a non-skinned mesh by an infinite plane.
        /// </summary>
        /// <param name="meshFilter">The MeshFilter which references the mesh that needs to be split.</param>
        /// <param name="slicePlane">The plane used to split the mesh.</param>
        /// <returns>MeshPlitData that can be used to construct the resulting meshes. Returns null if the mesh was not split.</returns>
        public MeshSplitData SplitMesh(MeshFilter meshFilter, PointPlane slicePlane)
        {
            Mesh targetMesh = meshFilter.sharedMesh;
            if (targetMesh == null)
                throw new Exception($"MeshFilter of object \"{meshFilter.gameObject}\" does not have a mesh.");
            if (targetMesh.isReadable == false)
                throw new Exception($"Mesh \"{targetMesh.name}\" is not readable. Read/Write must be enabled in the import settings to perform splitting.");

            if (meshesBeingSplit.Contains(targetMesh)) return null;

            AllocateMemory(meshFilter.transform, meshFilter.sharedMesh, slicePlane);

            CalculateWorldPositions();
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
        /// Splits a mesh by an infinite plane asynchronous.
        /// </summary>
        /// <param name="meshFilter">The MeshFilter which references the mesh that needs to be split.</param>
        /// <param name="slicePlane">The plane used to split the mesh.</param>
        /// <param name="callback">Fired on the main thread when the plitting is finished. The MeshSplitData will be null if the mesh was not split.</param>
        public void SplitMeshAsync(MeshFilter meshFilter, PointPlane slicePlane, Action<MeshSplitData> callback = null)
        {
            Mesh targetMesh = meshFilter.sharedMesh;
            if (targetMesh == null)
                throw new Exception($"MeshFilter of object \"{meshFilter.gameObject}\" does not have a mesh.");
            if (targetMesh.isReadable == false)
                throw new Exception($"Mesh \"{targetMesh.name}\" is not readable. Read/Write must be enabled in the import settings to perform splitting.");

            if (meshesBeingSplit.Contains(targetMesh)) return;
            meshesBeingSplit.Add(targetMesh);

            AllocateMemory(meshFilter.transform, meshFilter.sharedMesh, slicePlane);

            var syncContext = SynchronizationContext.Current;
            Task splitTask = Task.Run(SplitAsync);

            void SplitAsync()
            {
                try
                {
                    CalculateWorldPositions();
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
        /// Calculates the world position for each vertex of the target mesh.
        /// </summary>
        private void CalculateWorldPositions()
        {
            for (int i = 0; i < originalVerticesCount; i++)
            {
                worldSpaceVertices[i] = localToWorldMatrix.MultiplyPoint3x4(originalVertices[i]);
            }
        }
        #endregion

        #region Helper Functions
        //NOTE: These helper functions were created for the MeshSplitter/Splittable workflow. 
        //They use the result of the splitting algorithm to create new objects from the original ones.

        /// <summary>
        /// Copies the original target gameobject and replaces the meshes of both objects with the result of a split.
        /// </summary>
        /// <param name="rootObject">The object which will be cloned.</param>
        /// <param name="meshSplitData">The data calculated be a splitting sequence.</param>
        /// <param name="capMaterial">The material that is applied to the cap created by the plane splitting. </param>
        /// <param name="splitForce">An optional force which moves the resulting objects away from the splitting plane.</param>
        /// <returns>The split root objects a well as the objects holding the new meshes.</returns>
        public SplitResult CreateSplitObjects(GameObject rootObject, MeshSplitData meshSplitData, Material capMaterial, 
            bool destroyOriginalMeshWhenSplit, float splitForce = 0)
        {
            GameObject posObject = GameObject.Instantiate(rootObject, rootObject.transform.parent);
            GameObject negObject = rootObject;

            MeshFilter posMeshFilter = posObject.GetComponentInChildren<MeshFilter>();
            MeshFilter negMeshFilter = negObject.GetComponentInChildren<MeshFilter>();
            posMeshFilter.sharedMesh = meshSplitData.posMeshData.CreateMesh(originalMesh.name);
            negMeshFilter.sharedMesh = meshSplitData.negMeshData.CreateMesh(originalMesh.name);

            GameObject.Destroy(posMeshFilter.GetComponent<Collider>());
            GameObject.Destroy(negMeshFilter.GetComponent<Collider>());

            Renderer renderer0 = posMeshFilter.GetComponent<Renderer>();
            Renderer renderer1 = negMeshFilter.GetComponent<Renderer>();

            RemoveUnusedMaterials(renderer0, meshSplitData.posMeshData.unfilteredSubmeshes, capMaterial);
            RemoveUnusedMaterials(renderer1, meshSplitData.negMeshData.unfilteredSubmeshes, capMaterial);

            TryGeneratePhysics(posObject, negObject, meshSplitData.posMeshData.Mesh, meshSplitData.negMeshData.Mesh, splitForce);

            //This is done to prevent memory leaks.
            //It's ok to destroy these meshes next time they are split, assuming these MeshFilters will be the only ones using them.
            posObject.GetComponent<Splittable>().DestroyOriginalMeshWhenSplit = true;
            negObject.GetComponent<Splittable>().DestroyOriginalMeshWhenSplit = true;

            if (destroyOriginalMeshWhenSplit)
            {
                GameObject.Destroy(originalMesh);
            }

            return new SplitResult(posObject, negObject, posMeshFilter.gameObject, negMeshFilter.gameObject);
        }

        /// <summary>
        /// Adds a meshCollider to a gameobject.
        /// </summary>
        /// <param name="go">The GameObject the MeshCollider should be added to.</param>
        /// <param name="mesh">The Mesh the MeshCollider will be referencing.</param>
        public void GenerateMeshCollider(GameObject go, Mesh mesh)
        {
            Physics.BakeMesh(mesh.GetInstanceID(), true);
            AddMeshCollider(go, mesh);
        }

        /// <summary>
        /// Adds a meshCollider to a gameobject asynchronous.
        /// </summary>
        /// <param name="go">The GameObject the MeshCollider should be added to.</param>
        /// <param name="mesh">The Mesh the MeshCollider will be referencing.</param>
        public void GenerateMeshColliderAsync(GameObject go, Mesh mesh)
        {
            int instanceID = mesh.GetInstanceID();
            var syncContext = SynchronizationContext.Current;
            Task bakeTask = Task.Run(BakeAsync);

            void BakeAsync()
            {
                try
                {
                    Physics.BakeMesh(instanceID, true);
                    syncContext.Post(_ =>
                    {
                        AddMeshCollider(go, mesh);
                    }, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void AddMeshCollider(GameObject go, Mesh mesh)
        {
            //Unity automatically uses the mesh from the MeshFilter component when adding a MeshCollider.
            //Temporary setting the sharedMesh to null avoids double baking of the mesh data.
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            Mesh tempMesh = meshFilter.sharedMesh;
            meshFilter.sharedMesh = null;

            MeshCollider newCollider = go.AddComponent<MeshCollider>();
            newCollider.convex = true;
            newCollider.sharedMesh = mesh;

            meshFilter.sharedMesh = tempMesh;
        }
        #endregion

    }
}