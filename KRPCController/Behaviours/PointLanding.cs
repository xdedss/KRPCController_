using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.SpaceCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toe;

namespace KRPCController.Behaviours
{
    /// <summary>
    /// 从一级分离开始，控制火箭自动返回VAB楼顶
    /// </summary>
    class PointLanding : Behaviour
    {
        StabilityControlSeparated stability;
        CommonDataStream data;

        Status lastStatus;
        public Status status = Status.Wait;
        public enum Status { Wait, Adjust, Descend, Landing}

        public double Lat;
        public double Lon;
        public double Height;
        public double DescendHeight;

        public uint preDescendAction;
        bool preDescendActionTriggered = false;
        public float DecAoA;
        public float SafeVel;
        public float AoAStartVel;
        public float MaxAeroTilt;//aerodynamic adjust tilt
        public float MaxLandingTilt;//thrust adjust tilt

        public PointLanding()
        {

        }

        //Line line;
        public override void Start()
        {
            //default 默认返回到VAB楼顶靠北的那个停机坪   //？？？停机坪不是东西向两个的吗哪里有靠北的 我都不记得我这写的什么玩意
            Lon = -74.6200965;
            Lat = -0.0967588;
            Height = 0;// 110;
            DescendHeight = 13000;
            preDescendAction = 10;
            DecAoA = 15 * Mathf.Deg2Rad;
            SafeVel = 240;
            AoAStartVel = 600;
            MaxAeroTilt = 12 * Mathf.Deg2Rad;
            MaxLandingTilt = 6 * Mathf.Deg2Rad;

            //init
            stability = GetOrAddComponent<StabilityControlSeparated>();
            lastStatus = status;
            data = GetOrAddComponent<CommonDataStream>();
            vessel.Control.SAS = false;
            vessel.Control.RCS = true;

            WaitStart();

            //debug
            //line = connection.Drawing().AddLine(Vector3.Zero.ToTuple(), Vector3.One.ToTuple(), vessel.SurfaceReferenceFrame);
            //line.Thickness = 5;
        }

        public override void Update()
        {
            switch (status)
            {
                case Status.Wait://等待滑行至Ap附近
                    WaitUpdate();
                    break;
                case Status.Adjust://开始变轨使得落点在目标点附近
                    AdjustUpdate();
                    break;
                case Status.Descend://变轨完成之后滑行，等到合适的时机进行第一次反推消除大部分水平速度
                    DescendUpdate();
                    break;
                case Status.Landing://第一次反推后已经在目标点正上方，这时候竖直下落，并在合适的时机开始最终反推
                    LandingUpdate();
                    break;
            }
            if (status != lastStatus)
            {
                switch (status)
                {
                    case Status.Adjust:
                        AdjustStart();
                        break;
                    case Status.Descend:
                        DescendStart();
                        break;
                    case Status.Landing:
                        LandingStart();
                        break;
                }
            }
            lastStatus = status;

            LogInfo("status", status.ToString());
        }

        void WaitStart()
        {
            stability.reference = vessel.SurfaceVelocityReferenceFrame;
            stability.direction = new Vector3(0, 1, 0);
        }

        void WaitUpdate()
        {
            var bodyVel = data.GetVelocity(bodyRef);
            var bodyRot = data.GetRotation(bodyRef);
            var localVel = Vector3.Transform(bodyVel, Quaternion.Invert(bodyRot));
            var srfRot = data.GetRotation(surfaceRef);
            var srfVel = Vector3.Transform(localVel, srfRot);
            if (srfVel.X < 0 || vessel.Orbit.TimeToApoapsis < 30)
            {
                status = Status.Adjust;
            }
        }

        void AdjustStart()
        {
            stability.reference = vessel.SurfaceReferenceFrame;
            target = connection.SpaceCenter().TargetVessel;
            predictionStartTime = Time.UT;
        }

        float errorTimer = 0;
        double predictionStartTime;
        void AdjustUpdate()
        {

            double utAtHeight = FindHeightT((float)DescendHeight, 1000);
            //Vector3 posAtHeight = vessel.Orbit.PositionAt(utAtHeight, vessel.SurfaceReferenceFrame).ToVec();
            Vector3 target = TargetPosition(surfaceRef);

            var bodyVel = data.GetVelocity(bodyRef);
            var bodyRot = data.GetRotation(bodyRef);
            var localVel = Vector3.Transform(bodyVel, Quaternion.Invert(bodyRot));
            var srfRot = data.GetRotation(surfaceRef);
            var srfVel = Vector3.Transform(localVel, srfRot);

            if (data.GetMeanAlt() < 25000 && srfVel.X < 0)
            {
                status = Status.Descend;//如果高度过低，跳转到Descend
                return;
            }

            Vector3 srfHorVel = new Vector3(srfVel);
            srfHorVel.X = 0;
            Vector3 targetHor = new Vector3(target);
            targetHor.X = 0;
            Vector3 targetHorVel = targetHor / (float)(utAtHeight - Time.UT);
            Vector3 dv = targetHorVel - srfHorVel;//调至目标轨道所需的水平速度增量
            float dvm = dv.Length;
            stability.direction = dv;
            float errorAngle = Vector3.CalculateAngle(stability.currentDirection, dv);
            float maxAcc = data.GetMaxThrust() / data.GetMass();
            if(errorAngle < 0.1f)
            {
                errorTimer += (float)Time.gameDeltaTime;
            }
            else
            {
                errorTimer = 0;
            }
            if (errorTimer > 1 && dvm > 5)
            {//稳定朝向目标时打开节流阀，p控制
                vessel.Control.Throttle = dvm / maxAcc;
            }
            else
            {
                vessel.Control.Throttle = 0;
            }
            if(dvm < 5)
            {
                vessel.Control.Throttle = 0;
                status = Status.Descend;//足够接近目标轨道，转到Descend
            }

            LogInfo("dv", dv.ToString());
            LogInfo("error", errorAngle.ToString());
        }

        void DescendStart()
        {
            Log("descend start");
            stability.reference = vessel.SurfaceVelocityReferenceFrame;
            stability.direction = new Vector3(0, -1, 0);
            equator = body.EquatorialRadius;
            g = body.SurfaceGravity;
        }

        bool descendBurnStarted = false;
        float equator;
        float g;
        void DescendUpdate()
        {
            //stability.direction = -SrfVel;
            if (stability.error < 0.2f && !preDescendActionTriggered)
            {
                preDescendActionTriggered = true;
                vessel.Control.ToggleActionGroup(preDescendAction);
            }
            //var flightSrf = vessel.Flight(vessel.SurfaceReferenceFrame);

            var bodyVel = data.GetVelocity(bodyRef);
            var bodyRot = data.GetRotation(bodyRef);
            var localVel = Vector3.Transform(bodyVel, Quaternion.Invert(bodyRot));
            var srfRot = data.GetRotation(surfaceRef);
            var srfVel = Vector3.Transform(localVel, srfRot);

            var currentAoA = Mathf.Lerp(0, DecAoA, Mathf.Clamp01(Mathf.InverseLerp(AoAStartVel, SafeVel, srfVel.Length)));
            var horVel = srfVel.Yz.Length;
            var a = data.GetMaxThrust() / data.GetMass();
            var minHorAcc = a * (float)Math.Sin(DecAoA);
            var tilt = Math.Atan(srfVel.Yz.Length / srfVel.Length);
            var maxHorAcc = a * (float)Math.Sin(tilt + DecAoA);
            var estT = horVel / maxHorAcc;
            var dt = estT / 10;

            Vector3 targetPos = TargetPosition(surfaceRef);
            Vector3 vel = new Vector3(srfVel);
            Vector3 pos = Vector3.Zero;
            float t = 0;
            int count = 0;
            while(count < 40)//递推预测如果现在开始消除水平速度，火箭将会到达的位置，如果位置足够接近目标点则点火
            {
                t += dt;
                count++;
                Vector3 velNeg = -vel.Normalized();
                Vector3 up = new Vector3(1, 0, 0);
                Vector3 right = Vector3.Cross(up, velNeg);
                var curAoA = Mathf.Lerp(0, DecAoA, Mathf.InverseLerp(AoAStartVel, SafeVel, vel.Length));
                Vector3 thrustDir = Vector3.Transform(velNeg, Quaternion.FromAxisAngle(right, curAoA));//wtf RadOrDeg
                vel += thrustDir * a * dt;
                //float centerAlt = data.GetMeanAlt() + equator + pos.X;
                vel.X -= g * dt;
                pos += vel * dt;
                if(Vector2.Dot(vel.Yz, srfVel.Yz) < 0)
                {
                    break;
                }
            }
            Vector2 horDir = srfVel.Yz.Normalized();
            Vector2 horDirNorm = new Vector2(-horDir.Y, horDir.X);
            Vector2 targetHorDir = targetPos.Yz.Normalized();
            float movedDistance = Vector2.Dot(horDir, pos.Yz);
            float totalDistance = Vector2.Dot(horDir, targetPos.Yz);
            float ratio = movedDistance / totalDistance;//预测位置接近目标点的比例

            //同时考虑消除法线方向的偏差
            float horNormalBias = Math.Min(Vector2.Dot(horDirNorm, targetHorDir) * 20, 0.4f) * Math.Min(1f, horVel / 300);

            if(!descendBurnStarted && ratio > 0.5f)
            {
                descendBurnStarted = true;
            }
            if (descendBurnStarted)
            {
                if(horVel < 1f || ratio < 0)
                {
                    status = Status.Landing;//如果水平速度基本消除完成则跳转到Landing
                    vessel.Control.Throttle = 0;
                    stability.direction = new Vector3(0, -1, 0);
                }
                else
                {
                    vessel.Control.Throttle = ratio + (ratio - 0.6f) * 1;
                    var pitchRot = Quaternion.FromAxisAngle(new Vector3(0, 0, 1), -currentAoA);
                    stability.direction = Vector3.Transform(new Vector3(0, -1, horNormalBias), pitchRot);
                }
            }

            //line.End = pos.ToTuple();
            LogInfo("horVel", srfVel.Yz.ToString());
            LogInfo("iterations", count.ToString());
            LogInfo("est.T", t.ToString());
            LogInfo("horVelRatio", ratio.ToString());
            LogInfo("curAoA", (currentAoA * Mathf.Rad2Deg).ToString());
            LogInfo("horNormalBias", horNormalBias.ToString());
        }

        SoftLandingTilt landing;
        //SoftLanding landingSimple;
        void LandingStart()
        {
            //ClearRCS();
            stability.reference = vessel.SurfaceReferenceFrame;
            landing = GetOrAddComponent<SoftLandingTilt>();//Landing的时候竖直方向的控制就交给SoftLandingTilt完成
            landing.extraHeight = (float)Height;
            //landingSimple = GetOrAddComponent<SoftLanding>();
            //landingSimple.extraHeight = (float)Height;

            Log("landing started");
            //this.enabled = false;
            //stability.enabled = false;
            //landing.enabled = false;
        }

        float maxLandingHorVel = 8f;
        float maxAeroHorVel = 4f;
        float future = 0.12f;
        void LandingUpdate()
        {
            //var flightSrf = vessel.Flight(vessel.SurfaceReferenceFrame);
            var bodyVel = data.GetVelocity(bodyRef);
            var bodyRot = data.GetRotation(bodyRef);
            var localVel = Vector3.Transform(bodyVel, Quaternion.Invert(bodyRot));
            var srfRot = data.GetRotation(surfaceRef);
            var srfVel = Vector3.Transform(localVel, srfRot);

            if (landing.on)
            {//如果引擎开启则倾斜箭体调节水平速度和位置
                var a = data.GetMaxThrust() / data.GetMass();
                var maxAcc = a * (float)Math.Sin(MaxLandingTilt);
                var decDist = maxLandingHorVel * maxLandingHorVel / 2 / (maxAcc * 0.8f);

                var target = TargetPosition(surfaceRef);
                var targetHorDist = target.Yz.Length;
                var targetHorDir = target.Yz / targetHorDist;

                var targetHorVel = maxLandingHorVel * targetHorDir;
                if (targetHorDist < decDist)
                    targetHorVel *= targetHorDist / decDist / 2;//adjustable
                if (targetHorDist == 0)
                    targetHorVel = Vector2.Zero;

                //var targetHorVel = (target.Yz / 1.5f).Clamp(10);//adjustable

                //var targetHorVel = (target.Yz / (landing.estT / 2)).Clamp(landing.estT * 0.5f);

                var futureSrfVel = srfVel.Yz + Vector3.Transform(new Vector3(0, a, 0), srfRot).Yz * future;
                var targetHorAcc = ((targetHorVel - futureSrfVel) / 1.5f).Clamp(a / 2);//adjustable
                //var targetHorAcc = ((targetHorVel - futureSrfVel) / (landing.estT / 3)).Clamp(a / 2);
                var targetHorAccDir = new Vector3(0, targetHorAcc.X, targetHorAcc.Y).Normalized();
                var targetTilt = Math.Asin(targetHorAcc.Length / a);
                var tilt = Math.Min(targetTilt, MaxLandingTilt);
                var dir = new Vector3(1, 0, 0) + targetHorAccDir * (float)Math.Tan(tilt);
                stability.direction = dir;

                LogInfo("tilt", tilt.ToString());
            }
            else
            {//如果没有开启引擎则利用攻角产生的气动力调节了落点
                var a = 0.5f;
                var decDist = maxAeroHorVel * maxAeroHorVel / 2 / (a * 0.8f);

                var target = TargetPosition(surfaceRef);
                var targetHorDist = target.Yz.Length;
                //var targetHorDir = target.Yz / targetHorDist;

                //var targetHorVel = maxAeroHorVel * targetHorDir;
                //if (targetHorDist < decDist)
                //    targetHorVel *= targetHorDist / decDist / 2;//adjustable
                //if (targetHorDist == 0)
                //    targetHorVel = Vector2.Zero;
                var targetHorVel = (target.Yz / 4).Clamp(3);

                var targetHorAcc = ((targetHorVel - srfVel.Yz) / 4);//adjustable
                //var targetHorAcc = targetHorVel;
                var targetHorAccDir = new Vector3(0, targetHorAcc.X, targetHorAcc.Y).Normalized();
                var targetTilt = Mathf.Lerp(0, MaxAeroTilt, Mathf.Clamp01(Mathf.InverseLerp(0, a, targetHorAcc.Length)));
                //var tilt = Math.Min(targetTilt, MaxAeroTilt);
                var dir = new Vector3(1, 0, 0) - targetHorAccDir * (float)Math.Tan(targetTilt);
                stability.direction = dir;
                //stability.direction = -SrfVel;
            }

            if(srfVel.X >= 0)
            {
                stability.on = false;
                vessel.Control.RCS = false;
            }
        }

        double FindHeightT(float height, float error)//optimize this
        {
            var orbit = vessel.Orbit;
            var tlow = predictionStartTime + orbit.Period / 2;
            var thigh = predictionStartTime;
            var tc = (tlow + thigh) / 2;
            var altitude = orbit.AltitudeAt(tc);
            while (true)
            {
                if(altitude > height + error)
                {
                    thigh = tc;
                }
                else if(altitude < height - error)
                {
                    tlow = tc;
                }
                else
                {
                    break;
                }
                tc = (tlow + thigh) / 2;
                altitude = orbit.AltitudeAt(tc);
            }
            return tc;
        }

        Vector3 targetFixedPos = Vector3.Zero;
        ReferenceFrame targetVirtual = null;
        Vessel target;
        Vector3 TargetPosition(ReferenceFrame referenceFrame)//如果把另一个vessel设为目标（比如回收用的船），则返回这个vessel的位置， 否则返回由给定经纬度和高度确定的位置
        {
            if (targetVirtual == null)
            {
                if (target == null)
                {
                    targetFixedPos = body.PositionAtAltitude(Lat, Lon, Height + body.SurfaceHeight(Lat, Lon), bodyRef).ToVec();
                    targetVirtual = ReferenceFrame.CreateHybrid(connection, ReferenceFrame.CreateRelative(connection, bodyRef, targetFixedPos.ToTuple()), surfaceRef);
                }
                else
                {
                    targetVirtual = target.SurfaceReferenceFrame;
                }
            }
            //return connection.SpaceCenter().TransformPosition(targetFixedPos.ToTuple(), bodyRef, referenceFrame).ToVec();
            
            return -data.GetPosition(targetVirtual);
        }
    }
}
