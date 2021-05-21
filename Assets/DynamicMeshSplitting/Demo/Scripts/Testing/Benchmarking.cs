using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using JL.Splitting;

namespace JL.Demo
{
    public class Benchmarking : MonoBehaviour
    {
        [Header("Benchmark Data")]
        public int amountOfLoops;
        public GameObject meshPrefab;

        private MeshFilter targetFilter;
        public Transform targetPlane;

        MeshSplitter meshSplitter;
        float total;

        [Header("Splitting data")]
        public float SplitForce;
        public bool GenerateColliders = false;

        public bool CustomUVRange = false;
        public Vector2 CapUVMin = Vector2.zero;
        public Vector2 CapUVMax = Vector2.one;
        public Material CapMaterial;

        void Update()
        {
            if (Input.GetButtonDown("Jump"))
            {
                for (int i = 0; i < amountOfLoops; i++)
                {
                    meshSplitter = new MeshSplitter(CapUVMin, CapUVMax);
                    GameObject targetObj = Instantiate(meshPrefab, Vector3.up * i * 2, Quaternion.identity);
                    targetFilter = targetObj.GetComponent<MeshFilter>();
                    targetPlane.position = targetObj.transform.position;

                    Stopwatch s = new Stopwatch();
                    s.Start();

                    MeshSplitData meshSplitData = meshSplitter.SplitMesh(targetFilter, new PointPlane(targetPlane.position, targetPlane.rotation));

                    s.Stop();
                    total += s.ElapsedMilliseconds;
                    UnityEngine.Debug.Log(s.ElapsedMilliseconds);
                }

                UnityEngine.Debug.Log("Average (ms): " + total / amountOfLoops);
            }
        }
    }
}
