using System;
using System.Collections.Generic;
using Toe;

namespace KRPCController.Behaviours
{
    /// <summary>
    /// 简单的反推控制；
    /// 没有使用autopilot；
    /// 使用时需要游戏内设置SAS朝向Retrograde；
    /// 适用于低水平速度的非定点着陆
    /// </summary>
    //也就是我第一个视频里的效果

    class SoftLanding : Behaviour
    {
        public float extraHeight;
        float idealThrottle = 0.8f;
        public bool on = false;
        CommonDataStream data;

        public SoftLanding()
        {

        }

        public override void Start()
        {
            data = GetOrAddComponent<CommonDataStream>();
            g = body.SurfaceGravity;
        }

        float g;
        public override void Update()
        {
            var bodyVel = data.GetVelocity(bodyRef);
            var bodyRot = data.GetRotation(bodyRef);
            var localVel = Vector3.Transform(bodyVel, Quaternion.Invert(bodyRot));
            var srfRot = data.GetRotation(surfaceRef);
            var srfVel = Vector3.Transform(localVel, srfRot);

            var velVertical = -srfVel.X;

            var bounding = data.GetSurfaceBoundingBox();
            var height = (float)Math.Abs(Math.Min(bounding[0].X, bounding[1].X)) + extraHeight;

            var thrustDir = Vector3.Transform(new Vector3(0, -1, 0), srfRot);
            var maxAcc = (data.GetMaxThrust()) / data.GetMass() * -thrustDir.X;

            var alt = data.GetSurfaceAlt() - height;
            var needAcc = velVertical * velVertical / 2 / alt + g;
            var needThr = needAcc / maxAcc;

            LogInfo("height", height.ToString());
            LogInfo("alt", alt.ToString());
            LogInfo("velv", velVertical.ToString());
            LogInfo("maxAcc", maxAcc.ToString());
            LogInfo("needThr", needThr.ToString());

            if (!on && needThr > 0.5f)
            {
                on = true;
                Log("landing start");
            }
            if (on)
            {
                if (velVertical <= 0 || alt <= 0)
                {
                    on = false;
                    Log("landing end");
                    vessel.Control.Throttle = 0;
                }
                else
                {
                    vessel.Control.Throttle = needThr + (needThr - idealThrottle) * 3;
                }
            }
        }
    }
}
