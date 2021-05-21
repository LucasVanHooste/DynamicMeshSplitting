using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Demo
{
    public static class Extensions
    {
        public static T GetComponentInParentRecursive<T>(this Component component) where T : Component
        {
            if (component.TryGetComponent<T>(out T comp))
            {
                return comp;
            }
            else
            {
                if (component.transform.parent)
                {
                    return component.transform.parent.GetComponentInParentRecursive<T>();
                }
            }

            return null;
        }
    }

}