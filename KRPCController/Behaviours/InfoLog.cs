using KRPC.Client.Services.SpaceCenter;
using System;
using System.Collections;

namespace KRPCController.Behaviours
{
    //打印出各种数据
    //展示了Info类的东西

    class InfoLog : Behaviour
    {
        CommonDataStream data;
        ReferenceFrame orbitRef;

        public InfoLog()
        {

        }

        public override void Start()
        {
            data = GetOrAddComponent<CommonDataStream>();
            orbitRef = vessel.Orbit.Body.ReferenceFrame;
        }

        public override void Update()
        {
            var pos = data.GetPosition(orbitRef);
            var rot = data.GetRotation(surfaceRef);
            var lon = body.LongitudeAtPosition(pos, orbitRef);
            var lat = body.LatitudeAtPosition(pos, orbitRef);
            LogInfo("position", pos.ToString());
            LogInfo("rotation", rot.ToString());
            LogInfo("Lon", lon.ToString());
            LogInfo("Lat" , lat.ToString());
            LogInfo("SrfHeight", body.SurfaceHeight(lat, lon).ToString());
        }
    }
}
