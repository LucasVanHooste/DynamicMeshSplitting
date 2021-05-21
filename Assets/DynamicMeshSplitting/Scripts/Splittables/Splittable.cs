using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace JL.Splitting
{
    public class Splittable : SplittableBase
    {
        [Tooltip("The MeshFilter which contains the mesh that will be split.\n" +
            "The targetMeshFilter should be in the hierarchy of the GameObject to which this component is attached.")]
        public MeshFilter targetMeshFilter;
        [Tooltip("Should new convex MeshColliders be generated for the remaining objects after the mesh has been split?")]
        public bool generateMeshColliders = true;

        private MeshSplitter _meshSplitter = null;

        private void Awake()
        {
            _meshSplitter = new MeshSplitter(CapUVMin, CapUVMax);
        }

        /// <summary>
        /// Splits the targetMeshFilter's mesh and duplicates this component's GameObject to make it appear as if it was split.
        /// </summary>
        /// <param name="plane">The plane along which the target mesh is split.</param>
        /// <returns>A SplitResult containing the resulting objects. For more info, see SplitResult.</returns>
        public override SplitResult Split(PointPlane plane)
        {
            MeshSplitData meshSplitData = _meshSplitter.SplitMesh(targetMeshFilter, plane);

            if (meshSplitData == null)
            {
                return default;
            }

            SplitResult splitResult = _meshSplitter.CreateSplitObjects(gameObject, meshSplitData, CapMaterial, DestroyOriginalMeshWhenSplit, SplitForce);
            if (generateMeshColliders)
            {
                _meshSplitter.GenerateMeshCollider(splitResult.posMeshObject, meshSplitData.posMeshData.Mesh);
                _meshSplitter.GenerateMeshCollider(splitResult.negMeshObject, meshSplitData.negMeshData.Mesh);
            }

            StartCoroutine(_meshSplitter.ResetCenterOfMassNextFrame(splitResult));

            return splitResult;
        }

        /// <summary>
        /// Same as Split() but runs asynchronously, and the Splitresult is returned in a callback.
        /// </summary>
        public override void SplitAsync(PointPlane plane, Action<SplitResult> callback = null)
        {
            _meshSplitter.SplitMeshAsync(targetMeshFilter, plane, AsyncSplitCallback);

            void AsyncSplitCallback(MeshSplitData meshSplitData)
            {
                if (meshSplitData == null)
                {
                    callback?.Invoke(default);
                    return;
                }

                SplitResult splitResult = _meshSplitter.CreateSplitObjects(gameObject, meshSplitData, CapMaterial, DestroyOriginalMeshWhenSplit, SplitForce);
                if (generateMeshColliders)
                {
                    _meshSplitter.GenerateMeshColliderAsync(splitResult.posMeshObject, meshSplitData.posMeshData.Mesh);
                    _meshSplitter.GenerateMeshColliderAsync(splitResult.negMeshObject, meshSplitData.negMeshData.Mesh);
                }

                StartCoroutine(_meshSplitter.ResetCenterOfMassNextFrame(splitResult));

                callback?.Invoke(splitResult);
            }
        }
    }
}