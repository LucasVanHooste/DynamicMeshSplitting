using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    public abstract class SplittableBase : MonoBehaviour
    {
        [Header("Cap data")]
        [Tooltip("The material that will be applied to the cap created by the splitting")]
        public Material CapMaterial = null;
        [Tooltip("The minimum x and y bounds of the cap's UV coordinates.")]
        public Vector2 CapUVMin = Vector2.zero;
        [Tooltip("The maximum x and y bounds of the cap's UV coordinates.")] 
        public Vector2 CapUVMax = Vector2.one;

        [Header("Splitting data")]
        [Tooltip("This number is multipled with the plane's normal to make the split meshes move away from the cut.")] 
        public float SplitForce = 0;
        [Tooltip("Should the targetSkinnedMeshRenderer sharedMesh be destroyed when the mesh is split?\n" +
    "This should only be done when the mesh has been created trough code.")]
        public bool DestroyOriginalMeshWhenSplit = false;

        /// <summary>
        /// Splits the target mesh and duplicates this component's GameObject to make it appear as if it was split.
        /// </summary>
        /// <param name="plane">The plane along which the target mesh is split.</param>
        /// <returns>A SplitResult containing the resulting objects. For more info, see SplitResult.</returns>
        public abstract SplitResult Split(PointPlane plane);
        /// <summary>
        /// Same as Split() but runs asynchronously, and the Splitresult is returned in a callback.
        /// </summary>
        public abstract void SplitAsync(PointPlane plane, Action<SplitResult> callback = null);
    }

    /// <summary>
    /// This struct holds the resulting objects that are created after an object has been split.
    /// It's possible that the object which holds the mesh is not the root object which holds the Splittable component.
    /// This is why the mesh gameObjects are included as seperate fields.
    /// </summary>
    public struct SplitResult
    {
        public GameObject posObject;
        public GameObject negObject;
        public GameObject posMeshObject;
        public GameObject negMeshObject;

        public SplitResult(GameObject posObject, GameObject negObject, GameObject posMeshObject, GameObject negMeshObject)
        {
            this.posObject = posObject;
            this.negObject = negObject;
            this.posMeshObject = posMeshObject;
            this.negMeshObject = negMeshObject;
        }
    }
}