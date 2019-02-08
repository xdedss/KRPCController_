using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KRPC.Client;
using KRPC.Client.Services;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

namespace KRPCController
{
    /// <summary>
    /// 连接服务器并添加初始Behaviour
    /// </summary>
    static class ConnectionInitializer
    {
        public static Form1 form;

        public static Connection conn;

        public static void Init()
        {
            conn = new Connection("Test");
            var krpc = conn.KRPC();
            Log("Connected:  " + krpc.GetStatus().Version);

            InitComponents();
        }

        public static void InitComponents()
        {
            var spaceCenter = conn.SpaceCenter();
            var vessel = spaceCenter.ActiveVessel;


            //这里写初始加载的Behaviour
            //vessel.AddComponent<Behaviours.xxxxxx>();
            vessel.AddComponent<Behaviours.SoftLanding>();


            Console.WriteLine("initialized");
        }

        public static void Log(string msg)
        {
            form.LogBox.AppendText("\r\n" + msg);
        }

        public static void UpdateInfo(string info)
        {
            form.InfoBox.Text = info;
        }
    }
}
