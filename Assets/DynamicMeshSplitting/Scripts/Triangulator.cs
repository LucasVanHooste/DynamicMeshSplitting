using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    public class VertexNode
    {
        public VertexNode prevVertex;
        public VertexNode nextVertex;
        public int originalIndex;
        public Vector2 point;

        public VertexNode(Vector2 point, int index)
        {
            this.point = point;
            this.originalIndex = index;
        }
    }

    /// <summary>
    /// This triangulator uses an earclipping algorithm to triangulate polygons that don't have holes inside them.
    /// </summary>
    public static class Triangulator
    {
        static FList<VertexNode> concaveVertices;
        static FList<VertexNode> convexVertices;
        /// <summary>
        /// Triangulates a loop of vertices.
        /// </summary>
        /// <param name="points">A sorted array of points.</param>
        /// <returns>List of triangles consisting out of indexes.</returns>
        public static List<int> TriangulatePolygon(Vector2[] points)
        {
            List<int> triangles = new List<int>(points.Length * 3);

            if (points.Length == 3)
            {
                triangles.Add(0);
                triangles.Add(1);
                triangles.Add(2);

                return triangles;
            }

            PrepareVertexNodes(points);

            Triangulate(triangles);

            return triangles;
        }

        private static void PrepareVertexNodes(Vector2[] points)
        {
            List<VertexNode> vertices = new List<VertexNode>(points.Length);
            concaveVertices = new FList<VertexNode>(points.Length / 2);
            convexVertices = new FList<VertexNode>(points.Length / 2);

            for (int i = 0; i < points.Length; i++)
            {
                vertices.Add(new VertexNode(points[i], i));
            }

            //Set the next and previous vertex
            vertices[0].prevVertex = vertices[vertices.Count - 1];
            vertices[0].nextVertex = vertices[1];
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                vertices[i].prevVertex = vertices[i - 1];
                vertices[i].nextVertex = vertices[i + 1];
            }
            vertices[vertices.Count - 1].prevVertex = vertices[vertices.Count - 2];
            vertices[vertices.Count - 1].nextVertex = vertices[0];

            //Find the concave and convex vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                AddToConvexOrConcave(vertices[i]);
            }
        }

        private static void Triangulate(List<int> outputTriangles)
        {
            int counter = 0;
            int prevConvexCount = 0;
            int prevConcaveCount = 0;
            int prevFail = -2;

            while (convexVertices.Count + concaveVertices.Count >= 3)
            {
                VertexNode[] convexVerticesArray = convexVertices.RawArray;
                prevConvexCount = convexVertices.Count;
                prevConcaveCount = concaveVertices.Count;

                for (int i = convexVertices.Count - 1 - counter % 2; i >= 1 - counter % 2; i -= 2)
                {
                    VertexNode current = convexVerticesArray[i];
                    if (IsEarVertex(current))
                    {
                        convexVertices.RemoveAt(i);
                        VertexNode earVertexPrev = current.prevVertex;
                        VertexNode earVertexNext = current.nextVertex;

                        outputTriangles.Add(current.originalIndex);
                        outputTriangles.Add(earVertexNext.originalIndex);
                        outputTriangles.Add(earVertexPrev.originalIndex);

                        earVertexPrev.nextVertex = earVertexNext;
                        earVertexNext.prevVertex = earVertexPrev;
                    }
                }

                VertexNode[] concaveVerticesArray = concaveVertices.RawArray;
                counter++;

                for (int i = concaveVertices.Count - 1; i >= 0; i--)
                {
                    if (IsConvex(concaveVerticesArray[i]))
                    {
                        convexVertices.Add(concaveVerticesArray[i]);
                        concaveVertices.RemoveAt(i);
                    }
                }

                if (prevConvexCount == convexVertices.Count && prevConcaveCount == concaveVertices.Count)
                {
                    if (prevFail == counter - 1)
                    {
                        LogWrapper.LogError("Could not triangulate Clockwise edge loop correctly. Are you perhaps splitting a mesh that contains holes?");
                        break;
                    }

                    prevFail = counter;
                }
            }
        }


        private static bool IsConvex(VertexNode v)
        {
            Vector2 a = v.prevVertex.point;
            Vector2 b = v.point;
            Vector2 c = v.nextVertex.point;

            return a.x * b.y + c.x * a.y + b.x * c.y - a.x * c.y - c.x * b.y - b.x * a.y > 0f;
        }

        private static void AddToConvexOrConcave(VertexNode v)
        {
            Vector2 prev = v.prevVertex.point;
            Vector2 point = v.point;
            Vector2 next = v.nextVertex.point;

            if (prev.x * point.y + next.x * prev.y + point.x * next.y - prev.x * next.y - next.x * point.y - point.x * prev.y > 0f)
            {
                convexVertices.Add(v);
            }
            else
            {
                concaveVertices.Add(v);
            }
        }

        private static bool IsEarVertex(VertexNode v)
        {
            VertexNode[] concaveVerticesArray = concaveVertices.RawArray;

            for (int i = 0; i < concaveVertices.Count; i++)
            {
                if (PointInTriangle(concaveVerticesArray[i].point, v.prevVertex.point, v.point, v.nextVertex.point))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1 = (pt.x - v2.x) * (v1.y - v2.y) - (v1.x - v2.x) * (pt.y - v2.y);
            float d2 = (pt.x - v3.x) * (v2.y - v3.y) - (v2.x - v3.x) * (pt.y - v3.y);
            float d3 = (pt.x - v1.x) * (v3.y - v1.y) - (v3.x - v1.x) * (pt.y - v1.y);

            return ((d1 < 0) && (d2 < 0) && (d3 < 0)) || ((d1 > 0) && (d2 > 0) && (d3 > 0));
        }
    }
}
