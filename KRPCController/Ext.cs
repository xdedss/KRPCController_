//各种扩展方法

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KRPC.Client;
using KRPC.Client.Services;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Toe;

namespace KRPCController
{
    static class Ext
    {
        public static T AddComponent<T>(this Vessel v) where T : Behaviour, new()
        {
            return Behaviour.Add<T>(v);
        }

        public static T GetComponent<T>(this Vessel v) where T : Behaviour, new()
        {
            return Behaviour.Find<T>(v);
        }

        public static T[] GetComponents<T>(this Vessel v) where T : Behaviour, new()
        {
            return Behaviour.FindAll<T>(v);
        }
        public static Vector3[] GetRCSTorque(this Vessel v)
        {
            var vRef = v.ReferenceFrame;
            var allRCS = v.Parts.RCS;
            float pp = 0;
            float pn = 0;
            float rp = 0;
            float rn = 0;
            float yp = 0;
            float yn = 0;
            foreach(var rcs in allRCS)
            {
                var thrusters = rcs.Thrusters;
                var thrust = rcs.MaxThrust;// / thrusters.Count;

                var pos = rcs.Part.Position(vRef).ToVec();
                var pitchL= Vector3.Cross(Vector3.UnitX, pos);
                var rollL = Vector3.Cross(Vector3.UnitY, pos);
                var yawL = Vector3.Cross(Vector3.UnitZ, pos);
                var pitchDir = pitchL.Normalized();
                var rollDir = rollL.Normalized();
                var yawDir = yawL.Normalized();
                foreach (var thruster in thrusters)
                {
                    var dir = thruster.ThrustDirection(vRef).ToVec().Normalized();
                    var F = dir * thrust;
                    var pRes = Math.Abs(Vector3.Dot(dir, pitchDir));
                    var rRes = Math.Abs(Vector3.Dot(dir, rollDir));
                    var yRes = Math.Abs(Vector3.Dot(dir, yawDir));
                    var totalRes = pRes + rRes + yRes;
                    var p = Vector3.Dot(pitchL, F);// * pRes;// / totalRes;
                    var r = Vector3.Dot(rollL, F);// * rRes;// / totalRes;
                    var y = Vector3.Dot(yawL, F);// * yRes;// / totalRes;
                    if (p > 0) pp += p; else pn += p;
                    if (r > 0) rp += r; else rn += r;
                    if (y > 0) yp += y; else yn += y;
                }
            }

            return new Vector3[]
            {
                new Vector3(pp, rp, yp),
                new Vector3(pn, rn, yn)
            };
        }

        public static double LongitudeAtPosition(this CelestialBody body, Vector3 position, ReferenceFrame referenceFrame)
        {
            return body.LongitudeAtPosition(position.ToTuple(), referenceFrame);
        }
        public static double LatitudeAtPosition(this CelestialBody body, Vector3 position, ReferenceFrame referenceFrame)
        {
            return body.LatitudeAtPosition(position.ToTuple(), referenceFrame);
        }
        public static double SrfHeightAtPosition(this CelestialBody body, Vector3 position, ReferenceFrame referenceFrame)
        {
            return body.SurfaceHeight(body.LatitudeAtPosition(position, referenceFrame), body.LongitudeAtPosition(position, referenceFrame));
        }
        public static double SrfAltitudeAtPosision(this CelestialBody body, Vector3 position, ReferenceFrame referenceFrame)
        {
            return body.AltitudeAtPosition(position.ToTuple(), referenceFrame) - body.SrfHeightAtPosition(position, referenceFrame);
        }

        public static double AltitudeAt(this Orbit orbit, double ut)
        {
            var pos = orbit.PositionAt(ut, orbit.Body.NonRotatingReferenceFrame);
            var alt = orbit.Body.AltitudeAtPosition(pos, orbit.Body.NonRotatingReferenceFrame);
            return alt;
        }
        

        public static string ToStr(this Tuple<double, double, double> tuple)
        {
            return string.Format("({0:0.000}, {1:0.000}, {2:0.000})", tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static string ToStr(this Tuple<double, double, double, double> tuple)
        {
            return string.Format("({0:0.000}, {1:0.000}, {2:0.000}, {3:0.000})", tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }

        public static Vector3 ToVec(this Tuple<double, double, double> tuple)
        {
            return new Vector3((float)tuple.Item1, (float)tuple.Item2, (float)tuple.Item3);
        }
        public static Tuple<double, double, double> ToTuple(this Vector3 v)
        {
            return new Tuple<double, double, double>(v.X, v.Y, v.Z);
        }
        public static Vector3[] ToVec(this Tuple<Tuple<double, double, double>, Tuple<double, double, double>> tuple)
        {
            return new Vector3[] { tuple.Item1.ToVec(), tuple.Item2.ToVec() };
        }

        public static Quaternion ToQuat(this Tuple<double, double, double, double> tuple)
        {
            return new Quaternion((float)tuple.Item1, (float)tuple.Item2, (float)tuple.Item3, (float)tuple.Item4);
        }
        public static Tuple<double, double, double, double> ToTuple(this Quaternion q)
        {
            return new Tuple<double, double, double, double>(q.X, q.Y, q.Z, q.W);
        }

        public static Vector3 MoveToward(this Vector3 v, Vector3 target, float maxD, float t)
        {
            var dif = target - v;
            var mag = dif.Length;
            if (mag == 0)
                return v;
            var move = Mathf.Clamp(mag * Mathf.Clamp01(t), 0, maxD);
            return v + dif / mag * move;
        }
        public static Vector3 Clamp(this Vector3 v, float maxMag)
        {
            maxMag = Math.Abs(maxMag);
            var mag = v.Length;
            if(mag <= maxMag)
            {
                return v;
            }
            return v.Normalized() * maxMag;
        }
        public static Vector2 Clamp(this Vector2 v, float maxMag)
        {
            maxMag = Math.Abs(maxMag);
            var mag = v.Length;
            if (mag <= maxMag)
            {
                return v;
            }
            return v.Normalized() * maxMag;
        }
    }


    static class Quat
    {
        public static Quaternion LookRotation(Vector3 forward, Vector3 up, Vector3 localForward, Vector3 localUp)
        {
            var look = Quaternion.LookRotation(forward, up);
            var lookLocal = Quaternion.LookRotation(localForward, localUp);
            //Info.AddInfo("forward", forward.ToString());
            return look * Quaternion.Invert(lookLocal);
        }

        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            return Quat.LookRotation(forward, up, new Vector3(0, 1, 0), new Vector3(0, 0, -1));
        }
    }

    static class Mathf
    {
        public const float Rad2Deg = 57.295779513082320876798154814105f;
        public const float Deg2Rad = 0.01745329251994329576923690768489f;
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float InverseLerp(float a, float b, float v)
        {
            return (v - a) / (b - a);
        }

        public static float Map(float a, float b, float toA, float toB, float v)
        {
            return Lerp(toA, toB, InverseLerp(a, b, v));
        }

        public static float Clamp(float v, float min, float max)
        {
            if(v < min)
            {
                return min;
            }
            if(v > max)
            {
                return max;
            }
            return v;
        }

        public static float Clamp01(float v)
        {
            return Clamp(v, 0, 1);
        }
    }
}
