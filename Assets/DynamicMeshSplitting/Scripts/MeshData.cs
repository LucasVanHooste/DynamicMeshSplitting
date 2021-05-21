using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    /// <summary>
    /// A class which holds the result of a split.
    /// </summary>
    public class MeshSplitData
    {
        public MeshSplitData(MeshData posMeshData, MeshData negMeshData)
        {
            this.posMeshData = posMeshData;
            this.negMeshData = negMeshData;
        }

        public MeshData posMeshData;
        public MeshData negMeshData;
    }

    /// <summary>
    /// A helper class which stores data used to create a mesh.
    /// </summary>
    public class MeshData
    {
        /// <summary>
        /// A list containing an array of triangle indexes for each submesh.
        /// </summary>
        public List<int[]> trianglesSubmeshes;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector2[] uv, uv2;
        public Color[] colors;
        public BoneWeight[] boneWeights;
        public Matrix4x4[] bindPoses;

        /// <summary>
        /// Each element in the list corresponds to the amount of triangles in the submesh at the same index.
        /// Some of the submeshes will have 0 triangles because they lay on the other side of the cut.
        /// This array is useful for filtering out unused submeshes/materials after splitting a mesh.
        /// </summary>
        public List<int> unfilteredSubmeshes;

        public Mesh Mesh
        {
            get
            {
                if (_mesh == null)
                {
                    _mesh = CreateMesh(string.Empty);
                }

                return _mesh;
            }
        }
        private Mesh _mesh = null;

        public MeshData(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uv, Vector2[] uv2,
            Color[] colors, BoneWeight[] boneWeights, Matrix4x4[] bindPoses, List<int[]> trianglesSubmeshes, List<int> unfilteredSubmeshes)
        {
            this.vertices = vertices;
            this.normals = normals;
            this.tangents = tangents;
            this.uv = uv;
            this.uv2 = uv2;
            this.colors = colors;
            this.boneWeights = boneWeights;
            this.bindPoses = bindPoses;

            this.trianglesSubmeshes = trianglesSubmeshes;
            this.unfilteredSubmeshes = unfilteredSubmeshes;
        }

        /// <summary>
        /// Creates a mesh from the mesh data. Needs to be called on the main thread!
        /// </summary>
        /// <returns></returns>
        public Mesh CreateMesh(string name)
        {
            Mesh newMesh = new Mesh();
            newMesh.name = name;
            if(vertices.Length > 65535)
            {
                newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            newMesh.vertices = vertices;
            newMesh.normals = normals;
            newMesh.tangents = tangents;
            newMesh.uv = uv;
            newMesh.uv2 = uv2;
            newMesh.colors = colors;
            newMesh.boneWeights = boneWeights;
            newMesh.bindposes = bindPoses;

            int subMeshCount = trianglesSubmeshes.Count;
            newMesh.subMeshCount = subMeshCount;
            for (int i = 0; i < subMeshCount; i++)
            {
                newMesh.SetTriangles(trianglesSubmeshes[i], i);
            }

            newMesh.RecalculateBounds();

            return newMesh;
        }
    }

}
