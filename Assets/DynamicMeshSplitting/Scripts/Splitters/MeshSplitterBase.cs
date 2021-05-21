using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    public class MeshSplitterBase
    {
        public Vector2 CapUVMin = Vector2.zero;
        public Vector2 CapUVMax = Vector2.one;

        public const float ErrorThreshold = .000001f;
        public const float SqrErrorThreshold = ErrorThreshold * ErrorThreshold;
        /// <summary>
        /// A buffer that holds the meshes which are being split. 
        /// This ensures that a MeshSplitter can't split a mesh that is already being split.
        /// </summary>
        protected static HashSet<Mesh> meshesBeingSplit = new HashSet<Mesh>();

        protected FList<int> trisPos, trisNeg;
        protected FList<VertexEdge> newEdges;

        protected PointPlane plane;
        protected Quaternion ownRotation;

        protected float[] dotProducts;

        protected Mesh originalMesh;
        protected Vector3[] originalVertices;
        protected Vector3[] worldSpaceVertices;
        protected int originalVerticesCount;
        protected int[][] originalSubMeshes;
        protected List<int> subMeshesPos, subMeshesNeg;
        protected Vector3[] originalNormals;
        protected Vector4[] originalTangents;
        protected Vector2[] originalUVs;
        protected Vector2[] originalUV2s;
        protected Color[] originalColors;
        protected BoneWeight[] originalBoneWeights;
        protected Matrix4x4[] originalBindPoses;

        protected List<Vector3> verticesNew;
        protected List<Vector3> worldSpaceVerticesNew;
        protected List<Vector3> normalsNew;
        protected List<Vector4> tangentsNew;
        protected List<Vector2> uvsNew;
        protected List<Vector2> uv2sNew;
        protected List<Color> colorsNew;
        protected List<BoneWeight> boneWeightsNew;
        protected List<Matrix4x4> bindPosesNew;

        protected Dictionary<IndexEdge, int> cutEdges;
        protected Matrix4x4 localToWorldMatrix;

        public MeshSplitterBase() { }
        /// <param name="capUVMin">Scales the cap's UV coordinates to ensure no value is smaller.</param>
        /// <param name="capUVMax">Scales the cap's UV coordinates to ensure no value is greater.</param>
        public MeshSplitterBase(Vector2 capUVMin, Vector2 capUVMax)
        {
            CapUVMin = capUVMin;
            CapUVMax = capUVMax;
        }

        #region Mesh Splitting Functions
        /// <summary>
        /// Allocates the memory needed to perform the splitting of a mesh.
        /// </summary>
        protected void AllocateMemory(Transform meshTransform, Mesh mesh, PointPlane slicePlane)
        {
            originalMesh = mesh;
            ownRotation = meshTransform.rotation;
            localToWorldMatrix = meshTransform.localToWorldMatrix;
            plane = slicePlane;

            originalVertices = originalMesh.vertices;
            originalVerticesCount = originalVertices.Length;
            originalNormals = originalMesh.normals;
            originalTangents = originalMesh.tangents;
            originalUVs = originalMesh.uv;
            originalUV2s = originalMesh.uv2;
            originalColors = originalMesh.colors;
            originalBoneWeights = originalMesh.boneWeights;
            originalBindPoses = originalMesh.bindposes;
            worldSpaceVertices = new Vector3[originalVerticesCount];

            int countHalf = originalVerticesCount / 2;
            verticesNew = new List<Vector3>(countHalf);
            worldSpaceVerticesNew = new List<Vector3>(countHalf);
            if (originalNormals.Length != 0) normalsNew = new List<Vector3>(countHalf);
            if (originalTangents.Length != 0) tangentsNew = new List<Vector4>(countHalf);
            if (originalUVs.Length != 0) uvsNew = new List<Vector2>(countHalf);
            if (originalUV2s.Length != 0) uv2sNew = new List<Vector2>(countHalf);
            if (originalColors.Length != 0) colorsNew = new List<Color>(countHalf);
            if (originalBoneWeights.Length != 0) boneWeightsNew = new List<BoneWeight>(countHalf);
            if (originalBindPoses.Length != 0) bindPosesNew = new List<Matrix4x4>(countHalf);
            dotProducts = new float[originalVerticesCount];

            cutEdges = new Dictionary<IndexEdge, int>(countHalf);
            newEdges = new FList<VertexEdge>(countHalf);

            int triangleCount = 0;
            originalSubMeshes = new int[originalMesh.subMeshCount][];
            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; ++subMeshIndex)
            {
                originalSubMeshes[subMeshIndex] = mesh.GetTriangles(subMeshIndex);
                triangleCount += originalSubMeshes[subMeshIndex].Length;
            }

            int doublePlusOne = (triangleCount * 2) + 1;
            trisPos = new FList<int>(doublePlusOne);
            trisNeg = new FList<int>(doublePlusOne);
        }

        /// <summary>
        /// Calculates on which side of the plane each vertex of the target mesh lies.
        /// </summary>
        protected void CalculateSides()
        {
            for (int i = 0; i < originalVerticesCount; i++)
            {
                dotProducts[i] = plane.normal.x * (worldSpaceVertices[i].x - plane.point.x) +
                                plane.normal.y * (worldSpaceVertices[i].y - plane.point.y) +
                                plane.normal.z * (worldSpaceVertices[i].z - plane.point.z);
            }
        }

        /// <summary>
        /// Loops over all triangles/submeshes of the original mesh and checks whether the plane intersects with them or not.
        /// If an intersection is found, the points of intersection are calculated in SliceTriangleXXX().
        /// </summary>
        protected void CheckForIntersections()
        {
            int trisPosCount = 0, trisNegCount = 0;
            subMeshesPos = new List<int>(originalSubMeshes.Length);
            subMeshesNeg = new List<int>(originalSubMeshes.Length);

            int[] currentTriangle = new int[3];
            float[] dots = new float[3];
            bool[] edgeHit = new bool[3];

            int[] trisPosArray = trisPos.RawArray;
            int[] trisNegArray = trisNeg.RawArray;

            for (int subMeshIndex = 0; subMeshIndex < originalSubMeshes.Length; subMeshIndex++)
            {
                int[] currentSubMesh = originalSubMeshes[subMeshIndex];
                int subMeshTrianglesLength = currentSubMesh.Length - 2;

                for (int i = 0; i < subMeshTrianglesLength; i += 3)
                {
                    currentTriangle[0] = currentSubMesh[i];
                    currentTriangle[1] = currentSubMesh[i + 1];
                    currentTriangle[2] = currentSubMesh[i + 2];

                    dots[0] = dotProducts[currentTriangle[0]];
                    dots[1] = dotProducts[currentTriangle[1]];
                    dots[2] = dotProducts[currentTriangle[2]];

                    bool allpos = dots[0] >= 0 & dots[1] >= 0 & dots[2] >= 0;
                    bool allneg = dots[0] < 0 & dots[1] < 0 & dots[2] < 0;

                    //Adds the triangle to the positive list
                    if (allpos)
                    {
                        //same as doing Add() but faster
                        trisPosArray[trisPos.Count] = currentTriangle[0];
                        trisPosArray[trisPos.Count + 1] = currentTriangle[1];
                        trisPosArray[trisPos.Count + 2] = currentTriangle[2];

                        trisPos.Count += 3;
                        continue;
                    }
                    //Adds the triangle to the negative list
                    if (allneg)
                    {
                        //same as doing Add() but faster
                        trisNegArray[trisNeg.Count] = currentTriangle[0];
                        trisNegArray[trisNeg.Count + 1] = currentTriangle[1];
                        trisNegArray[trisNeg.Count + 2] = currentTriangle[2];

                        trisNeg.Count += 3;
                        continue;
                    }

                    //This means the plane intersects the triangle
                    edgeHit[0] = (dots[0] < 0 & dots[1] >= 0) | (dots[0] >= 0 & dots[1] < 0);
                    edgeHit[1] = (dots[1] < 0 & dots[2] >= 0) | (dots[1] >= 0 & dots[2] < 0);
                    edgeHit[2] = (dots[2] < 0 & dots[0] >= 0) | (dots[2] >= 0 & dots[0] < 0);

                    if (edgeHit[0] & edgeHit[1])
                    {
                        if (dots[1] >= 0)
                            SliceTrianglePos(1, currentTriangle);
                        else
                            SliceTriangleNeg(1, currentTriangle);
                    }
                    else if (edgeHit[1] & edgeHit[2])
                    {
                        if (dots[2] >= 0)
                            SliceTrianglePos(2, currentTriangle);
                        else
                            SliceTriangleNeg(2, currentTriangle);
                    }
                    else if (edgeHit[2] & edgeHit[0])
                    {
                        if (dots[0] >= 0)
                            SliceTrianglePos(0, currentTriangle);
                        else
                            SliceTriangleNeg(0, currentTriangle);
                    }
                }

                //total amount of tris on pos side minus old amount to get the amount of triangles that were just added.
                subMeshesPos.Add(trisPos.Count - trisPosCount);
                trisPosCount = trisPos.Count;

                subMeshesNeg.Add(trisNeg.Count - trisNegCount);
                trisNegCount = trisNeg.Count;
            }
        }

        /// <summary>
        /// Splits a triangle which has one vertex on the positive side of the plane and two on the negative side.
        /// </summary>
        /// <param name="posIndex">The index of the vertex which lies on the positive side of the plane. (0-2)</param>
        /// <param name="currentTriangle"></param>
        private void SliceTrianglePos(int posIndex, int[] currentTriangle)
        {
            int i0 = posIndex;
            int i1 = (posIndex + 1) % 3;
            int i2 = (posIndex + 2) % 3;

            //add the 2 new vertices to the newVertices arrays
            int indexHit0 = AddLerpVertex(currentTriangle[i0], currentTriangle[i1]);
            int indexHit1 = AddLerpVertex(currentTriangle[i0], currentTriangle[i2]);

            //add triangles to the pos or neg side
            trisPos.Add(currentTriangle[i0]);
            trisPos.Add(indexHit0);
            trisPos.Add(indexHit1);

            trisNeg.Add(currentTriangle[i1]);
            trisNeg.Add(indexHit1);
            trisNeg.Add(indexHit0);
            trisNeg.Add(currentTriangle[i2]);
            trisNeg.Add(indexHit1);
            trisNeg.Add(currentTriangle[i1]);

            //store the newly created edge
            Vector3 V0 = worldSpaceVerticesNew[indexHit0 - originalVerticesCount];
            Vector3 V1 = worldSpaceVerticesNew[indexHit1 - originalVerticesCount];

            if ((V0.x - V1.x) * (V0.x - V1.x) + (V0.y - V1.y) * (V0.y - V1.y) + (V0.z - V1.z) * (V0.z - V1.z)
                < SqrErrorThreshold) return;

            newEdges.Add(new VertexEdge(V1, V0, indexHit1 - originalVerticesCount));
        }

        /// <summary>
        /// Splits a triangle which has one vertex on the negative side of the plane and two on the positive side.
        /// </summary>
        /// <param name="negIndex">The index of the vertex which lies on the negative side of the plane. (0-2)</param>
        /// <param name="currentTriangle"></param>
        private void SliceTriangleNeg(int negIndex, int[] currentTriangle)
        {
            int i0 = negIndex;
            int i1 = (negIndex + 1) % 3;
            int i2 = (negIndex + 2) % 3;

            //add the 2 new vertices to the newVertices arrays
            int indexHit0 = AddLerpVertex(currentTriangle[i1], currentTriangle[i0]);
            int indexHit1 = AddLerpVertex(currentTriangle[i2], currentTriangle[i0]);

            //add triangles to the pos or neg side
            trisNeg.Add(currentTriangle[i0]);
            trisNeg.Add(indexHit0);
            trisNeg.Add(indexHit1);

            trisPos.Add(currentTriangle[i1]);
            trisPos.Add(indexHit1);
            trisPos.Add(indexHit0);
            trisPos.Add(currentTriangle[i2]);
            trisPos.Add(indexHit1);
            trisPos.Add(currentTriangle[i1]);

            //store the newly created edge
            Vector3 V0 = worldSpaceVerticesNew[indexHit0 - originalVerticesCount];
            Vector3 V1 = worldSpaceVerticesNew[indexHit1 - originalVerticesCount];

            if ((V0.x - V1.x) * (V0.x - V1.x) + (V0.y - V1.y) * (V0.y - V1.y) + (V0.z - V1.z) * (V0.z - V1.z)
                < SqrErrorThreshold) return;

            newEdges.Add(new VertexEdge(V0, V1, indexHit0 - originalVerticesCount));
        }

        /// <summary>
        /// Adds a new vertex at the point of intersection between an edge and the plane.
        /// </summary>
        /// <param name="from">The first vertex of the edge.</param>
        /// <param name="to">The second vertex of the edge.</param>
        /// <returns></returns>
        public int AddLerpVertex(int from, int to)
        {
            int index;
            IndexEdge currentEdge = new IndexEdge(from, to);

            if (cutEdges.TryGetValue(currentEdge, out index))
                return index;

            index = originalVerticesCount + verticesNew.Count;
            cutEdges[currentEdge] = index;

            float lerp = LineIntersect(worldSpaceVertices[from], worldSpaceVertices[to], plane);

            verticesNew.Add(Vector3.Lerp(originalVertices[from], originalVertices[to], lerp));
            worldSpaceVerticesNew.Add(Vector3.Lerp(worldSpaceVertices[from], worldSpaceVertices[to], lerp));
            if (normalsNew != null) normalsNew.Add(Vector3.Lerp(originalNormals[from], originalNormals[to], lerp));
            if (tangentsNew != null) tangentsNew.Add(Vector4.Lerp(originalTangents[from], originalTangents[to], lerp));
            if (uvsNew != null) uvsNew.Add(Vector2.Lerp(originalUVs[from], originalUVs[to], lerp));
            if (uv2sNew != null) uv2sNew.Add(Vector2.Lerp(originalUV2s[from], originalUV2s[to], lerp));
            if (colorsNew != null) colorsNew.Add(Color.Lerp(originalColors[from], originalColors[to], lerp));
            if (boneWeightsNew != null)
            {
                BoneWeight fromWeight = originalBoneWeights[from];
                BoneWeight toWeight = originalBoneWeights[to];

                var weights = new Dictionary<int, float>();
                var finalWeights = new FList<KeyValuePair<int, float>>(4);
                float totalOfWeights = 0;

                if (fromWeight.weight0 != 0) weights[fromWeight.boneIndex0] = fromWeight.weight0;
                if (fromWeight.weight1 != 0) weights[fromWeight.boneIndex1] = fromWeight.weight1;
                if (fromWeight.weight2 != 0) weights[fromWeight.boneIndex2] = fromWeight.weight2;
                if (fromWeight.weight3 != 0) weights[fromWeight.boneIndex3] = fromWeight.weight3;

                void AddWeight(int boneIndex, float boneWeight)
                {
                    if (weights.TryGetValue(boneIndex, out float weight))
                    {
                        float lerpWeight = Mathf.Lerp(weight, boneWeight, lerp);
                        finalWeights.Add(new KeyValuePair<int, float>(boneIndex, lerpWeight));
                        totalOfWeights += lerpWeight;
                        weights.Remove(boneIndex);
                    }
                    else
                    {
                        weights[boneIndex] = boneWeight;
                    }
                }

                if (toWeight.weight0 != 0)
                {
                    AddWeight(toWeight.boneIndex0, toWeight.weight0);
                }
                if (toWeight.weight1 != 0)
                {
                    AddWeight(toWeight.boneIndex1, toWeight.weight1);
                }
                if (toWeight.weight2 != 0)
                {
                    AddWeight(toWeight.boneIndex2, toWeight.weight2);
                }
                if (toWeight.weight3 != 0)
                {
                    AddWeight(toWeight.boneIndex3, toWeight.weight3);
                }

                //fill remaining weights
                while (weights.Count > 0 & finalWeights.Count < 4)
                {
                    //get highest weight in dict
                    float highest = 0;
                    int highestIndex = 0;
                    foreach (var kvp in weights)
                    {
                        if (kvp.Value > highest)
                        {
                            highest = kvp.Value;
                            highestIndex = kvp.Key;
                        }
                    }

                    totalOfWeights += highest;
                    finalWeights.Add(new KeyValuePair<int, float>(highestIndex, highest));
                }

                //apply normalized weights
                float ratio = 1 / totalOfWeights;
                BoneWeight boneWeightNew = new BoneWeight();
                var rawArray = finalWeights.RawArray;

                boneWeightNew.boneIndex0 = rawArray[0].Key;
                boneWeightNew.weight0 = rawArray[0].Value * ratio;
                boneWeightNew.boneIndex1 = rawArray[1].Key;
                boneWeightNew.weight1 = rawArray[1].Value * ratio;
                boneWeightNew.boneIndex2 = rawArray[2].Key;
                boneWeightNew.weight2 = rawArray[2].Value * ratio;
                boneWeightNew.boneIndex3 = rawArray[3].Key;
                boneWeightNew.weight3 = rawArray[3].Value * ratio;

                boneWeightsNew.Add(boneWeightNew);
            }

            return index;
        }

        /// <summary>
        /// Returns the lerp representation of where the plane has intersected with and edge.
        /// </summary>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        private float LineIntersect(Vector3 lineStart, Vector3 lineEnd, PointPlane plane)
        {
            return Vector3.Dot(plane.normal, plane.point - lineStart) / Vector3.Dot(plane.normal, lineEnd - lineStart);
        }

        /// <summary>
        /// Creates caps on boths sides where the plane has split the original mesh.
        /// </summary>
        /// <param name="capEdges"></param>
        /// <param name="normal"></param>
        protected void CreateCaps(FList<VertexEdge> capEdges, Vector3 normal)
        {
            int amountOfEdges = capEdges.Count;
            if (amountOfEdges < 3)
                return;

            VertexEdge[][] sortedCapEdges = SortCaps(capEdges);
            Vector2[][] flatCapPoints = ToFlatPoints(sortedCapEdges);

            Vector2[] capsUV = CalculateUVCoordinates(amountOfEdges, flatCapPoints);
            List<int>[] triangulatedCaps = TriangulateCaps(flatCapPoints);

            //add caps on pos side
            int oldTrisCount = trisPos.Count;
            for (int i = 0; i < triangulatedCaps.Length; i++)
            {
                int oldVertexCount = originalVerticesCount + verticesNew.Count;

                //The cap has different vertex properties (normals, etc.) so the vertices have to be added again.
                VertexEdge[] sortedCapEdge = sortedCapEdges[i];
                for (int j = 0; j < sortedCapEdge.Length; j++)
                {
                    AddCapVertex(sortedCapEdge[j].OriginalIndex, -normal, capsUV[j]);
                }

                //Add new triangles from cap to list of triangles
                List<int> triangulatedCapIndices = triangulatedCaps[i];
                for (int j = 0; j < triangulatedCapIndices.Count; j++)
                {
                    trisPos.Add(triangulatedCapIndices[j] + oldVertexCount);
                }
            }
            subMeshesPos.Add(trisPos.Count - oldTrisCount);

            //add caps on neg side
            oldTrisCount = trisNeg.Count;
            for (int i = 0; i < triangulatedCaps.Length; i++)
            {
                int oldVertexCount = originalVerticesCount + verticesNew.Count;

                //The cap has different vertex properties (normals, etc.) so the vertices have to be added again.
                VertexEdge[] sortedCapEdge = sortedCapEdges[i];
                for (int j = 0; j < sortedCapEdge.Length; j++)
                {
                    AddCapVertex(sortedCapEdge[j].OriginalIndex, normal, capsUV[j]);
                }

                //Add new triangles from cap to list of triangles
                List<int> triangulatedCapIndices = triangulatedCaps[i];
                for (int j = triangulatedCapIndices.Count - 1; j >= 0; j--)
                {
                    trisNeg.Add(triangulatedCapIndices[j] + oldVertexCount);
                }
            }
            subMeshesNeg.Add(trisNeg.Count - oldTrisCount);
        }

        /// <summary>
        /// Attempts to create loops out of all the edges on the plane's cut.
        /// </summary>
        /// <param name="capEdges">Unsorted list of all cap edges.</param>
        /// <returns>Array of sorted arrays.</returns>
        private VertexEdge[][] SortCaps(FList<VertexEdge> capEdges)
        {
            List<SortedLoop> sortedLoops = new List<SortedLoop>();
            VertexEdge[] capEdgesArray = capEdges.RawArray;

            int startcount = capEdges.Count - 1;
            var currentLoop = new SortedLoop(capEdges.Count, capEdgesArray[startcount]);
            capEdges.RemoveAt(startcount);

            while (currentLoop != null)
            {
                int prevCount = 0;
                while (prevCount < currentLoop.Count)
                {
                    prevCount = currentLoop.Count;
                    for (int j = capEdges.Count - 1; j >= 0; j--)
                    {
                        if (currentLoop.TryAdd(capEdgesArray[j]))
                        {
                            capEdges.RemoveAt(j);
                        }
                    }
                }

                if (currentLoop.First.V1 == currentLoop.Last.V2)
                    sortedLoops.Add(currentLoop);
                else
                    LogWrapper.LogWarning("Failed to create edge loop");

                currentLoop = null;

                if (capEdges.Count > 2)
                {
                    int countMinusOne = capEdges.Count - 1;

                    currentLoop = new SortedLoop(capEdges.Count, capEdgesArray[countMinusOne]);
                    capEdges.RemoveAt(countMinusOne);
                }
            }

            LogWrapper.Log("Amount of cap loops: " + sortedLoops.Count);
            return ConvertSortedLoopsToArrays(sortedLoops);
        }

        private VertexEdge[][] ConvertSortedLoopsToArrays(List<SortedLoop> sortedLoops)
        {
            VertexEdge[][] sortedArrays = new VertexEdge[sortedLoops.Count][];
            for (int i = 0; i < sortedLoops.Count; i++)
            {
                sortedArrays[i] = sortedLoops[i].ToArray();
            }

            return sortedArrays;
        }
        /// <summary>
        /// Converts the 3D edges to 2D points on the splitting plane.
        /// </summary>
        /// <param name="caps">An array of arrays containing 3D edges.</param>
        /// <returns>An array of arrays containing 2D points.</returns>
        private Vector2[][] ToFlatPoints(VertexEdge[][] caps)
        {
            Quaternion invRotation = Quaternion.Inverse(plane.rotation);

            Vector2[][] result = new Vector2[caps.Length][];
            for (int i = 0; i < caps.Length; i++)
            {
                VertexEdge[] capEdges = caps[i];
                Vector2[] flatEdges = new Vector2[caps[i].Length];

                for (int j = 0; j < capEdges.Length; j++)
                {
                    Vector3 vec = invRotation * capEdges[j].V1;
                    flatEdges[j] = new Vector2(vec.x, vec.z);
                }

                result[i] = flatEdges;
            }

            return result;
        }

        /// <summary>
        /// Calculates the UV coordinates for all points on the cap.
        /// </summary>
        /// <returns>Array of UV coordinates.</returns>
        private Vector2[] CalculateUVCoordinates(int amountOfEdges, Vector2[][] flatCapPoints)
        {
            Vector2[] capsUV = new Vector2[amountOfEdges];
            Vector2 minBounds = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxBounds = new Vector2(float.MinValue, float.MinValue);

            //get the 2D bounds of the points.
            for (int i = 0; i < flatCapPoints.Length; i++)
            {
                for (int j = 0; j < flatCapPoints[i].Length; j++)
                {
                    Vector2 vert = flatCapPoints[i][j];

                    if (minBounds.x > vert.x)
                        minBounds.x = vert.x;
                    else if (maxBounds.x < vert.x)
                        maxBounds.x = vert.x;

                    if (minBounds.y > vert.y)
                        minBounds.y = vert.y;
                    else if (maxBounds.y < vert.y)
                        maxBounds.y = vert.y;
                }
            }

            float xRange = maxBounds.x - minBounds.x;
            float yRange = maxBounds.y - minBounds.y;
            int counter = 0;
            //normalize 2d points to [0-1,0-1] using x and y ranges
            for (int i = 0; i < flatCapPoints.Length; i++)
            {
                for (int j = 0; j < flatCapPoints[i].Length; j++)
                {
                    Vector2 vert = flatCapPoints[i][j];
                    capsUV[counter].x = (vert.x - minBounds.x) / xRange;
                    capsUV[counter].y = (vert.y - minBounds.y) / yRange;
                    counter++;
                }
            }

            //map 2d points from normalized to a custom uv area, if a custom area is set
            if (CapUVMin.x != 0 | CapUVMin.y != 0 | CapUVMax.x != 1 | CapUVMax.y != 1)
            {
                xRange = CapUVMax.x - CapUVMin.x;
                yRange = CapUVMax.y - CapUVMin.y;

                for (int i = 0; i < counter; i++)
                {
                    capsUV[i].x = (capsUV[i].x * xRange) + CapUVMin.x;
                    capsUV[i].y = (capsUV[i].y * yRange) + CapUVMin.y;
                }
            }

            return capsUV;
        }

        /// <summary>
        /// Triangulates the caps created by the plane splitting.
        /// </summary>
        /// <param name="flatCapPoints">An array of arrays containing cap points.</param>
        /// <returns>An array of lists containing triangle indexes.</returns>
        private List<int>[] TriangulateCaps(Vector2[][] flatCapPoints)
        {
            List<int>[] triangulatedCaps = new List<int>[flatCapPoints.Length];
            for (int i = 0; i < flatCapPoints.Length; i++)
            {
                triangulatedCaps[i] = Triangulator.TriangulatePolygon(flatCapPoints[i]);
            }

            return triangulatedCaps;
        }

        private static Vector3 Vector3Up = Vector3.up;
        private static Vector3 Vector3Forward = Vector3.forward;
        private static Vector4 Vector4Zero = Vector4.zero;
        /// <summary>
        /// Adds a new cap vertex with properties.
        /// </summary>
        /// <param name="index">Index of the vertex that will be copied.</param>
        /// <param name="normal">New normal for the vertex.</param>
        /// <param name="capUV">New UV coordinates for the vertex.</param>
        public void AddCapVertex(int index, Vector3 normal, Vector2 capUV)
        {
            verticesNew.Add(verticesNew[index]);

            if (normalsNew != null) normalsNew.Add(normal);
            if (uvsNew != null) uvsNew.Add(capUV);
            if (uv2sNew != null) uv2sNew.Add(capUV);
            if (colorsNew != null) colorsNew.Add(colorsNew[index]);
            if (boneWeightsNew != null) boneWeightsNew.Add(boneWeightsNew[index]);
            if (tangentsNew != null)
            {
                Vector4 tangent = Vector4Zero;
                Vector3 c1 = Vector3.Cross(normal, Vector3Forward);
                Vector3 c2 = Vector3.Cross(normal, Vector3Up);
                if (c1.sqrMagnitude > c2.sqrMagnitude)
                {
                    tangent.x = c1.x;
                    tangent.y = c1.y;
                    tangent.z = c1.z;
                }
                else
                {
                    tangent.x = c2.x;
                    tangent.y = c2.y;
                    tangent.z = c2.z;
                }
                tangentsNew.Add(tangent.normalized);
            }
        }

        /// <summary>
        /// Not all of the old vertices will be present in the new mesh so they need to be remapped.
        /// </summary>
        /// <param name="triangles">All triangles on that side of the cut.</param>
        /// <param name="subMeshes">The indexes of the submeshes in the triangles array.</param>
        /// <returns></returns>
        protected MeshData CreateMeshData(int[] triangles, List<int> subMeshes)
        {
            List<int[]> resultingSubMeshes = new List<int[]>(subMeshes.Count);
            for (int i = 0; i < subMeshes.Count; i++)
            {
                if (subMeshes[i] > 0)
                    resultingSubMeshes.Add(new int[subMeshes[i]]);
            }

            int[] translateIndex = new int[originalVerticesCount + verticesNew.Count];
            for (int i = 0; i < translateIndex.Length; i++)
                translateIndex[i] = -1;

            int uniTriCount = 0;
            int currentTriangleIndex = 0;
            //Run over all triangles, using the submeshes
            for (int i = 0; i < resultingSubMeshes.Count; i++)
            {
                int[] resultingSubMesh = resultingSubMeshes[i];

                for (int j = 0; j < resultingSubMesh.Length; currentTriangleIndex++, j++)
                {
                    int vertexIndex = triangles[currentTriangleIndex];

                    //map submeshes, if the same index is found, don't add new map 
                    if (translateIndex[vertexIndex] == -1)
                    {
                        translateIndex[vertexIndex] = uniTriCount++;
                    }

                    resultingSubMesh[j] = translateIndex[vertexIndex];
                }
            }

            Vector3[] localVerts = new Vector3[uniTriCount];
            Vector3[] localNormals = originalNormals.Length != 0 ? new Vector3[uniTriCount] : null;
            Vector4[] localTangents = originalTangents.Length != 0 ? new Vector4[uniTriCount] : null;
            Vector2[] localUv = originalUVs.Length != 0 ? new Vector2[uniTriCount] : null;
            Vector2[] localUv2 = originalUV2s.Length != 0 ? new Vector2[uniTriCount] : null;
            Color[] localColors = originalColors.Length != 0 ? new Color[uniTriCount] : null;
            BoneWeight[] localBoneWeights = originalBoneWeights.Length != 0 ? new BoneWeight[uniTriCount] : null;

            uniTriCount = 0;
            currentTriangleIndex = 0;

            for (int i = 0; i < resultingSubMeshes.Count; i++)
            {
                int subMeshTriangleCount = resultingSubMeshes[i].Length;
                for (int j = 0; j < subMeshTriangleCount; currentTriangleIndex++, j++)
                {
                    int vertIndex = triangles[currentTriangleIndex];
                    if (translateIndex[vertIndex] >= uniTriCount)
                    {
                        if (vertIndex < originalVerticesCount)
                        {
                            localVerts[uniTriCount] = originalVertices[vertIndex];
                            if (normalsNew != null) localNormals[uniTriCount] = originalNormals[vertIndex];
                            if (tangentsNew != null) localTangents[uniTriCount] = originalTangents[vertIndex];
                            if (uvsNew != null) localUv[uniTriCount] = originalUVs[vertIndex];
                            if (uv2sNew != null) localUv2[uniTriCount] = originalUV2s[vertIndex];
                            if (colorsNew != null) localColors[uniTriCount] = originalColors[vertIndex];
                            if (boneWeightsNew != null) localBoneWeights[uniTriCount] = originalBoneWeights[vertIndex];
                        }
                        else
                        {
                            vertIndex -= originalVerticesCount;
                            localVerts[uniTriCount] = verticesNew[vertIndex];
                            if (normalsNew != null) localNormals[uniTriCount] = normalsNew[vertIndex];
                            if (tangentsNew != null) localTangents[uniTriCount] = tangentsNew[vertIndex];
                            if (uvsNew != null) localUv[uniTriCount] = uvsNew[vertIndex];
                            if (uv2sNew != null) localUv2[uniTriCount] = uv2sNew[vertIndex];
                            if (colorsNew != null) localColors[uniTriCount] = colorsNew[vertIndex];
                            if (boneWeightsNew != null) localBoneWeights[uniTriCount] = boneWeightsNew[vertIndex];
                        }

                        uniTriCount++;
                    }
                }
            }

            return new MeshData(localVerts, localNormals, localTangents, localUv, localUv2,
                localColors, localBoneWeights, originalBindPoses, resultingSubMeshes, subMeshes);
        }
        #endregion

        #region Helper Functions
        List<Material> sharedMaterials = new List<Material>();
        /// <summary>
        /// When a mesh is split, it's possible some of the original materials aren't on both sides of the split. 
        /// This function removes those unused materials, but also adds a new capMaterial for the newly created cap submesh.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="subMehses"></param>
        /// <param name="capMaterial"></param>
        public void RemoveUnusedMaterials(Renderer renderer, List<int> subMehses, Material capMaterial)
        {
            int removeIndex = 0;
            renderer.GetSharedMaterials(sharedMaterials);
            for (int i = 0; i < sharedMaterials.Count; i++, removeIndex++)
            {
                if (i >= subMehses.Count || subMehses[i] == 0)
                {
                    sharedMaterials.RemoveAt(removeIndex);
                    removeIndex--;
                }
            }

            if (subMehses[subMehses.Count - 1] > 0)
                sharedMaterials.Add(capMaterial);

            renderer.sharedMaterials = sharedMaterials.ToArray();
            sharedMaterials.Clear();
        }

        /// <summary>
        /// Attempts to generate physics for split objects.
        /// </summary>
        protected void TryGeneratePhysics(GameObject posObject, GameObject negObject, Mesh posMesh, Mesh negMesh, float splitForce)
        {
            if (negObject.TryGetComponent(out Rigidbody originalRb)) //checking the neg object here because this is the original object.
            {
                float originalMass = originalRb.mass;
                Vector3 originalBounds = originalMesh.bounds.size;
                float originalVolume = originalBounds.x * originalBounds.y * originalBounds.z;

                Rigidbody posBody = GeneratePhysics(posObject, posMesh, originalRb, originalMass, originalVolume);
                Rigidbody negBody = GeneratePhysics(negObject, negMesh, originalRb, originalMass, originalVolume);

                if (splitForce > 0)
                {
                    posBody.AddForce(plane.normal * posBody.mass * splitForce, ForceMode.Impulse);
                    negBody.AddForce(plane.normal * negBody.mass * -splitForce, ForceMode.Impulse);
                }
            }
        }
        /// <summary>
        /// Generates physics for split objects. 
        /// </summary>
        private Rigidbody GeneratePhysics(GameObject newObject, Mesh newMesh, Rigidbody originalRb, float originalMass, float originalVolume)
        {
            Rigidbody newBody = newObject.GetComponent<Rigidbody>();

            Vector3 newMeshSize = newMesh.bounds.size;
            float meshVolume = newMeshSize.x * newMeshSize.y * newMeshSize.z;
            float newMass = originalMass * (meshVolume / originalVolume);

            newBody.isKinematic = originalRb.isKinematic;
            newBody.useGravity = originalRb.useGravity;
            newBody.mass = newMass;
            newBody.velocity = originalRb.velocity;
            newBody.angularVelocity = originalRb.angularVelocity;

            return newBody;
        }

        /// <summary>
        /// There needs to be at least one frame between the creation of the new mesh and the recalculating of the bounds. 
        /// </summary>
        /// <param name="gameobjects"></param>
        /// <returns></returns>
        public IEnumerator ResetCenterOfMassNextFrame(SplitResult gameobjects)
        {
            yield return null;

            if (gameobjects.posObject.TryGetComponent(out Rigidbody rb))
                rb.ResetCenterOfMass();
            if (gameobjects.negObject.TryGetComponent(out rb))
                rb.ResetCenterOfMass();
        }
        #endregion
    }
}
