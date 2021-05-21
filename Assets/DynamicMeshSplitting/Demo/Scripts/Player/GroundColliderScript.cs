using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Demo
{

    public class GroundColliderScript : MonoBehaviour
    {

        private List<Collider> _col = new List<Collider>();

        public bool isGrounded()
        {
            return _col.Count > 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger && other.gameObject != transform.parent.gameObject)
                _col.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_col.Contains(other))
                _col.Remove(other);
        }
    }
}
