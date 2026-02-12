using System.Runtime.InteropServices;
using UnityEngine;

namespace JellyGunner
{
    [StructLayout(LayoutKind.Sequential)]
    public struct JellyInstanceData
    {
        public Matrix4x4 objectToWorld;
        public Matrix4x4 worldToObject;
        public Vector4 color;
        public float deformImpact;
        public float hpNormalized;
        public float deathProgress;
        public float highlightPulse;

        public static int Stride => Marshal.SizeOf<JellyInstanceData>();

        public static JellyInstanceData Create(Vector3 position, Quaternion rotation, Vector3 scale, Vector4 color)
        {
            var mat = Matrix4x4.TRS(position, rotation, scale);
            return new JellyInstanceData
            {
                objectToWorld = mat,
                worldToObject = mat.inverse,
                color = color,
                deformImpact = 0f,
                hpNormalized = 1f,
                deathProgress = 0f,
                highlightPulse = 0f
            };
        }

        public void SetTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var mat = Matrix4x4.TRS(position, rotation, scale);
            objectToWorld = mat;
            worldToObject = mat.inverse;
        }
    }
}
