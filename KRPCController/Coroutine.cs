using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPCController
{
    /// <summary>
    /// 掌控协程的类
    /// </summary>
    class Coroutine
    {
        public static Coroutine StartCoroutine(IEnumerator enumerator, Behaviour behaviour)
        {
            var c = new Coroutine(enumerator, behaviour);
            coroutinesAddQueue.Add(c);
            return c;
        }
        public static void StopCoroutine(Coroutine c)
        {
            if (coroutines.Contains(c))
            {
                coroutinesStopQueue.Add(c);
            }
        }
        public static void StopCoroutineOn(Behaviour behaviour)
        {
            var toStop = new List<Coroutine>();
            foreach(var c in coroutines)
            {
                if(c.behaviour == behaviour)
                {
                    toStop.Add(c);
                }
            }
            foreach(var c in coroutinesAddQueue)
            {
                if (c.behaviour == behaviour)
                {
                    toStop.Add(c);
                }
            }
            foreach(var c in toStop)
            {
                StopCoroutine(c);
            }
        }

        public static List<Coroutine> coroutines = new List<Coroutine>();
        public static List<Coroutine> coroutinesAddQueue = new List<Coroutine>();
        public static List<Coroutine> coroutinesStopQueue = new List<Coroutine>();
        public static void Update()
        {
            ProcessChange();
            var ended = new List<Coroutine>();
            foreach (var c in coroutines)
            {
                bool hasNext = true;
                if(c.enumerator.Current is IWait)
                {
                    var wait = c.enumerator.Current as IWait;
                    if (wait.Update())
                    {
                        hasNext = c.enumerator.MoveNext();
                    }
                }
                else
                {
                    hasNext = c.enumerator.MoveNext();
                }
                if (!hasNext)
                {
                    ended.Add(c);
                }
            }
            foreach(var ec in ended)
            {
                coroutines.Remove(ec);
            }
            ProcessChange();
        }

        static void ProcessChange()
        {
            foreach (var c in coroutinesAddQueue)
            {
                coroutines.Add(c);
            }
            coroutinesAddQueue.Clear();
            foreach (var c in coroutinesStopQueue)
            {
                if (coroutines.Contains(c))
                {
                    coroutines.Remove(c);
                }
            }
            coroutinesStopQueue.Clear();
        }

        public Coroutine(IEnumerator enumerator, Behaviour behaviour)
        {
            this.enumerator = enumerator;
            this.behaviour = behaviour;
        }
        public bool IsRunning() => coroutines.Contains(this);
        public IEnumerator enumerator;
        public Behaviour behaviour;
    }

    interface IWait
    {
        bool Update();
    }
    public class WaitForSeconds : IWait
    {
        public double seconds;
        public WaitForSeconds(double seconds)
        {
            this.seconds = seconds;
        }
        public bool Update()
        {
            seconds -= Time.deltaTime;
            return seconds <= 0;
        }
    }
    public class WaitForUpdates : IWait
    {
        public long updates;
        public WaitForUpdates(long updates)
        {
            this.updates = updates;
        }
        public bool Update()
        {
            updates--;
            return updates <= 0;
        }
    }
}
