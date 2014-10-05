
using System;
using System.Windows;
using Microsoft.Kinect;


namespace FiveTwoFiveTwo.KinectMathHelpers
{

    public static class MathHelpers
    {
        public struct Vector3
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public static Vector3 Zero
            {
                get
                {
                    return new Vector3() { X = 0, Y = 0, Z = 0 };
                }
            }
        }
        /// <summary> 
        /// The function converts a Quaternion into a Vector3 
        /// </summary> 
        /// <param name="q">The Quaternion to convert</param> 
        /// <returns>An equivalent Vector3</returns> 
        /// <remarks> 
        /// This function was extrapolated by reading the work of Martin John Baker. 
        /// All credit for this function goes to Martin John. 
        /// http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/index.htm 
        /// </remarks> 
        public static Vector3 QuaternionToEuler(Vector4 q)
        {
            Vector3 v = Vector3.Zero;
            v.X = Math.Atan2(2 * q.Y * q.W - 2 * q.X * q.Z,
                                    1 - 2 * Math.Pow(q.Y, 2) - 2 * Math.Pow(q.Z, 2));

            v.Z = Math.Asin(2 * q.X * q.Y + 2 * q.Z * q.W);

            v.Y = Math.Atan2(2 * q.X * q.W - 2 * q.Y * q.Z,
                                      1 - 2 * Math.Pow(q.X, 2) - 2 * Math.Pow(q.Z, 2));

            if (q.X * q.Y + q.Z * q.W == 0.5)
            {
                v.X = (2 * Math.Atan2(q.X, q.W));
                v.Y = 0;
            }
            else if (q.X * q.Y + q.Z * q.W == -0.5)
            {
                v.X = (-2 * Math.Atan2(q.X, q.W));
                v.Y = 0;
            }

            v.X = RadianToDegree(v.X);
            v.Y = RadianToDegree(v.Y);
            v.Z = RadianToDegree(v.Z);
            return v;
        }

        /// <summary>
        /// Converts a Vector4 quaternion to a Vector3 CameraSpacePoint.
        /// </summary>
        /// <param name="orientation">The Vector4 quaternion.</param>
        /// <returns>A Vector3 representation of the quaternion.</returns>
        public static CameraSpacePoint QuaternionToEuler3(this Vector4 orientation)
        {
            CameraSpacePoint point = new CameraSpacePoint();

            point.X = (float)Math.Atan2
            (
                2 * orientation.Y * orientation.W - 2 * orientation.X * orientation.Z,
                1 - 2 * Math.Pow(orientation.Y, 2) - 2 * Math.Pow(orientation.Z, 2)
            );

            point.Y = (float)Math.Asin
            (
                2 * orientation.X * orientation.Y + 2 * orientation.Z * orientation.W
            );

            point.Z = (float)Math.Atan2
            (
                2 * orientation.X * orientation.W - 2 * orientation.Y * orientation.Z,
                1 - 2 * Math.Pow(orientation.X, 2) - 2 * Math.Pow(orientation.Z, 2)
            );

            if (orientation.X * orientation.Y + orientation.Z * orientation.W == 0.5)
            {
                point.X = (float)(2 * Math.Atan2(orientation.X, orientation.W));
                point.Z = 0;
            }

            else if (orientation.X * orientation.Y + orientation.Z * orientation.W == -0.5)
            {
                point.X = (float)(-2 * Math.Atan2(orientation.X, orientation.W));
                point.Z = 0;
            }

            point.X = (float)RadianToDegree(point.X);
            point.Y = (float)RadianToDegree(point.Y);
            point.Z = (float)RadianToDegree(point.Z);
            return point;
        }


        private static double RadianToDegree(double angle)
        {//Return degrees (0->360) from radians
            return angle * (180.0 / Math.PI) + 180;
        }
        public static Vector3 QuaternionToYawPitchRoll(Vector4 q)
        {
            const double Epsilon = 0.0009765625f;
            const double Threshold = 0.5f - Epsilon;

            double yaw;
            double pitch;
            double roll;

            double XY = q.X * q.Y;
            double ZW = q.Z * q.W;

            double TEST = XY + ZW;

            if (TEST < -Threshold || TEST > Threshold)
            {

                int sign = Math.Sign(TEST);

                yaw = sign * 2 * (double)Math.Atan2(q.X, q.W);

                pitch = sign * (Math.PI / 2.0d);

                roll = 0;

            }
            else
            {

                double XX = q.X * q.X;
                double XZ = q.X * q.Z;
                double XW = q.X * q.W;

                double YY = q.Y * q.Y;
                double YW = q.Y * q.W;
                double YZ = q.Y * q.Z;

                double ZZ = q.Z * q.Z;

                yaw = (double)Math.Atan2(2 * YW - 2 * XZ, 1 - 2 * YY - 2 * ZZ);

                pitch = (double)Math.Atan2(2 * XW - 2 * YZ, 1 - 2 * XX - 2 * ZZ);

                roll = (double)Math.Asin(2 * TEST);

            }//if 

            return new Vector3() { X = yaw, Y = pitch, Z = roll };

        }//method 
        public static double AngleBetweenPoints(Point p1, Point p2)
        {
            double retval;
            double xDiff = p1.X - p2.X;
            double yDiff = p1.Y - p2.Y;
            retval = (double)Math.Atan2(yDiff, xDiff) * (double)(180 / Math.PI);
            return retval;
        }


    }
    public class Quaternion
    {
        public Quaternion(float w_, float xi, float yj, float zk)
        {
            w = w_;
            x = xi;
            y = yj;
            z = zk;
        }
        private float w;
        public float W
        {
            set
            { w = value; }
            get
            { return w; }
        }
        private float x;
        public float X
        {
            set { x = value; }
            get { return x; }
        }
        private float y;
        public float Y
        {
            set { y = value; }
            get { return y; }
        }
        private float z;
        public float Z
        {
            set { z = value; }
            get { return z; }
        }
        /// <summary>
        /// Euclidean norm
        /// </summary>
        public float Norm
        {
            get { return (float)Math.Sqrt(w * w + x * x + y * y + z * z); }
        }
        /// <summary>
        /// Conjugate
        /// </summary>
        public Quaternion Conj
        {
            get { return new Quaternion(w, -x, -y, -z); }
        }
        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.W + q2.W, q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z);
        }
        public static Quaternion operator -(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.W - q2.W, q1.X - q2.X, q1.Y - q2.Y, q1.Z - q2.Z);
        }
        /// <summary>
        /// product of two quaterions
        /// </summary>
        /// <param name="q1">Quaternion1
        /// <param name="q2">Quaternion2
        /// <returns>Quaternion1*Quaternion2</returns>
        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
                , q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y
                , q1.w * q2.y + q1.y * q2.w + q1.z * q2.x - q1.x * q2.z
                , q1.w * q2.z + q1.z * q2.w + q1.x * q2.y - q1.y * q2.x);
        }
        public static Quaternion operator *(float f, Quaternion q)
        {
            return new Quaternion(f * q.w, f * q.x, f * q.y, f * q.z);
        }
        public static Quaternion operator *(Quaternion q, float f)
        {
            return new Quaternion(f * q.w, f * q.x, f * q.y, f * q.z);
        }
        public static Quaternion operator /(Quaternion q, float f)
        {
            if (f == 0.0f) { throw new DivideByZeroException(); }
            return new Quaternion(1 / f * q.w, 1 / f * q.x, 1 / f * q.y, 1 / f * q.z);
        }
        public static Quaternion operator /(float f, Quaternion q)
        {
            if (q.Norm == 0.0f) { throw new DivideByZeroException(); }
            return f / (q.Norm * q.Norm) * q.Conj;
        }
        public static Quaternion operator /(Quaternion q1, Quaternion q2)
        {
            return q1 * q2.Conj / (q2.Norm * q2.Norm);
        }
        public static bool operator ==(Quaternion q1, Quaternion q2)
        {
            if (Math.Abs(q1.w - q2.w) < 0.00001f
                && Math.Abs(q1.x - q2.x) < 0.00001f
                && Math.Abs(q1.y - q2.y) < 0.00001f
                && Math.Abs(q1.z - q2.z) < 0.00001f)
            {
                return true;
            }
            return false;
        }
        public static bool operator !=(Quaternion q1, Quaternion q2)
        {
            if (q1.w == q2.w && q1.x == q2.x && q1.y == q2.y && q1.z == q2.z)
            {
                return false;
            }
            return true;
        }
        public override bool Equals(object obj)
        {
            Quaternion q = obj as Quaternion;
            return q == this;
        }
        public override int GetHashCode()
        {
            return this.w.GetHashCode() ^ (this.x.GetHashCode() * this.y.GetHashCode() * this.z.GetHashCode());
        }
        public float[] Rotate(float x1, float y1, float z1)
        {
            Quaternion q = new Quaternion(0.0f, x1, y1, z1);
            Quaternion r = this * q * this.Conj;
            return new float[3] { r.X, r.Y, r.Z };
        }
    }

    public static class KinectHelpers
    {
        // returns the parent joint of the given joint
        public static JointType GetParentJoint(JointType joint)
        {
            switch (joint)
            {
                case JointType.SpineBase:
                    return JointType.SpineBase;

                case JointType.Neck:
                    return JointType.SpineShoulder;

                case JointType.SpineShoulder:
                    return JointType.SpineBase;

                case JointType.ShoulderLeft:
                case JointType.ShoulderRight:
                    return JointType.SpineShoulder;

                case JointType.HipLeft:
                case JointType.HipRight:
                    return JointType.SpineBase;

                case JointType.HandTipLeft:
                case JointType.ThumbLeft:
                    return JointType.HandLeft;

                case JointType.HandTipRight:
                case JointType.ThumbRight:
                    return JointType.HandRight;
            }

            return (JointType)((int)joint - 1);
        }
    }
}



