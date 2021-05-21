using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JL.Demo
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] Material _standardMat;
        [SerializeField] Material _wireframeMat;

        private bool _wireframeToggled;
        private static string _splitLayer = "SplitMesh";
        private int _layerID;

        private void Awake()
        {
            _layerID = LayerMask.NameToLayer(_splitLayer);
        }

        // Update is called once per frame
        void Update()
        {
            if (InputController.RestartButtonDown)
            {
                Restart();
            }

            if (InputController.WireframeButtonDown)
            {
                ToggleWireframe();
            }
        }

        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToggleWireframe()
        {
            GameObject[] objects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in objects)
            {
                if (go.layer == _layerID)
                {
                    MeshRenderer mr = go.GetComponent<MeshRenderer>();
                    if (mr)
                    {
                        if (_wireframeToggled)
                        {
                            mr.material = _standardMat;
                        }
                        else
                        {
                            mr.material = _wireframeMat;
                        }
                    }
                }
            }

            _wireframeToggled = !_wireframeToggled;
        }
    }
}
