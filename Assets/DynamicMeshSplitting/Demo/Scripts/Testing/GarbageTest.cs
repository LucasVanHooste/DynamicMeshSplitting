using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Demo
{
    public class GarbageTest : MonoBehaviour
    {
        private struct Test
        {
            public Vector3 vector;
            public int extra;
        }
        public int size;
        private Vector3[] vectorArray;
        private Test[] testArray;

        // Start is called before the first frame update
        void Start()
        {
            vectorArray = new Vector3[size];
            testArray = new Test[size];
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                vectorArray = new Vector3[size];
                GC.Collect();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                testArray = new Test[size];
                GC.Collect();
            }
        }
    }

}