using UnityEngine;

namespace JL.Splitting
{
    /// <summary>
    /// Representation of a plane, consisting out of a point, normal, but also an orientation.
    /// </summary>
    public struct PointPlane
    {
        public readonly Vector3 point;
        public readonly Vector3 normal;
        public readonly Quaternion rotation;

        public PointPlane(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            this.normal = normal;
        }

        public PointPlane(Vector3 point, Quaternion rotation)
        {
            this.point = point;
            this.rotation = rotation;
            this.normal = rotation * Vector3.up;
        }
    }
}
