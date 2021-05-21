using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using JL.Splitting;

namespace JL.Demo
{
    public class Sword : MonoBehaviour
    {
        [SerializeField] private Transform _basePosition;
        [SerializeField] private Transform _tipPosition;

        private Vector3 _startPoint0, _startPoint1;
        private Vector3 _endPoint0, _endPoint1;

        private SplittableSkinned _splittable;

        private void OnTriggerEnter(Collider other)
        {
            _startPoint0 = _basePosition.position;
            _startPoint1 = _tipPosition.position;
            _splittable = GetComponentInParentRecursive<SplittableSkinned>(other.transform);
        }

        private void OnTriggerExit(Collider other)
        {
            _endPoint0 = _basePosition.position;
            _endPoint1 = _tipPosition.position;

            if (_splittable == GetComponentInParentRecursive<SplittableSkinned>(other.transform))
            {
                TrySplitObject();
            }
        }

        private void TrySplitObject()
        {
            if (_splittable != null)
            {
                Stopwatch s = new Stopwatch();
                s.Start();

                //_splittable.Split(_startPoint0, _startPoint1, _endPoint0, _endPoint1);

                s.Stop();
                UnityEngine.Debug.Log(s.ElapsedMilliseconds);
                _splittable = null;
            }
        }

        public T GetComponentInParentRecursive<T>(Transform target) where T : Component
        {
            if (target.TryGetComponent(out T component))
            {
                return component;
            }
            else
            {
                if (target.parent != null)
                {
                    return GetComponentInParentRecursive<T>(target.parent);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
