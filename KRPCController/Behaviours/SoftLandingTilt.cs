using KRPC.Client.Services.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toe;

namespace KRPCController.Behaviours
{
    /// <summary>
    /// 考虑了水平速度的反推着陆
    /// </summary>
    class SoftLandingTilt : Behaviour
    {
        public bool on = false;
        public float extraHeight = 0;

        CommonDataStream data;

        public SoftLandingTilt()
        {

        }

        //Line line;
        public override void Start()
        {
            data = GetOrAddComponent<CommonDataStream>();
            //line = connection.Drawing().AddLine(Vector3.Zero.ToTuple(), new Vector3(0, 10, 0).ToTuple(), vessel.SurfaceReferenceFrame);
            //line.Thickness = 5f;
            g = body.SurfaceGravity;
        }

        public override void Update()
        {
            Predict();   
        }

        public float estT;
        float g;
        public Vector3 Predict()
        {
            //var flightSrf = vessel.Flight(vessel.SurfaceReferenceFrame);

            var bodyVel = data.GetVelocity(bodyRef);
            var bodyRot = data.GetRotation(bodyRef);
            var localVel = Vector3.Transform(bodyVel, Quaternion.Invert(bodyRot));
            var srfRot = data.GetRotation(surfaceRef);
            var srfVel = Vector3.Transform(localVel, srfRot);


            //var bounding = vessel.BoundingBox(surfaceRef);
            var boundingLocal = data.GetLocalBoundingBox();
            var boundingSurface = data.GetSurfaceBoundingBox();
            var vesselHeight = Math.Max(Math.Abs(Math.Min(boundingLocal[0].X, boundingLocal[1].X)), Math.Abs(Math.Min(boundingSurface[0].X, boundingSurface[1].X))) + extraHeight;
            //var alt = (float)vessel.Flight().SurfaceAltitude - vesselHeight;
            //var alt = (float)body.SrfAltitudeAtPosision(Vector3.Zero, surfaceRef) - vesselHeight;
            var alt = data.GetSurfaceAlt() - vesselHeight;

            //var thrustDir = Vector3.Transform(new Vector3(0, -1, 0), srfRot);
            
            var pos = Vector3.Zero;
            var vel = new Vector3(srfVel);
            float a = data.GetMaxThrust() / data.GetMass();
            float t = 0;

            //float vg = (float)Math.Sqrt(2 * alt * g + srfVel.X * srfVel.X);
            //float apprT = (vg + srfVel.X) / g;
            float apprT = -vel.X / (a - g);//在忽略空气阻力和水平速度的情况下预估出将竖直速度降至零所需的大致时间
            float dt = Math.Max(apprT / 10, 0.01f);//递推的步长
            int count = 0;
            while(vel.X < 0 && count < 30)
            {
                count++;
                t += dt;
                vel -= vel.Normalized() * a * dt;//推力的加速
                vel.X -= g * dt;//重力的加速
                //var drag = flightSrf.SimulateAerodynamicForceAt(body, pos.ToTuple(), vel.ToTuple()).ToVec();
                //if(drag.Length > )
                //vel += drag / vessel.Mass * dt;
                pos += vel * dt;//位移
            }

            //line.End = pos.ToTuple();
            //var estAlt = (float)body.SrfAltitudeAtPosision(pos, surfaceRef) - vesselHeight;// wtf no way to optimize
            var estAlt = alt + pos.X;//预计停下的高度
            var notSpareRate = -pos.X / (-pos.X + estAlt);//这个比值越接近 1说明剩余的减速空间越小

            estT = t;
            //LogInfo("srfVel", srfVel.ToString());
            LogInfo("alt", alt.ToString() + " m");
            LogInfo("iterations", count.ToString());
            LogInfo("est.T", apprT.ToString() + " s");
            LogInfo("est.burn", t.ToString() + " s");
            LogInfo("est.vel", vel.ToString());
            //LogInfo("est.position", pos.ToString());
            LogInfo("est.alt", estAlt.ToString() + " m");
            LogInfo("ratio", notSpareRate.ToString());
            LogInfo("Xtra", extraHeight.ToString());

            if (!on && alt > 10 && notSpareRate > 0.8f)
            {
                on = true;
                Log("SoftLandingTilt engaged");
            }

            if (on)
            {
                if (t < 4.5f && !vessel.Control.Gear)
                {
                    vessel.Control.Gear = true;
                }
                if(srfVel.X >= 0 || notSpareRate < 0.5f)
                {
                    on = false;
                    Log("SoftLandingTilt ended");
                    vessel.Control.Throttle = 0;
                }
                else
                {
                    vessel.Control.Throttle = notSpareRate + (notSpareRate - 0.8f) * 2;//反馈调节notSpareRate到0.8
                }
            }
            else
            {
                //vessel.Control.Throttle = 0;
            }

            return Vector3.Zero;
        }
    }
}
