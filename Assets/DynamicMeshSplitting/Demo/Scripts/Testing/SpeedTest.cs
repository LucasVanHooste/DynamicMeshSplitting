using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using JL.Splitting;

namespace JL.Demo
{
    public class SpeedTest : MonoBehaviour
    {
        List<Vector3> verticesNew;
        List<Vector3> normalsNew;
        List<Vector4> tangentsNew;

        private int[] currentTriangle = new int[2];
        int count;
        Vector3[] originalVertices;
        Vector3[] originalNormals;
        Vector4[] originalTangents;
        // Start is called before the first frame update
        void Start()
        {
            count = 10000000;
            originalVertices = new Vector3[count];
            originalNormals = new Vector3[count];
            originalTangents = new Vector4[count];

            verticesNew = new List<Vector3>(count);
            normalsNew = new List<Vector3>(count);
            tangentsNew = new List<Vector4>(count);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetButtonDown("Jump"))
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                PointPlane plane = new PointPlane(Vector3.zero, Quaternion.identity);
                for (int i = 0; i < count; i += 2)
                {
                    for (int k = 0; k < 3; k++)
                        LineIntersect(Vector3.up, Vector3.down, plane);
                }

                s.Stop();
                UnityEngine.Debug.Log(s.ElapsedMilliseconds);

                s.Reset();
                s.Start();
                float total = 0;
                float absTotal = 0;
                for (int i = 0; i < count; i += 2)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float j = Vector3.Dot(plane.normal, Vector3.right);
                        total += j;
                        absTotal += Mathf.Abs(total);
                        absTotal = 0;
                    }

                }

                s.Stop();
                UnityEngine.Debug.Log(s.ElapsedMilliseconds);

                s.Reset();
                s.Start();
                float[] dots = new float[3];

                for (int i = 0; i < count; i += 2)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        dots[k] = Vector3.Dot(plane.normal, plane.normal - plane.point);
                        //total += dots[k];
                        //absTotal += Mathf.Abs(dots[k]);
                    }

                    total = (dots[0] * dots[1]) * (dots[1] * dots[2]) * (dots[0] * dots[2]);
                }


                s.Stop();
                UnityEngine.Debug.Log(s.ElapsedMilliseconds);
                UnityEngine.Debug.Log(total);
            }
        }

        private float LineIntersect(Vector3 lineStart, Vector3 lineEnd, PointPlane plane)
        {
            return Vector3.Dot(plane.normal, plane.point - lineStart) / Vector3.Dot(plane.normal, lineEnd - lineStart);
        }
    }
}
