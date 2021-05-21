using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    /// <summary>
    /// A helper class used to sort vertices in a loop
    /// </summary>
    public class SortedLoop
    {
        public VertexEdge First => rawArray[first];
        public VertexEdge Last => rawArray[last];

        public int Count;
        private int first;
        private int last;

        private VertexEdge[] rawArray;

        public SortedLoop(int capacity, VertexEdge firstElement)
        {
            rawArray = new VertexEdge[capacity];

            rawArray[0] = firstElement;
            Count = 1;
        }

        public bool TryAdd(VertexEdge edge)
        {
            //last
            if ((edge.V1.x - rawArray[last].V2.x) * (edge.V1.x - rawArray[last].V2.x)
                + (edge.V1.y - rawArray[last].V2.y) * (edge.V1.y - rawArray[last].V2.y)
                + (edge.V1.z - rawArray[last].V2.z) * (edge.V1.z - rawArray[last].V2.z) < MeshSplitterBase.SqrErrorThreshold
                //&& (edge.V1.x - rawArray[first].V1.x) * (edge.V1.x - rawArray[first].V1.x)
                //+ (edge.V1.y - rawArray[first].V1.y) * (edge.V1.y - rawArray[first].V1.y)
                //+ (edge.V1.z - rawArray[first].V1.z) * (edge.V1.z - rawArray[first].V1.z) >= MeshSlicer.SqrErrorThreshold
                )
            {
                ++last; ++Count;
                rawArray[last] = edge;
                return true;
            }
            //first
            if ((edge.V2.x - rawArray[first].V1.x) * (edge.V2.x - rawArray[first].V1.x)
                + (edge.V2.y - rawArray[first].V1.y) * (edge.V2.y - rawArray[first].V1.y)
                + (edge.V2.z - rawArray[first].V1.z) * (edge.V2.z - rawArray[first].V1.z) < MeshSplitterBase.SqrErrorThreshold
                //&& (edge.V2.x - rawArray[last].V2.x) * (edge.V2.x - rawArray[last].V2.x)
                //+ (edge.V2.y - rawArray[last].V2.y) * (edge.V2.y - rawArray[last].V2.y)
                //+ (edge.V2.z - rawArray[last].V2.z) * (edge.V2.z - rawArray[last].V2.z) >= MeshSlicer.SqrErrorThreshold
                )
            {
                first = (first - 1 + rawArray.Length) % rawArray.Length;
                ++Count;
                rawArray[first] = edge;
                return true;
            }

            return false;
        }

        public VertexEdge[] ToArray()
        {
            VertexEdge[] result = new VertexEdge[Count];
            int counter = 0;

            if (first != 0)
            {
                for (int i = first; i < rawArray.Length; i++)
                {
                    result[counter] = rawArray[i];
                    counter++;
                }
            }
            for (int i = 0; i <= last; i++)
            {
                result[counter] = rawArray[i];
                counter++;
            }

            return result;
        }
    }
}