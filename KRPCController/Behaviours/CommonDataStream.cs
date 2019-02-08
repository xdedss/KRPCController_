using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.KRPC;
using System;
using System.Collections;
using System.Collections.Generic;
using Toe;
using KRPC.Client;
using System.Linq.Expressions;

namespace KRPCController.Behaviours
{
    /// <summary>
    /// 用于获取每一帧都需要获取的数据；
    /// 便于优化通讯效率；
    /// 取值时会自动建立一个流；
    /// 长时间没有获取值时已有的流会自动关闭；
    /// </summary>

    class CommonDataStream : Behaviour
    {
        public static int GetCount = 0;

        Vector3[] storedRCS;
        float storedRCSidle = 0;
        //public Vector3[] GetAvailableTorque()
        //{
        //    storedRCSidle = 0;
        //    var torque1 = vessel.AvailableReactionWheelTorque;
        //    var torque2 = vessel.AvailableEngineTorque;
        //    var torque3 = vessel.AvailableControlSurfaceTorque;
        //    var torque4 = vessel.AvailableOtherTorque;
        //    return new Vector3[] {torque1.Item1.ToVec() + torque2.Item1.ToVec() - torque3.Item1.ToVec() + torque4.Item1.ToVec() + storedRCS[0],
        //        torque1.Item2.ToVec() + torque2.Item2.ToVec() - torque3.Item2.ToVec() + torque4.Item2.ToVec() + storedRCS[1] };
        //}
        

        IEnumerator FetchRCS()
        {
            while (true)
            {
                if (storedRCSidle < 10)
                {
                    storedRCS = vessel.GetRCSTorque();
                }
                else
                {
                    Log("RCS skipped");
                }
                yield return new WaitForSeconds(15);
            }
        }

        Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double, double>>> rotStream = new Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double, double>>>();
        Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double>>> posStream = new Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double>>>();
        Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double>>> avelStream = new Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double>>>();
        Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double>>> velStream = new Dictionary<ReferenceFrame, TimedStream<Tuple<double, double, double>>>();
        Dictionary<string, TimedStream<double>> otherStream1 = new Dictionary<string, TimedStream<double>>();
        Dictionary<string, TimedStream<float>> otherStream1f = new Dictionary<string, TimedStream<float>>();
        Dictionary<string, TimedStream<Tuple<double, double, double>>> otherStream3 = new Dictionary<string, TimedStream<Tuple<double, double, double>>>();
        Dictionary<string, TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>> otherStream3_3 = new Dictionary<string, TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>>();

        public class TimedStream<T>
        {
            Stream<T> stream;
            T value;
            public float idleTime;
            public float maxIdleTime;
            public TimedStream(Stream<T> stream, float maxIdleTime = 5)//adjustable
            {
                //stream.Rate = 20;
                this.stream = stream;
                this.maxIdleTime = maxIdleTime;
                value = stream.Get();
                idleTime = 0;
            }
            public T Get()
            {
                GetCount++;
                idleTime = 0;
                return value;
            }
            public bool Check()
            {
                value = stream.Get();
                idleTime += (float)Time.deltaTime;
                return idleTime > maxIdleTime;
            }
            public void Close()
            {
                stream.Remove();
            }
        }

        public Vector3 GetPosition(ReferenceFrame reference)
        {
            if (!posStream.ContainsKey(reference))
            {
                var stream = new TimedStream<Tuple<double, double, double>>(connection.AddStream(() => vessel.Position(reference)));
                posStream.Add(reference, stream);
                Log("StreamAdded : " + vessel.id + " + " + reference.id + "pos");
            }
            return posStream[reference].Get().ToVec();
        }
        public Vector3 GetVelocity(ReferenceFrame reference)
        {
            if (!velStream.ContainsKey(reference))
            {
                var stream = new TimedStream<Tuple<double, double, double>>(connection.AddStream(() => vessel.Velocity(reference)));
                velStream.Add(reference, stream);
                Log("StreamAdded : " + vessel.id + " + " + reference.id + "vel");
            }
            return velStream[reference].Get().ToVec();
        }
        public Vector3 GetAngularVelocity(ReferenceFrame reference)
        {
            if (!avelStream.ContainsKey(reference))
            {
                var stream = new TimedStream<Tuple<double, double, double>>(connection.AddStream(() => vessel.AngularVelocity(reference)));
                avelStream.Add(reference, stream);
                Log("StreamAdded : " + vessel.id + " + " + reference.id + "avel");
            }
            return avelStream[reference].Get().ToVec();
        }
        public Quaternion GetRotation(ReferenceFrame reference)
        {
            if (!rotStream.ContainsKey(reference))
            {
                var stream = new TimedStream<Tuple<double, double, double, double>>(connection.AddStream(() => vessel.Flight(reference).Rotation));
                rotStream.Add(reference, stream);
                Log("StreamAdded : " + vessel.id + " + " + reference.id + "rot");
            }
            return rotStream[reference].Get().ToQuat();
        }

        public Vector3[] GetLocalBoundingBox()
        {
            var key = "localBoundingBox";
            if (!otherStream3_3.ContainsKey(key))
            {
                var stream = new TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>(connection.AddStream(() => vessel.BoundingBox(vessel.ReferenceFrame)));
                otherStream3_3.Add(key, stream);
            }
            return otherStream3_3[key].Get().ToVec();
        }
        public Vector3[] GetSurfaceBoundingBox()
        {
            var key = "surfaceBoundingBox";
            if (!otherStream3_3.ContainsKey(key))
            {
                var stream = new TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>(connection.AddStream(() => vessel.BoundingBox(vessel.SurfaceReferenceFrame)));
                otherStream3_3.Add(key, stream);
            }
            return otherStream3_3[key].Get().ToVec();
        }

        public Vector3 GetInteria()
        {
            var key = "interia";
            if (!otherStream3.ContainsKey(key))
            {
                var stream = new TimedStream<Tuple<double, double, double>>(connection.AddStream(() => vessel.MomentOfInertia));
                otherStream3.Add(key, stream);
            }
            return otherStream3[key].Get().ToVec();
        }

        public Vector3[] GetTorque()
        {
            storedRCSidle = 0;
            var keys = new string[] { "reactionTorque", "surfaceTorque", "engineTorque", "otherTorque" };
            if (!otherStream3_3.ContainsKey(keys[0])) otherStream3_3.Add(keys[0], new TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>(connection.AddStream(() => vessel.AvailableReactionWheelTorque)));
            if (!otherStream3_3.ContainsKey(keys[1])) otherStream3_3.Add(keys[1], new TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>(connection.AddStream(() => vessel.AvailableControlSurfaceTorque)));
            if (!otherStream3_3.ContainsKey(keys[2])) otherStream3_3.Add(keys[2], new TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>(connection.AddStream(() => vessel.AvailableEngineTorque)));
            if (!otherStream3_3.ContainsKey(keys[3])) otherStream3_3.Add(keys[3], new TimedStream<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>(connection.AddStream(() => vessel.AvailableOtherTorque)));
            var t0 = otherStream3_3[keys[0]].Get().ToVec();
            var t1 = otherStream3_3[keys[1]].Get().ToVec();
            var t2 = otherStream3_3[keys[2]].Get().ToVec();
            var t3 = otherStream3_3[keys[3]].Get().ToVec();
            return new Vector3[]
            {
                t0[0] + t1[0] * Math.Sign(t1[0].X) + t2[0] + t3[0] + storedRCS[0],
                t0[1] - t1[1] * Math.Sign(t1[1].X) + t2[1] + t3[1] + storedRCS[1],
            };
        }

        public float GetMeanAlt()
        {
            var key = "meanAlt";
            if (!otherStream1.ContainsKey(key))
            {
                var stream = new TimedStream<double>(connection.AddStream(() => vessel.Flight(vessel.ReferenceFrame).MeanAltitude));
                otherStream1.Add(key, stream);
            }
            return (float)otherStream1[key].Get();
        }
        public float GetBedrockAlt()
        {
            var key = "bedrockAlt";
            if (!otherStream1.ContainsKey(key))
            {
                var stream = new TimedStream<double>(connection.AddStream(() => vessel.Flight(vessel.ReferenceFrame).BedrockAltitude));
                otherStream1.Add(key, stream);
            }
            return (float)otherStream1[key].Get();
        }
        public float GetSurfaceAlt()
        {
            var key = "surfaceAlt";
            if (!otherStream1.ContainsKey(key))
            {
                var stream = new TimedStream<double>(connection.AddStream(() => vessel.Flight(vessel.ReferenceFrame).SurfaceAltitude));
                otherStream1.Add(key, stream);
            }
            return (float)otherStream1[key].Get();
        }
        public float GetMaxThrust()
        {
            var key = "maxThrust";
            if (!otherStream1f.ContainsKey(key))
            {
                var stream = new TimedStream<float>(connection.AddStream(() => vessel.MaxThrust));
                otherStream1f.Add(key, stream);
            }
            return otherStream1f[key].Get();
        }
        public float GetMass()
        {
            var key = "mass";
            if (!otherStream1f.ContainsKey(key))
            {
                var stream = new TimedStream<float>(connection.AddStream(() => vessel.Mass));
                otherStream1f.Add(key, stream);
            }
            return otherStream1f[key].Get();
        }

        void UpdateDic<TRef, T>(Dictionary<TRef, TimedStream<T>> dic)
        {
            var toRemove = new List<TRef>();
            foreach(var pair in dic)
            {
                var stream = pair.Value;
                if (stream.Check())
                {
                    if(pair.Key is ReferenceFrame)
                    {
                        var r = pair.Key as ReferenceFrame;
                        Log("StreamClosed : " + vessel.id + " + " + r.id);
                    }
                    stream.Close();
                    toRemove.Add(pair.Key);
                }
            }
            foreach(var reference in toRemove)
            {
                dic.Remove(reference);
            }
        }

        public override void Start()
        {
            storedRCS = vessel.GetRCSTorque();
            StartCoroutine(FetchRCS());
            Log("data stream started");
        }

        public override void Update()
        {

        }

        public override void EarlyUpdate()
        {
            storedRCSidle += (float)Time.deltaTime;
            UpdateDic(rotStream);
            UpdateDic(velStream);
            UpdateDic(posStream);
            UpdateDic(avelStream);
            UpdateDic(otherStream1);
            UpdateDic(otherStream1f);
            UpdateDic(otherStream3);
            UpdateDic(otherStream3_3);

            GetCount = 0;
        }

        public override void LateUpdate()
        {
            LogInfo("dataGetCount", GetCount.ToString());
            LogInfo("streamCount", (posStream.Count + rotStream.Count + velStream.Count + avelStream.Count + otherStream1.Count + otherStream1f.Count + otherStream3.Count + otherStream3_3.Count).ToString());
        }
    }
}
