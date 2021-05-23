using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using JL.Splitting;

namespace JL.Demo
{
    [RequireComponent(typeof(PhysicsController))]
    public class PlayerScript : MonoBehaviour
    {
        private Transform _transform;
        private PhysicsController _physicsController;
        [SerializeField] private GameObject _onScreenUI = null;
        [SerializeField] private Camera _camera = null;
        [SerializeField] private RectTransform _selector = null;
        [SerializeField] private Transform _grabPosition = null;
        [SerializeField] private Transform _planeTransform = null;
        [SerializeField] private float _rotationSpeed = 120;
        [SerializeField] private RectTransform _planeUI = null;
        [Space]
        [SerializeField] private bool _splitAsync = true;

        private Transform _grabObject;
        private Rigidbody _grabObjectRigidbody;
        private Vector3 _grabPositionOnObject;

        public static PlayerScript Player { get; private set; }

        private void Awake()
        {
            Player = this;
            Screen.fullScreen = false;
            _onScreenUI.SetActive(true);
        }

        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _transform = transform;
            _physicsController = GetComponent<PhysicsController>();
        }

        private void Update()
        {
            _physicsController.Movement = new Vector3(InputController.RunXAxis, 0, InputController.RunYAxis);
            _physicsController.Aim = new Vector3(InputController.LookXAxis, 0, InputController.LookYAxis);
            if (InputController.JumpButtonDown && _physicsController.IsGrounded())
            {
                _physicsController.Jump = true;
            }

            RotatePlane();

            if (InputController.SplitButtonDown)
            {
                TrySplitObject();
            }

            InteractWithObject();
        }

        private void RotatePlane()
        {
            Vector3 rotation = new Vector3(0, 0, _rotationSpeed * Time.deltaTime);

            if (Input.GetKey(KeyCode.Z))
            {
                _planeTransform.localEulerAngles += rotation;
                _planeUI.localEulerAngles += rotation;
            }
            if (Input.GetKey(KeyCode.X))
            {
                _planeTransform.localEulerAngles -= rotation;
                _planeUI.localEulerAngles -= rotation;
            }
        }

        private void TrySplitObject()
        {
            RaycastHit hit;
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, 100f))
            {
                SplittableBase splittable = hit.transform.GetComponentInParentRecursive<SplittableBase>();
                if (splittable != null)
                {
                    SplitObject(splittable);
                }
            }
        }

        private void SplitObject(SplittableBase splittable)
        {
            Stopwatch s = new Stopwatch();
            s.Start();

            PointPlane plane = new PointPlane(_planeTransform.position, _planeTransform.rotation);
            if (_splitAsync)
                splittable.SplitAsync(plane, null);
            else
                splittable.Split(plane);

            s.Stop();
            LogWrapper.Log("Elapsed time on main thread (ms): " + s.ElapsedMilliseconds);
        }


        private void InteractWithObject()
        {
            if (InputController.InteractButtonDown)
            {
                RaycastHit hit;
                if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, 100f))
                {
                    if (hit.transform.TryGetComponent(out Rigidbody rb))
                    {
                        _grabPosition.position = hit.point;
                        _grabObject = hit.transform;
                        _grabPositionOnObject = _grabObject.InverseTransformPoint(hit.point);

                        _grabObjectRigidbody = rb;
                        _grabObjectRigidbody.useGravity = false;
                    }
                }
            }
            if (InputController.InteractButtonUp)
            {
                _grabObject = null;
                if (_grabObjectRigidbody)
                    _grabObjectRigidbody.useGravity = true;
                _grabObjectRigidbody = null;
            }
            if (InputController.InteractButton)
            {
                if (_grabObject)
                {
                    Vector3 globalGrapPosition = _grabObject.TransformPoint(_grabPositionOnObject);
                    _grabObject.position = _grabPosition.position + (_grabObject.position - globalGrapPosition);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward);
        }
    }
}
