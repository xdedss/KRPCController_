using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.SpaceCenter;
using System;
using System.Collections;
using System.Collections.Generic;
using Toe;

namespace KRPCController.Behaviours
{
    class DronePosition : Behaviour
    {
        public Vector3 targetPosition { get { return targetPosition_; } set { targetPosition_ = value; CreateHybrid(); } }
        Vector3 targetPosition_;
        public ReferenceFrame reference { get { return reference_; }
            set {
                reference_ = value;
                //stability.reference = value;
                CreateHybrid();
            }
        }
        ReferenceFrame reference_;
        ReferenceFrame hybrid;

        CommonDataStream data;
        StabilityControlSeparated stability;

        void CreateHybrid()
        {
            var relative = ReferenceFrame.CreateRelative(connection, reference_, targetPosition.ToTuple());
            hybrid = ReferenceFrame.CreateHybrid(connection, relative, surfaceRef);
        }
        IEnumerator CreateHybridTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                //CreateHybrid();
            }
        }

        public DronePosition()
        {

        }

        Line line;
        bool showLine = true;
        public override void Start()
        {
            data = GetOrAddComponent<CommonDataStream>();
            stability = GetOrAddComponent<StabilityControlSeparated>();
            stability.reference = surfaceRef;
            stability.direction = new Vector3(1, 0, 0);
            targetPosition = new Vector3(10, 20, 0);

            StartCoroutine(CreateHybridTimer());

            foreach(var engine in vessel.Parts.Engines)
            {
                engine.Active = true;
            }
            vessel.Control.SAS = false;

            if (showLine)
                line = connection.Drawing().AddLine(Vector3.Zero.ToTuple(), Vector3.One.ToTuple(), surfaceRef);
            //vessel.Control.ActivateNextStage();
        }

        float Kph = 0.08f;
        float Kih = 0.004f;
        float Kdh = 0.25f;
        float integralh = 0;
        float KpHor = 0.08f;
        float KiHor = 0f;
        float KdHor = 0.15f;
        float future = 1f;
        Vector2 integralHor = new Vector2(0, 0);
        public override void Update()
        {
            if (Input.GetKey(System.Windows.Forms.Keys.K))
            {
                Log("max");
                vessel.Control.Throttle = 1;
                return;
            }

            var targetLocalSurfacePos = -data.GetPosition(hybrid);
            var localSurfaceVel = data.GetVelocity(hybrid);
            var thrust = data.GetThrust();
            var maxThrust = data.GetMaxThrust();
            var mass = data.GetMass();
            var a = (thrust / mass - 9.81f);
            var pitch = vessel.Flight().Pitch;

            var posh = targetLocalSurfacePos.X;
            var horAdjustFactor = Math.Max(1 - Math.Abs(posh) / 3, 0);
            var velh = localSurfaceVel.X;
            var poshFuture = posh + velh * future + 0.5f * a * future * future;
            var velhFuture = velh + a * future;
            integralh = Mathf.Clamp(integralh + posh * (float)Time.gameDeltaTime * horAdjustFactor, -0.1f / Kih, 0.1f / Kih);
            var ph = Kph * poshFuture;
            var ih = integralh * Kih;
            var gFactor = mass * 9.81f / maxThrust / (float)Math.Cos((90 - pitch) * Mathf.Deg2Rad);
            var dh = -Kdh * velhFuture;

            var targetThrottle = Mathf.Clamp01(ph + ih + dh + gFactor);
            var currentThrottle = thrust / maxThrust;
            vessel.Control.Throttle = targetThrottle + (targetThrottle - currentThrottle) * 3;

            var posHor = targetLocalSurfacePos.Yz;
            var velHor = localSurfaceVel.Yz;
            integralHor += posHor * (float)Time.gameDeltaTime;
            var pHor = KpHor * posHor;
            var iHor = integralHor * KiHor * horAdjustFactor;
            var dHor = -KdHor * velHor;
            var sum = (pHor * horAdjustFactor + iHor * horAdjustFactor + dHor).Clamp(1);
            stability.direction = new Vector3(1, sum.X, sum.Y);

            if (showLine)
            {
                line.End = targetLocalSurfacePos.ToTuple();
            }
        }
    }
}
