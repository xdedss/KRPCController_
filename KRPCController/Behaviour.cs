//#define CATCH_EXCEPTION

using System;
using System.Collections;
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
    /// <summary>
    /// 所有Behaviour的基类（仿Unity MonoBehaviour）；必须实现Start() Update()
    /// </summary>
    abstract class Behaviour
    {
        public static List<Behaviour> behaviours = new List<Behaviour>();
        static List<Behaviour> behavioursAddQueue = new List<Behaviour>();
        static List<Behaviour> behavioursDelQueue = new List<Behaviour>();
        public static T Add<T>(Vessel vessel, Connection conn = null) where T : Behaviour, new()
        {
            var t = new T();
            t.connection = conn == null ? ConnectionInitializer.conn : conn;
            t.vessel = vessel;
            t.spaceCenter = conn.SpaceCenter();
            t.vid = vessel.id;
#if CATCH_EXCEPTION
                    try
                    {
#endif
            t.Start();
#if CATCH_EXCEPTION
        }
            catch (Exception e)
            {
                new Exception("Exception during Start() of " + t.GetType().ToString() + " : ", e);
            }
#endif
            behavioursAddQueue.Add(t);//to fix can't enum error when add in update 
            return t;
        }
        public static T Find<T>(Vessel vessel) where T : Behaviour, new()
        {
            foreach (var b in behaviours)
            {
                if (b.GetType().Equals(typeof(T)) && b.vessel == vessel)
                {
                    return b as T;
                }
            }
            foreach (var b in behavioursAddQueue)
            {
                if (b.GetType().Equals(typeof(T)) && b.vessel == vessel)
                {
                    return b as T;
                }
            }
            return null;
        }
        public static T[] FindAll<T>(Vessel vessel) where T : Behaviour, new()
        {
            List<T> all = new List<T>();
            foreach (var b in behaviours)
            {
                if (b.GetType().Equals(typeof(T)) && b.vessel == vessel)
                {
                    all.Add(b as T);
                }
            }
            foreach (var b in behavioursAddQueue)
            {
                if (b.GetType().Equals(typeof(T)) && b.vessel == vessel)
                {
                    all.Add(b as T);
                }
            }
            return all.ToArray();
        }
        public static void Clear(Vessel v)
        {
            foreach (var b in behaviours)
            {
                if(b.vessel == v)
                {
                    Remove(b);
                }
            }
        }
        public static void Remove(Behaviour b)
        {
            behavioursDelQueue.Add(b);
            Coroutine.StopCoroutineOn(b);
        }
        public static void UpdateAll()
        {
            foreach (var t in behaviours)
            {
                if (t && t.enabled)
                {
#if CATCH_EXCEPTION
                    try
                    {
#endif
                    t.EarlyUpdate();
#if CATCH_EXCEPTION
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Exception during EarlyUpdate() of " + t.GetType().ToString() + " : ", e);
                    }
#endif
                }
            }
            foreach (var t in behavioursAddQueue)
            {
                behaviours.Add(t);
            }
            behavioursAddQueue.Clear();
            foreach (var t in behaviours)
            {
                if (t && t.enabled)
                {
#if CATCH_EXCEPTION
                    try
                    {
#endif
                    t.Update();
#if CATCH_EXCEPTION
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Exception during Update() of " + t.GetType().ToString() + " : ", e);
                    }
#endif
                }
            }
            foreach (var t in behavioursAddQueue)
            {
                behaviours.Add(t);
            }
            behavioursAddQueue.Clear();
            foreach (var t in behaviours)
            {
                if (t && t.enabled)
                {
#if CATCH_EXCEPTION
                    try
                    {
#endif
                    t.LateUpdate();
#if CATCH_EXCEPTION
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Exception during LateUpdate() of " + t.GetType().ToString() + " : ", e);
                    }
#endif
                }
            }
            foreach (var t in behavioursAddQueue)
            {
                behaviours.Add(t);
            }
            behavioursAddQueue.Clear();
            foreach (var t in behavioursDelQueue)
            {
                if (behaviours.Contains(t))
                {
                    behaviours.Remove(t);
                }
            }
            behavioursDelQueue.Clear();
        }
        public Coroutine StartCoroutine(IEnumerator enumerator) => Coroutine.StartCoroutine(enumerator, this);
        public void StopCoroutine(Coroutine coroutine) => Coroutine.StopCoroutine(coroutine);
        public void Log(string msg) => ConnectionInitializer.Log("[" + vid + "]" + msg);
        public void LogInfo(string key, string msg) => Info.AddInfo("[" + vid + "]" + key, msg);

        public static implicit operator bool(Behaviour b) => b != null;

        public bool enabled
        {
            get { return enabled_; }
            set
            {
                enabled_ = value;
                if (value && !enabled_)
                    OnEnable();
                else if (!value && enabled_)
                    OnDisable();
            }
        }
        private bool enabled_ = true;
        public Vessel activeVessel { get { return spaceCenter.ActiveVessel; } }
        public CelestialBody body { get { return vessel.Orbit.Body; } }
        ReferenceFrame bodyRef_; public ReferenceFrame bodyRef { get { if (bodyRef_ == null) bodyRef_ = body.ReferenceFrame; return bodyRef_; } }
        ReferenceFrame localRef_; public ReferenceFrame localRef { get { if (localRef_ == null) localRef_ = vessel.ReferenceFrame; return localRef_; } }
        ReferenceFrame surfaceRef_; public ReferenceFrame surfaceRef { get { if (surfaceRef_ == null) surfaceRef_ = vessel.SurfaceReferenceFrame; return surfaceRef_; } }
        public Vessel vessel;
        //public Vector3 SrfVel => Vector3.Transform(LocalVel, vessel.Flight(vessel.SurfaceReferenceFrame).Rotation.ToQuat());
        //public Vector3 LocalVel => Vector3.Transform(vessel.Flight(body.ReferenceFrame).Velocity.ToVec(), Quaternion.Invert(vessel.Flight(body.ReferenceFrame).Rotation.ToQuat()));
        public Connection connection;
        public ulong vid;
        public KRPC.Client.Services.SpaceCenter.Service spaceCenter;
        public T GetComponent<T>() where T : Behaviour, new() => vessel.GetComponent<T>();
        public T AddComponent<T>() where T : Behaviour, new() => vessel.AddComponent<T>();
        public T GetOrAddComponent<T>() where T : Behaviour, new() { T c = GetComponent<T>(); return c ? c : AddComponent<T>(); }
        public abstract void Start();
        public abstract void Update();
        public virtual void EarlyUpdate() { }
        public virtual void LateUpdate() { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        
    }
}
