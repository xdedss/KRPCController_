using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KRPC.Client;
using KRPC.Client.Services;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Toe;
using SocketUtil;

namespace KRPCController
{
    static class ConnectionInitializer
    {
        public static Form1 form;

        public static Connection conn;

        public static SocketServer socketServer;

        public class SocketData
        {
            public SocketData()
            {
                toggles = new bool[16];
            }
            public Vector2 joystickL;
            public Vector2 joystickR;
            public float throttle;
            public float rudder;
            public bool[] toggles;
        }

        public static void InitSocket()
        {
            socketServer = new SocketServer(8080);
            socketServer.StartListen();
        }

        public static void HandleSocketData(byte[] bytes)
        {
            var bytesStr = bytes[0].ToString() + '|' + bytes[1].ToString() + '|' + bytes[2].ToString() + '|' + bytes[3].ToString() + '|' + bytes[4].ToString() + '|' + bytes[5].ToString();
            //var bytesStr = bytes[0].ToString();
            //Log(data.joystickR.ToString());
            if (conn != null)
            {
                Info.AddInfo("received", bytesStr);
                var vessel = conn.SpaceCenter().ActiveVessel;
                if (vessel != null)
                {
                    vessel.Control.InputMode = ControlInputMode.Additive;
                    var data = ParseSocketBytes(bytes);
                    vessel.Control.Throttle = data.throttle;
                    vessel.Control.Pitch = -data.joystickR.Y;
                    vessel.Control.Roll = data.joystickR.X;
                    vessel.Control.Yaw = data.joystickL.X;
                    vessel.Control.WheelSteering = -data.rudder;
                    for(int i = 1; i < 10; i++)
                    {
                        if (data.toggles[i])
                        {
                            vessel.Control.ToggleActionGroup((uint)(i));
                        }
                    }
                    if (data.toggles[0])
                        vessel.Control.ToggleActionGroup((uint)10);
                    if (data.toggles[10])
                        vessel.Control.SAS = !vessel.Control.SAS;
                    if (data.toggles[11])
                        vessel.Control.RCS = !vessel.Control.RCS;
                    if (data.toggles[12])
                        vessel.Control.Brakes = !vessel.Control.Brakes;
                    if (data.toggles[13])
                        vessel.Control.Lights = !vessel.Control.Lights;
                    if (data.toggles[14])
                        vessel.Control.Gear = !vessel.Control.Gear;
                    if (data.toggles[15])
                        vessel.Control.ActivateNextStage();
                }
            }
            else
            {
                Log(bytesStr);
            }
        }

        public static SocketData ParseSocketData(string msg)
        {
            var msgs = msg.Split('|');
            var data = new SocketData();
            var j1 = new Vector2(float.Parse(msgs[0]), float.Parse(msgs[1]));
            var j2 = new Vector2(float.Parse(msgs[2]), float.Parse(msgs[3]));
            data.joystickL = j1;
            data.joystickR = j2;
            return data;
        }

        public static SocketData ParseSocketBytes(byte[] bytes)
        {
            var data = new SocketData();
            var j1 = new Vector2(((float)bytes[0]) / 255 * 2 - 1, ((float)bytes[1]) / 255 * 2 - 1);
            var j2 = new Vector2(((float)bytes[2]) / 255 * 2 - 1, ((float)bytes[3]) / 255 * 2 - 1);
            var thr = ((float)bytes[4]) / 255;
            var rud = ((float)bytes[5]) / 255 * 2 - 1;
            data.joystickL = j1;
            data.joystickR = j2;
            data.throttle = thr;
            data.rudder = rud;
            for(int i = 0; i < 8; i++)
            {
                if((bytes[6] & ByteMask(i)) != 0)
                {
                    data.toggles[i] = true;
                }
            }
            for(int i = 8; i < 16; i++)
            {
                if((bytes[7] & ByteMask(i - 8)) != 0)
                {
                    data.toggles[i] = true;
                }
            }
            return data;
        }

        static byte ByteMask(int pos)
        {
            return (byte)(1 << pos);
        }

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

            //vessel.AddComponent<Behaviours.InfoLog>();
            //vessel.AddComponent<Behaviours.StabilityControlSeparated>();
            //vessel.AddComponent<Behaviours.StabilityDirection>();
            //vessel.AddComponent<Behaviours.AscentControl>();
            //vessel.AddComponent<Behaviours.SoftLandingTilt>();
            //vessel.AddComponent<Behaviours.PointLanding>();
            //vessel.AddComponent<Behaviours.CommonDataStream>();
            //vessel.AddComponent<Behaviours.DronePosition>().reference = spaceCenter.TargetVessel.ReferenceFrame;
            //vessel.AddComponent<Behaviours.Drones>();
            //vessel.AddComponent<Behaviours.HeightEstLanding>();

            Console.WriteLine("initialized");
        }

        public static void Log(string msg)
        {
            form.LogBox.AppendText("\r\n" + msg);
        }

        public static void LogFile(string fname, string msg)
        {
            var sw = System.IO.File.AppendText("D:/" + fname + ".txt");
            sw.WriteLine(msg);
            sw.Flush();
            sw.Close();
        }

        public static void UpdateInfo(string info)
        {
            form.InfoBox.Text = info;
        }
    }
}
