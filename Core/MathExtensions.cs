using GTA.Math;
using System;

namespace RageCoop.Core
{
    internal static class MathExtensions
    {
        public const float Deg2Rad = (float)(Math.PI * 2) / 360;
        public const float Rad2Deg = 360 / (float)(Math.PI * 2);

        public static Vector3 ToDirection(this Vector3 rotation)
        {
            double z = DegToRad(rotation.Z);
            double x = DegToRad(rotation.X);
            double num = Math.Abs(Math.Cos(x));

            return new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 ToVector(this Quaternion vec)
        {
            return new Vector3()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static Quaternion ToQuaternion(this Vector3 vec, float vW = 0.0f)
        {
            return new Quaternion()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = vW
            };
        }

        public static float Denormalize(this float h)
        {
            return h < 0f ? h + 360f : h;
        }

        public static float ToRadians(this float val)
        {
            return (float)(Math.PI / 180) * val;
        }

        public static Vector3 ToRadians(this Vector3 i)
        {
            return new Vector3()
            {
                X = ToRadians(i.X),
                Y = ToRadians(i.Y),
                Z = ToRadians(i.Z),
            };
        }

        public static Quaternion ToQuaternion(this Vector3 vect)
        {
            vect = new Vector3()
            {
                X = vect.X.Denormalize() * -1,
                Y = vect.Y.Denormalize() - 180f,
                Z = vect.Z.Denormalize() - 180f,
            };

            vect = vect.ToRadians();

            float rollOver2 = vect.Z * 0.5f;
            float sinRollOver2 = (float)Math.Sin(rollOver2);
            float cosRollOver2 = (float)Math.Cos(rollOver2);
            float pitchOver2 = vect.Y * 0.5f;
            float sinPitchOver2 = (float)Math.Sin(pitchOver2);
            float cosPitchOver2 = (float)Math.Cos(pitchOver2);
            float yawOver2 = vect.X * 0.5f; // pitch
            float sinYawOver2 = (float)Math.Sin(yawOver2);
            float cosYawOver2 = (float)Math.Cos(yawOver2);
            Quaternion result = new Quaternion()
            {
                X = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2,
                Y = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2,
                Z = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2,
                W = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2
            };
            return result;
        }
        public static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }
        public static Vector3 ToEulerRotation(this Vector3 dir, Vector3 up)
        {
            var rot = Quaternion.LookRotation(dir.Normalized, up).ToEulerAngles().ToDegree();
            return rot;

        }
        public static Vector3 ToDegree(this Vector3 radian)
        {
            return radian * (float)(180 / Math.PI);
        }
        public static Vector3 ToEulerDegrees(this Quaternion q)
        {
            return q.ToEulerAngles().ToDegree();
        }
        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            Vector3 angles = new Vector3();

            // roll / x
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.Y = CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
        private static float CopySign(double x, double y)
        {
            if (y >= 0)
            {
                return x >= 0 ? (float)x : (float)-x;
            }

            return x >= 0 ? (float)-x : (float)x;
        }
        public static double AngelTo(this Vector3 v1, Vector3 v2)
        {
            return Math.Acos(v1.GetCosTheta(v2));
        }
        public static float GetCosTheta(this Vector3 v1, Vector3 v2)
        {
            return Vector3.Dot(v1, v2) / (v1.Length() * v2.Length());
        }

    }
}
