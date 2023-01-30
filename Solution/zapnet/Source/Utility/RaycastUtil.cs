/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// Data representing an oriented bounding box hit.
    /// </summary>
    public struct HitInfoOBB
    {
        /// <summary>
        /// The distance between where the ray began and the hit position.
        /// </summary>
        public float distance;

        /// <summary>
        /// The position that the ray intersects the OBB.
        /// </summary>
        public Vector3 position;

        /// <summary>
        ///  The normal vector of the hit intersection.
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// Whether or not the ray did hit any OBB.
        /// </summary>
        public bool didHit;
    }

    /// <summary>
    /// A class representing an oriented bounding box.
    /// </summary>
    public class OBB
    {
        private Quaternion _rotation;
        private Vector3 _position;
        private Vector3 _halfSize;
        private Matrix4x4 _matrix;
        private bool _isValid;

        public Vector3 HalfSize
        {
            get
            {
                return _halfSize;
            }
        }

        public Matrix4x4 Matrix
        {
            get
            {
                return _matrix;
            }
        }

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }

        public void SetValues(Vector3 position, Quaternion rotation, Vector3 halfSize)
        {
            _matrix.SetTRS(position, rotation, Vector3.one);

            _isValid = _matrix.ValidTRS();
            _rotation = rotation;
            _position = position;
            _halfSize = halfSize;
        }

        public OBB()
        {
            _matrix = new Matrix4x4();
        }
    }

    /// <summary>
    /// A static class of utilities relating to raycasts.
    /// </summary>
    public static class RaycastUtil
    {
        /// <summary>
        /// Get whether a Ray intersects an OBB and pass out any hit data.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="obb"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public static bool OBB(Ray ray, OBB obb, out HitInfoOBB hitInfo)
        {
            var origin = obb.Matrix.inverse.MultiplyPoint(ray.origin);
            var direction = obb.Matrix.inverse.MultiplyVector(ray.direction).normalized;

            var min = (obb.HalfSize * -0.5f);
            var max = (obb.HalfSize * 0.5f);
            var aabbCenter = (min + max) * 0.5f;
            var hitNormal = Vector3.one.normalized;
            var hitPosition = Vector3.zero;

            hitInfo = new HitInfoOBB();

            float tmin, tmax, tymin, tymax, tzmin, tzmax;
            var invrd = direction;
            invrd.x = 1.0f / invrd.x;
            invrd.y = 1.0f / invrd.y;
            invrd.z = 1.0f / invrd.z;

            if (invrd.x >= 0.0f)
            {
                tmin = (min.x - origin.x) * invrd.x;
                tmax = (max.x - origin.x) * invrd.x;
            }
            else
            {
                tmin = (max.x - origin.x) * invrd.x;
                tmax = (min.x - origin.x) * invrd.x;
            }

            if (invrd.y >= 0.0f)
            {
                tymin = (min.y - origin.y) * invrd.y;
                tymax = (max.y - origin.y) * invrd.y;
            }
            else
            {
                tymin = (max.y - origin.y) * invrd.y;
                tymax = (min.y - origin.y) * invrd.y;
            }

            if ((tmin > tymax) || (tymin > tmax))
            {
                return false;
            }

            if (tymin > tmin) tmin = tymin;
            if (tymax < tmax) tmax = tymax;

            if (invrd.z >= 0.0f)
            {
                tzmin = (min.z - origin.z) * invrd.z;
                tzmax = (max.z - origin.z) * invrd.z;
            }
            else
            {
                tzmin = (max.z - origin.z) * invrd.z;
                tzmax = (min.z - origin.z) * invrd.z;
            }

            if ((tmin > tzmax) || (tzmin > tmax))
            {
                return false;
            }

            if (tzmin > tmin) tmin = tzmin;
            if (tzmax < tmax) tmax = tzmax;

            if (tmin < 0) tmin = tmax;
            if (tmax < 0)
            {
                return false;
            }

            var distance = tmin;
            hitPosition = origin + distance * direction;
            var normalDir = hitPosition - aabbCenter;

            var width = max - min;
            width.x = Mathf.Abs(width.x);
            width.y = Mathf.Abs(width.y);
            width.z = Mathf.Abs(width.z);

            var ratio = Vector3.one;
            ratio.x = Mathf.Abs(normalDir.x / width.x);
            ratio.y = Mathf.Abs(normalDir.y / width.y);
            ratio.z = Mathf.Abs(normalDir.z / width.z);

            hitNormal = Vector3.zero;
            int maxDir = 0;

            if (ratio.x >= ratio.y && ratio.x >= ratio.z)
            {
                maxDir = 0;
            }
            else if (ratio.y >= ratio.x && ratio.y >= ratio.z)
            {
                maxDir = 1;
            }
            else if (ratio.z >= ratio.x && ratio.z >= ratio.y)
            {
                maxDir = 2;
            }

            if (normalDir[maxDir] > 0)
            {
                hitNormal[maxDir] = 1.0f;
            }
            else
            {
                hitNormal[maxDir] = -1.0f;
            }

            hitInfo.distance = distance;
            hitInfo.position = obb.Matrix.MultiplyPoint(hitPosition);
            hitInfo.normal = obb.Matrix.MultiplyVector(hitNormal);
            hitInfo.didHit = true;

            return true;
        }
    }
}
