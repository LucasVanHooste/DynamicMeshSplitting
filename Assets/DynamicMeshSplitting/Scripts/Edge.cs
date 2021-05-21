using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    /// <summary>
    /// Representation of an edge consisting out of two indexes pointing to vertices in the vertex array.
    /// </summary>
    public struct IndexEdge : IEquatable<IndexEdge>
    {
        public readonly int V1;
        public readonly int V2;

        public IndexEdge(int v1, int v2)
        {
            V1 = v1;
            V2 = v2;
        }

        public bool Equals(IndexEdge other)
        {
            return (V1 == other.V2 & V2 == other.V1) | (V1 == other.V1 & V2 == other.V2);
        }
    }

    /// <summary>
    /// Representation of an edge consisting out of two vertices.
    /// Also contains the index of vertex V1 in the vertex array.
    /// </summary>
    public struct VertexEdge : IEquatable<VertexEdge>
    {
        public readonly Vector3 V1;
        public readonly Vector3 V2;
        public readonly int OriginalIndex;

        public VertexEdge(Vector3 v1, Vector3 v2, int originalIndex)
        {
            V1 = v1;
            V2 = v2;
            OriginalIndex = originalIndex;
        }

        public bool Equals(VertexEdge other)
        {
            return (V1 == other.V2 & V2 == other.V1)/* | (V1 == other.V1 & V2 == other.V2)*/;
        }
    }
}