using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    public class SplittableSkinned : SplittableBase
    {
        [Tooltip("The SkinnedMeshRenderer which contains the mesh that will be split.\n" +
    "The targetSkinnedMeshRenderer should be in the hierarchy of the GameObject to which this component is attached.")]
        public SkinnedMeshRenderer targetSkinnedMeshRenderer = null;
        [Tooltip("If any of these bones are cut by the splitting plane, the resulting objects will turn into ragdolls.")] 
        public Transform[] RagdollBones = null;

        private MeshSplitterSkinned _meshSplitterSkinned;
        private void Awake()
        {
            _meshSplitterSkinned = new MeshSplitterSkinned(CapUVMin, CapUVMax);
        }

        /// <summary>
        /// Splits the targetSkinnedMeshRenderer's mesh and duplicates this component's GameObject to make it appear as if it was split.
        /// </summary>
        /// <param name="plane">The plane along which the target mesh is split.</param>
        /// <returns>A SplitResult containing the resulting objects. For more info, see SplitResult.</returns>
        public override SplitResult Split(PointPlane plane)
        {
            MeshSplitData meshSplitData = _meshSplitterSkinned.SplitSkinnedMesh(targetSkinnedMeshRenderer, plane);

            if (meshSplitData == null)
            {
                return default;
            }

            SplitResult splitResult = _meshSplitterSkinned.CreateSplitObjectsSkinned(gameObject, meshSplitData, CapMaterial, DestroyOriginalMeshWhenSplit, SplitForce, RagdollBones);
            StartCoroutine(_meshSplitterSkinned.ResetCenterOfMassNextFrame(splitResult));

            return splitResult;
        }

        /// <summary>
        /// Same as Split() but runs asynchronously, and the Splitresult is returned in a callback.
        /// </summary>
        public override void SplitAsync(PointPlane plane, System.Action<SplitResult> callback = null)
        {
            _meshSplitterSkinned.SplitSkinnedMeshAsync(targetSkinnedMeshRenderer, plane, AsyncSplitCallback);

            void AsyncSplitCallback(MeshSplitData meshSplitData)
            {
                if(meshSplitData == null)
                {
                    callback?.Invoke(default);
                    return;
                }

                SplitResult splitResult = _meshSplitterSkinned.CreateSplitObjectsSkinned(gameObject, meshSplitData, CapMaterial, DestroyOriginalMeshWhenSplit, SplitForce, RagdollBones);               
                StartCoroutine(_meshSplitterSkinned.ResetCenterOfMassNextFrame(splitResult));

                callback?.Invoke(splitResult);
            }
        }
    }
}