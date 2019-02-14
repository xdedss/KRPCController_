using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toe;

namespace KRPCController.Behaviours
{
    class StabilityControlSeparated : Behaviour
    {
        public Vector3 currentDirection { get; private set; }
        public Vector3 direction { get { return direction_; } set { direction_ = value; direction_.Normalize(); } }
        Vector3 direction_;
        public float roll;
        public Quaternion rotation { set { direction = Vector3.Transform(new Vector3(0, 1, 0), value); } }
        public ReferenceFrame reference
        {
            get { return reference_; }
            set
            {
                reference_ = value;
            }
        }
        ReferenceFrame reference_;
        public bool on = true;
        public float error => Vector3.CalculateAngle(currentDirection, direction);

        public StabilityControlSeparated()
        {

        }

        Line line;
        bool showline = false;
        CommonDataStream data;
        public override void Start()
        {
            //default
            reference = vessel.SurfaceVelocityReferenceFrame;
            direction = new Vector3(0, -1, 0);
            roll = float.NaN;

            //init
            data = GetOrAddComponent<CommonDataStream>();
            lastLocalAngularVel = vessel.AngularVelocity(reference).ToVec();
            localExternalAngularAccFilter = Vector3.Zero;
            lastAppliedAcc = Vector3.Zero;

            //debug
            if (showline)
            {
                line = connection.Drawing().AddLine(Vector3.Zero.ToTuple(), new Vector3(0, 10, 0).ToTuple(), reference);
                line.Thickness = 0.1f;
            }
        }

        public override void Update()
        {
            if(direction == Vector3.Zero)
            {
                Log("cannot accept Zero");
                return;
            }
            if (Input.GetKeyDown(System.Windows.Forms.Keys.Space))
            {
                //on ^= true;
                Log("stability is " + (on ? "on" : "off"));
            }
            //var a = AvailableTorque;
            if (Time.gameDeltaTime > double.Epsilon && on)
            {
                Correct();
            }
        }

        Vector3 lastLocalAngularVel;
        Vector3 lastAppliedAcc;
        Vector3 localExternalAngularAcc;
        Vector3 localExternalAngularAccFilter;
        Vector3 localMaxAngularAccP;
        Vector3 localMaxAngularAccN;
        float Kp = 0.5f;
        float decelerateMargin = 5 * Mathf.Deg2Rad;
        float decelerateK = 0.35f;
        float angularVelLimit = (float)Math.PI / 6;
        void Correct()
        {
            //var flight = vessel.Flight(reference);
            currentDirection = Vector3.Transform(new Vector3(0, 1, 0), data.GetRotation(reference));
            Quaternion invRotation = Quaternion.Invert(data.GetRotation(reference));
            Vector3 localTarget = Vector3.Transform(direction, invRotation);
            Vector3 localAngularVel = Vector3.Transform(data.GetAngularVelocity(reference), invRotation);
            Vector3 localAngularAcc = (localAngularVel - lastLocalAngularVel) / (float)Time.gameDeltaTime;
            localExternalAngularAcc = localAngularAcc - lastAppliedAcc;
            //Log(localExternalAngularAccFilter.ToString() + "///" + localExternalAngularAcc.ToString() + "///--///" + Time.gameDeltaTime.ToString());
            localExternalAngularAccFilter = Vector3.Lerp(localExternalAngularAccFilter, localExternalAngularAcc, (float)Time.gameDeltaTime * 0.2f);//adjustable
            Vector3 interia = data.GetInteria();
            var torque = data.GetTorque();
            localMaxAngularAccP = Vector3.Divide(torque[0], interia);
            localMaxAngularAccN = Vector3.Divide(torque[1], interia);

            var pitchVel = localAngularVel.X;
            var pitchWeight = (float)Math.Sqrt(localTarget.Z * localTarget.Z + localTarget.Y * localTarget.Y);
            var pitchTarget = (float)Math.Atan2(localTarget.Z, localTarget.Y);
            var pitchBrakeAcc = InX2Acc(-Math.Sign(pitchTarget));
            //var pitchForwardAcc = InX2Acc(Math.Sign(pitchTarget));
            var pitchTargetVel = DecideVel(pitchTarget, pitchBrakeAcc);
            var pitchDiffVel = pitchTargetVel - pitchVel;
            var pitchNeedAcc = pitchDiffVel / (float)Time.gameDeltaTime * Kp;
            if(pitchWeight > 0.2f)
            {
                vessel.Control.Pitch = AccX2In(pitchNeedAcc);
            }

            var yawVel = localAngularVel.Z;
            var yawWeight = (float)Math.Sqrt(localTarget.X * localTarget.X + localTarget.Y * localTarget.Y);
            var yawTarget = (float)Math.Atan2(-localTarget.X, localTarget.Y);
            var yawBrakeAcc = InZ2Acc(-Math.Sign(yawTarget));
            var yawTargetVel = DecideVel(yawTarget, yawBrakeAcc);
            var yawDiffVel = yawTargetVel - yawVel;
            var yawNeedAcc = yawDiffVel / (float)Time.gameDeltaTime * Kp;
            if (yawWeight > 0.2f)
            {
                vessel.Control.Yaw = AccZ2In(yawNeedAcc);
            }

            if (showline)
            {
                line.End = (direction * 20).ToTuple();
                line.ReferenceFrame = reference;
            }
            //LogInfo("localDir   ", localTarget.ToString());
            //LogInfo("localVel   ", localAngularVel.ToString());
            LogInfo("localTarget", "yaw=" + yawTarget + "   pitch=" + pitchTarget);
            LogInfo("maxAcc", "pos=" + localMaxAngularAccP.ToString() + "   neg=" + localMaxAngularAccN.ToString());
            LogInfo("Acc   ", "yaw=" + localAngularAcc.Z + "   pitch=" + localAngularAcc.X);
            LogInfo("ExtAcc", "yaw=" + localExternalAngularAcc.Z + "   pitch=" + localExternalAngularAcc.X);
            LogInfo("ExtAccf", "yaw=" + localExternalAngularAccFilter.Z + "   pitch=" + localExternalAngularAccFilter.X);
            LogInfo("targetVel", "yaw=" + yawTargetVel + "   pitch=" + pitchTargetVel);


            lastLocalAngularVel = localAngularVel;
            lastAppliedAcc = Input2Acc(new Vector3(-vessel.Control.Pitch, -vessel.Control.Roll, -vessel.Control.Yaw));
        }

        float DecideVel(float target, float brakeAcc)
        {
            float maxVel = Math.Min((float)Math.Sqrt(Math.Abs(2 * decelerateMargin * brakeAcc)), angularVelLimit);
            if(target > decelerateMargin || target < -decelerateMargin)
            {
                return maxVel * Math.Sign(target);
            }
            else
            {
                return target / decelerateMargin * maxVel * decelerateK;
            }
        }

        float InX2Acc(float x) => x > 0 ? x * localMaxAngularAccP.X : -x * localMaxAngularAccN.X;
        float InY2Acc(float y) => y > 0 ? y * localMaxAngularAccP.Y : -y * localMaxAngularAccN.Y;
        float InZ2Acc(float z) => z > 0 ? z * localMaxAngularAccP.Z : -z * localMaxAngularAccN.Z;
        Vector3 Input2Acc(Vector3 inp) => new Vector3(InX2Acc(inp.X), InY2Acc(inp.Y), InZ2Acc(inp.Z));
        float AccX2In(float acc) => acc > 0 ? acc / localMaxAngularAccN.X : -acc / localMaxAngularAccP.X;
        float AccY2In(float acc) => acc > 0 ? acc / localMaxAngularAccN.Y : -acc / localMaxAngularAccP.Y;
        float AccZ2In(float acc) => acc > 0 ? acc / localMaxAngularAccN.Z : -acc / localMaxAngularAccP.Z;
        Vector3 Acc2Input(Vector3 acc) => new Vector3(AccX2In(acc.X), AccY2In(acc.Y), AccZ2In(acc.Z));
        float InX2AccReal(float x) => InX2Acc(x) + localExternalAngularAccFilter.X;
        float InY2AccReal(float y) => InY2Acc(y) + localExternalAngularAccFilter.Y;
        float InZ2AccReal(float z) => InZ2Acc(z) + localExternalAngularAccFilter.Z;

        float AccX2InReal(float acc) => AccX2In(acc - localExternalAngularAccFilter.X);
        float AccY2InReal(float acc) => AccY2In(acc - localExternalAngularAccFilter.Y);
        float AccZ2InReal(float acc) => AccZ2In(acc - localExternalAngularAccFilter.Z);
    }
}
