using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KRPC.Client.Services.SpaceCenter;

namespace KRPCController
{
    /// <summary>
    /// 时钟，每帧更新各个系统，并且提供deltatime（仿Unity)
    /// </summary>
    static class Time
    {
        private static double startSecond = 0;
        public static double secondsSinceStart = 0;
        private static double lastSecondsSinceStart = 0;
        public static double UT;
        private static double lastUT = 0;
        /// <summary>
        /// 游戏中上一帧所用的时间
        /// </summary>
        public static double gameDeltaTime = 0;
        public static long framesSinceStart = 0;
        /// <summary>
        /// 程序中上一帧所用的时间
        /// </summary>
        public static double deltaTime = 0;

        public static void Update()
        {
            
            var ticks = DateTime.Now.Ticks;
            if (Time.startSecond == 0)
            {
                startSecond = (double)ticks / 10000000;
            }
            var seconds = (double)ticks / 10000000 - startSecond;
            Time.secondsSinceStart = seconds;
            Time.deltaTime = Time.secondsSinceStart - Time.lastSecondsSinceStart;
            Time.framesSinceStart++;
            UT = ConnectionInitializer.conn.SpaceCenter().UT;
            if (lastUT == 0)
            {
                lastUT = UT;
            }
            gameDeltaTime = UT - lastUT;
            Info.AddInfo("deltaTime", deltaTime.ToString());
            Info.AddInfo("gamedeltaTime", gameDeltaTime.ToString());
            Input.Update();
            Behaviour.UpdateAll();
            Coroutine.Update();
            Info.Update();
            Time.lastSecondsSinceStart = Time.secondsSinceStart;
            Time.lastUT = UT;
        }
    }
}
