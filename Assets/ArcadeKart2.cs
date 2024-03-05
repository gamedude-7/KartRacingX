using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.VFX;

namespace KartGame.KartSystems
{
    public class ArcadeKart2 : MonoBehaviour
    {
        public Text speedText;
        public Sector pSector;
        public Transform ovalTrack;
        public ArcadeKart arcadeKart;
        public string currentSector;

        [System.Serializable]
        public class StatPowerup
        {
            public ArcadeKart.Stats modifiers;
            public string PowerUpID;
            public float ElapsedTime;
            public float MaxTime;
        }

        [System.Serializable]
        public struct Stats
        {
            [Header("Movement Settings")]
            [Min(0.001f), Tooltip("Top speed attainable when moving forward.")]
            public float TopSpeed;

            [Tooltip("How quickly the kart reaches top speed.")]
            public float Acceleration;

            [Min(0.001f), Tooltip("Top speed attainable when moving backward.")]
            public float ReverseSpeed;

            [Tooltip("How quickly the kart reaches top speed, when moving backward.")]
            public float ReverseAcceleration;

            [Tooltip("How quickly the kart starts accelerating from 0. A higher number means it accelerates faster sooner.")]
            [Range(0.2f, 1)]
            public float AccelerationCurve;

            [Tooltip("How quickly the kart slows down when the brake is applied.")]
            public float Braking;

            [Tooltip("How quickly the kart will reach a full stop when no inputs are made.")]
            public float CoastingDrag;

            [Range(0.0f, 1.0f)]
            [Tooltip("The amount of side-to-side friction.")]
            public float Grip;

            [Tooltip("How tightly the kart can turn left or right.")]
            public float Steer;

            [Tooltip("Additional gravity for when the kart is in the air.")]
            public float AddedGravity;

            // allow for stat adding for powerups.
            public static Stats operator +(Stats a, Stats b)
            {
                return new Stats
                {
                    Acceleration = a.Acceleration + b.Acceleration,
                    AccelerationCurve = a.AccelerationCurve + b.AccelerationCurve,
                    Braking = a.Braking + b.Braking,
                    CoastingDrag = a.CoastingDrag + b.CoastingDrag,
                    AddedGravity = a.AddedGravity + b.AddedGravity,
                    Grip = a.Grip + b.Grip,
                    ReverseAcceleration = a.ReverseAcceleration + b.ReverseAcceleration,
                    ReverseSpeed = a.ReverseSpeed + b.ReverseSpeed,
                    TopSpeed = a.TopSpeed + b.TopSpeed,
                    Steer = a.Steer + b.Steer,
                };
            }
        }

        public Rigidbody Rigidbody { get; private set; }
        public InputData Input { get; private set; }
        public float AirPercent { get; private set; }
        public float GroundPercent { get; private set; }

        public ArcadeKart2.Stats baseStats = new ArcadeKart2.Stats
        {
            TopSpeed = 10f,
            Acceleration = 5f,
            AccelerationCurve = 4f,
            Braking = 10f,
            ReverseAcceleration = 5f,
            ReverseSpeed = 5f,
            Steer = 5f,
            CoastingDrag = 4f,
            Grip = .95f,
            AddedGravity = 1f,
        };

        [Header("Vehicle Visual")]
        public List<GameObject> m_VisualWheels;

        [Header("Vehicle Physics")]
        [Tooltip("The transform that determines the position of the kart's mass.")]
        public Transform CenterOfMass;

        [Range(0.0f, 20.0f), Tooltip("Coefficient used to reorient the kart in the air. The higher the number, the faster the kart will readjust itself along the horizontal plane.")]
        public float AirborneReorientationCoefficient = 3.0f;

        [Header("Drifting")]
        [Range(0.01f, 1.0f), Tooltip("The grip value when drifting.")]
        public float DriftGrip = 0.4f;
        [Range(0.0f, 10.0f), Tooltip("Additional steer when the kart is drifting.")]
        public float DriftAdditionalSteer = 5.0f;
        [Range(1.0f, 30.0f), Tooltip("The higher the angle, the easier it is to regain full grip.")]
        public float MinAngleToFinishDrift = 10.0f;
        [Range(0.01f, 0.99f), Tooltip("Mininum speed percentage to switch back to full grip.")]
        public float MinSpeedPercentToFinishDrift = 0.5f;
        [Range(1.0f, 20.0f), Tooltip("The higher the value, the easier it is to control the drift steering.")]
        public float DriftControl = 10.0f;
        [Range(0.0f, 20.0f), Tooltip("The lower the value, the longer the drift will last without trying to control it by steering.")]
        public float DriftDampening = 10.0f;

        [Header("VFX")]
        [Tooltip("VFX that will be placed on the wheels when drifting.")]
        public ParticleSystem DriftSparkVFX;
        [Range(0.0f, 0.2f), Tooltip("Offset to displace the VFX to the side.")]
        public float DriftSparkHorizontalOffset = 0.1f;
        [Range(0.0f, 90.0f), Tooltip("Angle to rotate the VFX.")]
        public float DriftSparkRotation = 17.0f;
        [Tooltip("VFX that will be placed on the wheels when drifting.")]
        public GameObject DriftTrailPrefab;
        [Range(-0.1f, 0.1f), Tooltip("Vertical to move the trails up or down and ensure they are above the ground.")]
        public float DriftTrailVerticalOffset;
        [Tooltip("VFX that will spawn upon landing, after a jump.")]
        public GameObject JumpVFX;
        [Tooltip("VFX that is spawn on the nozzles of the kart.")]
        public GameObject NozzleVFX;
        [Tooltip("List of the kart's nozzles.")]
        public List<Transform> Nozzles;

        [Header("Suspensions")]
        [Tooltip("The maximum extension possible between the kart's body and the wheels.")]
        [Range(0.0f, 1.0f)]
        public float SuspensionHeight = 0.2f;
        [Range(10.0f, 100000.0f), Tooltip("The higher the value, the stiffer the suspension will be.")]
        public float SuspensionSpring = 20000.0f;
        [Range(0.0f, 5000.0f), Tooltip("The higher the value, the faster the kart will stabilize itself.")]
        public float SuspensionDamp = 500.0f;
        [Tooltip("Vertical offset to adjust the position of the wheels relative to the kart's body.")]
        [Range(-1.0f, 1.0f)]
        public float WheelsPositionVerticalOffset = 0.0f;

        [Header("Physical Wheels")]
        [Tooltip("The physical representations of the Kart's wheels.")]
        public WheelCollider FrontLeftWheel;
        public WheelCollider FrontRightWheel;
        public WheelCollider RearLeftWheel;
        public WheelCollider RearRightWheel;

        [Tooltip("Which layers the wheels will detect.")]
        public LayerMask GroundLayers = Physics.DefaultRaycastLayers;

        // the input sources that can control the kart
        IInput[] m_Inputs;

        const float k_NullInput = 0.01f;
        const float k_NullSpeed = 0.01f;
        Vector3 m_VerticalReference = Vector3.up;

        // Drift params
        public bool WantsToDrift { get; private set; } = false;
        public bool IsDrifting { get; private set; } = false;
        float m_CurrentGrip = 1.0f;
        float m_DriftTurningPower = 0.0f;
        float m_PreviousGroundPercent = 1.0f;
        readonly List<(GameObject trailRoot, WheelCollider wheel, TrailRenderer trail)> m_DriftTrailInstances = new List<(GameObject, WheelCollider, TrailRenderer)>();
        readonly List<(WheelCollider wheel, float horizontalOffset, float rotation, ParticleSystem sparks)> m_DriftSparkInstances = new List<(WheelCollider, float, float, ParticleSystem)>();

        // can the kart move?
        bool m_CanMove = true;
        List<StatPowerup> m_ActivePowerupList = new List<StatPowerup>();
        ArcadeKart2.Stats m_FinalStats;
       // m_FinalStats.TopSpeed = 10;//baseStats.TopSpeed;

        Quaternion m_LastValidRotation;
        Vector3 m_LastValidPosition;
        Vector3 m_LastCollisionNormal;
        bool m_HasCollision;
        bool m_InAir = false;

        public void AddPowerup(StatPowerup statPowerup) => m_ActivePowerupList.Add(statPowerup);
        public void SetCanMove(bool move) => m_CanMove = move;
        public float GetMaxSpeed() => Mathf.Max(m_FinalStats.TopSpeed, m_FinalStats.ReverseSpeed);

        private void ActivateDriftVFX(bool active)
        {
            foreach (var vfx in m_DriftSparkInstances)
            {
                if (active && vfx.wheel.GetGroundHit(out WheelHit hit))
                {
                    if (!vfx.sparks.isPlaying)
                        vfx.sparks.Play();
                }
                else
                {
                    if (vfx.sparks.isPlaying)
                        vfx.sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

            }

            foreach (var trail in m_DriftTrailInstances)
                trail.Item3.emitting = active && trail.wheel.GetGroundHit(out WheelHit hit);
        }

        private void UpdateDriftVFXOrientation()
        {
            foreach (var vfx in m_DriftSparkInstances)
            {
                vfx.sparks.transform.position = vfx.wheel.transform.position - (vfx.wheel.radius * Vector3.up) + (DriftTrailVerticalOffset * Vector3.up) + (transform.right * vfx.horizontalOffset);
                vfx.sparks.transform.rotation = transform.rotation * Quaternion.Euler(0.0f, 0.0f, vfx.rotation);
            }

            foreach (var trail in m_DriftTrailInstances)
            {
                trail.trailRoot.transform.position = trail.wheel.transform.position - (trail.wheel.radius * Vector3.up) + (DriftTrailVerticalOffset * Vector3.up);
                trail.trailRoot.transform.rotation = transform.rotation;
            }
        }

        void UpdateSuspensionParams(WheelCollider wheel)
        {
            wheel.suspensionDistance = SuspensionHeight;
            wheel.center = new Vector3(0.0f, WheelsPositionVerticalOffset, 0.0f);
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = SuspensionSpring;
            spring.damper = SuspensionDamp;
            wheel.suspensionSpring = spring;
        }

        void Awake()
        {
            Debug.Log("Begin Awake");
            Rigidbody = GetComponent<Rigidbody>();
            m_Inputs = GetComponents<IInput>();

            UpdateSuspensionParams(FrontLeftWheel);
            UpdateSuspensionParams(FrontRightWheel);
            UpdateSuspensionParams(RearLeftWheel);
            UpdateSuspensionParams(RearRightWheel);

            m_CurrentGrip = baseStats.Grip;

            if (DriftSparkVFX != null)
            {
                AddSparkToWheel(RearLeftWheel, -DriftSparkHorizontalOffset, -DriftSparkRotation);
                AddSparkToWheel(RearRightWheel, DriftSparkHorizontalOffset, DriftSparkRotation);
            }

            if (DriftTrailPrefab != null)
            {
                AddTrailToWheel(RearLeftWheel);
                AddTrailToWheel(RearRightWheel);
            }

            if (NozzleVFX != null)
            {
                foreach (var nozzle in Nozzles)
                {
                    Instantiate(NozzleVFX, nozzle, false);
                }
            }
            m_FinalStats = baseStats;
            Debug.Log("End Awake");
        }

        void AddTrailToWheel(WheelCollider wheel)
        {
            GameObject trailRoot = Instantiate(DriftTrailPrefab, gameObject.transform, false);
            TrailRenderer trail = trailRoot.GetComponentInChildren<TrailRenderer>();
            trail.emitting = false;
            m_DriftTrailInstances.Add((trailRoot, wheel, trail));
        }

        void AddSparkToWheel(WheelCollider wheel, float horizontalOffset, float rotation)
        {
            GameObject vfx = Instantiate(DriftSparkVFX.gameObject, wheel.transform, false);
            ParticleSystem spark = vfx.GetComponent<ParticleSystem>();
            spark.Stop();
            m_DriftSparkInstances.Add((wheel, horizontalOffset, -rotation, spark));
        }

        void Update()
        {
            //Debug.Log("Begin Update");
          //  Debug.DrawRay(transform.position, Rigidbody.velocity, Color.green);
           
            int speed = (int)Rigidbody.velocity.magnitude;
            //Debug.Log("speed: " + speed);            
            speedText.text = speed.ToString() + "m/s";
            //Debug.Log("End Update");
            
            foreach (Transform child in ovalTrack)
            {
                //Debug.Log("name: " + transform.name + " pos: " + transform.position);
                foreach (Transform modularTrack in child)
                {
                    Vector3 a, b, c, d;
                    Vector3 trailEdge, leadEdge;
                    if (modularTrack.name.StartsWith("Sector"))
                    {
                        Sector sector = (Sector)modularTrack.GetComponent("Sector");
                        //Vector3[] normals = new Vector3[24];
                        //Vector3[] v = new Vector3[24];
                        //Debug.Log("name: " + sector.name);
                        //Debug.Log("corners " + sector.corners.Length);
                       
                        for (int i = 0; i < sector.corners.Length-1; i++)
                        { 
                           
                          //  Debug.Log("corners[" + i + "] = " + sector.corners[i]);
                            
                            switch (i)
                            {
                                //case 13:
                                //    Debug.DrawLine(sector.corners[i], sector.corners[i + 1], Color.green);
                                //    break;
                                //case 4:
                                //    Debug.DrawLine(sector.corners[i], sector.corners[i + 1], Color.green);
                                //    break;
                                //case 5:
                                //    Debug.DrawLine(sector.corners[i], sector.corners[i + 1], Color.green);
                                 //   break;
                                //case 12:
                                //    Debug.DrawLine(sector.corners[i], sector.corners[i + 1], Color.green);
                                //    break;
                                default:
                                   // Debug.DrawLine(sector.corners[i], sector.corners[i + 1], Color.blue);
                                    break;

                            }
                            
                            //Vector3 midVector = (sector.corners[i + 1] - sector.corners[i]);
                            //float halfLength = midVector.magnitude / 2;
                            //midVector = sector.corners[i] + halfLength * midVector.normalized;


                            //normals[i] = Vector3.Cross(sector.corners[i], sector.corners[i + 1]).normalized;

                            //Debug.DrawLine(midVector, midVector + normals[i], Color.red);


                            //    Debug.DrawLine(sector.corners[0], sector.corners[1]);
                            //Debug.DrawLine(sector.corners[0], sector.corners[2]);
                            //Debug.DrawLine(sector.corners[1], sector.corners[3]);
                            //Debug.DrawLine(sector.corners[2], sector.corners[3]);
                            //if (i != 23)
                            //{

                            //normals[i] = normalOfOneFace(sector.transform.position, sector.corners[i], sector.corners[i + 1], ref v[i]);


                            //  }
                            // else
                            // {
                            //     Debug.DrawLine(sector.corners[i], sector.corners[0]);
                            ////     normals[i] = normalOfOneFace(sector.transform.position, sector.corners[i], sector.corners[0], ref v[i]);
                            // }
                        }

                        Vector3 midVector = (sector.corners[4 + 1] - sector.corners[4]);
                        float halfLength = midVector.magnitude / 2;
                        midVector = sector.corners[4] + halfLength * midVector.normalized;

                        // trailing corners
                        a = sector.corners[6] - sector.corners[5];
                        b = sector.corners[5] - sector.corners[4];
                        trailEdge = b;
                        // leading edge corners
                        c = sector.corners[13] - sector.corners[12];
                        d = sector.corners[14] - sector.corners[13];
                        leadEdge = d;

                        Vector3 normalOfTrailingEdge = Vector3.Cross(a, b).normalized;
                        Vector3 normalOfLeadingEdge = Vector3.Cross(d, c).normalized;
                        Vector3 normOfLeftEdge, normOfRightEdge;
                        
                        Vector3 pointOfInterest = this.transform.position;
                        Vector3 pointOnTrailingEdge = sector.corners[5];
                        Vector3 pointOnLeadingEdge = sector.corners[13];
                        Vector3 leadingEdgeToPt = pointOfInterest - pointOnLeadingEdge;
                        Vector3 trailingEdgeToPt = pointOfInterest - pointOnTrailingEdge;

                        Vector3 pointOnLeftEdgeToKart = pointOfInterest - sector.corners[5];
                        Vector3 pointOnRightEdgeToKart = pointOfInterest - sector.corners[13];

                        //Debug.DrawLine(sector.corners[5], sector.corners[9], Color.cyan);
                       // Debug.DrawLine(sector.corners[13], sector.corners[16], Color.cyan);
                        Vector3 leftEdge = sector.corners[5] - sector.corners[9];
                        Vector3 rightEdge = sector.corners[13] - sector.corners[16];
                        //Debug.DrawLine(sector.corners[4] , midVector, Color.yellow);
                        //Debug.DrawLine(midVector, midVector + normalOfLeadingEdge, Color.red);

                        //if (modularTrack.name == "Sector14")
                        //{
                        //Debug.DrawLine(sector.corners[5], pointOfInterest, Color.white);
                        Vector3 projOnLeftEdge = Vector3.Project(pointOnLeftEdgeToKart, leftEdge);

                        
                        if (modularTrack.name == "Sector4")
                            normOfLeftEdge = -sector.transform.right;
                        else
                            normOfLeftEdge = sector.transform.right;//pointOfInterest - (sector.corners[5] + projOnLeftEdge);
                        normOfLeftEdge = normOfLeftEdge.normalized;
                        

                        // Debug.DrawLine(sector.corners[13], pointOfInterest, Color.white);
                        Vector3 projOnRightEdge = Vector3.Project(pointOnRightEdgeToKart, rightEdge);

                        if (modularTrack.name == "Sector4")
                            normOfRightEdge = sector.transform.right;//pointOfInterest - (sector.corners[13] + projOnRightEdge);
                        else
                            normOfRightEdge = -sector.transform.right;
                        normOfRightEdge = normOfRightEdge.normalized;
                       

                            
                        leftEdge = sector.corners[9] - sector.corners[5];
                        rightEdge = sector.corners[16] - sector.corners[13];
                        trailEdge = sector.corners[4] - sector.corners[5];
                        if (modularTrack.name == "Sector4" || modularTrack.name == "Sector8" || modularTrack.name == "Sector18" || modularTrack.name == "Sector22")
                        {
                            //Debug.DrawLine(sector.corners[5] + projOnLeftEdge, sector.corners[5] + projOnLeftEdge + normOfLeftEdge, Color.white);
                            //Debug.DrawLine(sector.corners[5], sector.corners[5] + pointOnLeftEdgeToKart, Color.white);
                            //Debug.DrawLine(sector.corners[5], sector.corners[5] + projOnLeftEdge, Color.white);

                            //Debug.DrawLine(sector.corners[13], sector.corners[13] + projOnRightEdge, Color.green);
                            //Debug.DrawLine(sector.corners[13] + projOnRightEdge, sector.corners[13] + projOnRightEdge + normOfRightEdge, Color.green);
                            //Debug.DrawLine(sector.corners[13], sector.corners[13] + pointOnRightEdgeToKart, Color.green);
                            //Debug.DrawLine(sector.corners[16], sector.corners[16] + Vector3.up,Color.black);
                            sector.backLeftCorner = sector.corners[5];
                            sector.backRightCorner = sector.corners[14];
                            sector.frontLeftCorner = sector.corners[9];
                            sector.frontRightCorner = sector.corners[16];

                            //Debug.DrawLine(sector.frontLeftCorner, sector.frontLeftCorner + Vector3.up);
                            //sector.setEdgeNorms();

                            //pointOnLeadingEdge = sector.corners[5];
                            //Vector3 leadingEdge = sector.corners[14] - sector.corners[5];
                            //Vector3 halfLeadingEdge = leadingEdge / 2.0f;
                            //Debug.DrawLine(sector.corners[5], sector.corners[5] + halfLeadingEdge, Color.green);
                            //normalOfLeadingEdge = (Quaternion.AngleAxis(-90, Vector3.up) * halfLeadingEdge).normalized;
                            //Debug.DrawLine(sector.corners[5] + halfLeadingEdge, sector.corners[5] + halfLeadingEdge + normalOfLeadingEdge, Color.green);
                            //normalOfTrailingEdge = -normalOfTrailingEdge;
                            //// Debug.DrawLine(pointOnLeadingEdge, transform.position, Color.green);
                            //leadingEdgeToPt = transform.position - pointOnLeadingEdge;
                            //Debug.DrawLine(pointOnLeadingEdge, pointOnLeadingEdge + leadingEdgeToPt, Color.green);

                            //pointOnTrailingEdge = sector.corners[9];
                            //Vector3 trailingEdge = sector.corners[16] - sector.corners[9];
                            ////Debug.DrawLine(sector.corners[9], sector.corners[16], Color.blue);
                            //Vector3 halfTrailingingEdge = trailingEdge / 2.0f;
                            //Debug.DrawLine(sector.corners[9], sector.corners[9] + halfTrailingingEdge, Color.blue);
                            //normalOfTrailingEdge = (Quaternion.AngleAxis(90, Vector3.up) * halfTrailingingEdge).normalized;
                            //Debug.DrawLine(sector.corners[9] + halfTrailingingEdge, sector.corners[9] + halfTrailingingEdge + normalOfTrailingEdge, Color.blue);
                            //trailingEdgeToPt = transform.position - pointOnTrailingEdge;
                            //Debug.DrawLine(pointOnTrailingEdge, pointOnTrailingEdge + trailingEdgeToPt, Color.blue);

                            //Vector3 pointOnLeftEdge = sector.corners[5];
                            ////Debug.DrawLine(pointOnLeftEdge, sector.corners[9], Color.red);
                            //Vector3 halfLeftEdge = leftEdge / 2.0f;
                            //Debug.DrawLine(pointOnLeftEdge, pointOnLeftEdge + halfLeftEdge, Color.red);
                            //normOfLeftEdge = (Quaternion.AngleAxis(90, Vector3.up) * halfLeftEdge).normalized;
                            //Debug.DrawLine(pointOnLeftEdge + halfLeftEdge, pointOnLeftEdge + halfLeftEdge + normOfLeftEdge, Color.red);
                            //pointOnLeftEdgeToKart = transform.position - pointOnLeftEdge;
                            //Debug.DrawLine(pointOnLeftEdge, pointOnLeftEdge + pointOnLeftEdgeToKart, Color.red);

                            //Vector3 pointOnRightEdge = sector.corners[14];
                            ////Debug.DrawLine(pointOnRightEdge, sector.corners[16], Color.yellow);
                            //rightEdge = sector.corners[16] - pointOnRightEdge;
                            //Vector3 halfRightEdge = rightEdge / 2.0f;
                            //Debug.DrawLine(pointOnRightEdge, pointOnRightEdge + halfRightEdge, Color.yellow);
                            //normOfRightEdge = (Quaternion.AngleAxis(-90, Vector3.up) * halfRightEdge).normalized;
                            //Debug.DrawLine(pointOnRightEdge + halfRightEdge, pointOnRightEdge + halfRightEdge + normOfRightEdge, Color.yellow);
                            //pointOnRightEdgeToKart = transform.position - pointOnRightEdge;
                            //Debug.DrawLine(pointOnRightEdge, pointOnRightEdge + pointOnRightEdgeToKart, Color.yellow);

                            //normalOfTrailingEdge = -normalOfTrailingEdge;
                            //if (Vector3.Dot(pointOnRightEdgeToKart, normOfRightEdge) > 0)
                            //    Debug.Log("Positive side on right edge");
                            //if (Vector3.Dot(pointOnLeftEdgeToKart, normOfLeftEdge) > 0)
                            //    Debug.Log("Positive side on left edge");
                            //if (Vector3.Dot(leadingEdgeToPt, leadEdge) > 0)
                            //    Debug.Log("Positive side on leading edge");
                            //if (Vector3.Dot(trailingEdgeToPt, trailEdge) > 0)
                            //    Debug.Log("Positive side on trailing edge");

                            if (sector.checkPointInsideSector(transform.position))
                            {
                                currentSector = modularTrack.name;
                                Debug.Log("Current Sector is " + modularTrack.name);
                            }
                        }
                        else if (modularTrack.name == "Sector5" || modularTrack.name == "Sector9" || modularTrack.name == "Sector19" || modularTrack.name == "Sector23")
                        {
                            sector.frontLeftCorner = sector.corners[9];
                            sector.frontRightCorner = sector.corners[16];
                            sector.backLeftCorner = sector.corners[5];
                            sector.backRightCorner = sector.corners[14];                            
                            if (sector.checkPointInsideSector(transform.position))
                            {
                                currentSector = modularTrack.name;
                                Debug.Log("Current Sector is " + modularTrack.name);
                            }                           
                        }
                        else if (modularTrack.name == "Sector6" || modularTrack.name == "Sector10" || modularTrack.name == "Sector20" || modularTrack.name == "Sector24")
                        {                            
                            sector.frontLeftCorner = sector.corners[5];
                            sector.frontRightCorner = sector.corners[9];
                            sector.backLeftCorner = sector.corners[14];
                            sector.backRightCorner = sector.corners[16];
                            if (sector.checkPointInsideSector(transform.position))
                            {
                                currentSector = modularTrack.name;
                                Debug.Log("Current Sector is " + modularTrack.name);
                            }
                           // Debug.DrawLine(sector.corners[16], sector.corners[16] + Vector3.up * 5.0f, Color.white);
                        }
                        else if (modularTrack.name == "Sector7" || modularTrack.name == "Sector11" || modularTrack.name == "Sector21" || modularTrack.name == "Sector25")
                        {
                            sector.frontLeftCorner = sector.corners[9];
                            sector.frontRightCorner = sector.corners[16];
                            sector.backLeftCorner = sector.corners[5];
                            sector.backRightCorner = sector.corners[14];
                            if (sector.checkPointInsideSector(transform.position))
                            {
                                currentSector = modularTrack.name;
                                Debug.Log("Current Sector is " + modularTrack.name);
                            }
                            Debug.DrawLine(sector.corners[5], sector.corners[5] + Vector3.up * 3);
                        }
                        else if (Vector3.Dot(pointOnRightEdgeToKart, normOfRightEdge) > 0 &&
                                Vector3.Dot(pointOnLeftEdgeToKart, normOfLeftEdge) > 0 &&
                                Vector3.Dot(leadingEdgeToPt, normalOfLeadingEdge) > 0 &&
                                Vector3.Dot(trailingEdgeToPt, normalOfTrailingEdge) > 0)                           
                        {
                            currentSector = modularTrack.name;
                            Debug.Log("Current Sector is " + modularTrack.name);
                                                                       
                        }
                      //  }
                        //Vector3 normalOfLeftEdge = Vector3.Project(pointOnTrailEdgeToKart, pointOnTrailingEdge);
                        //Vector3 normalOfRightEdge = Vector3.Project(pointOnLeadingEdgeToKart, pointOnLeadingEdge);

                        //midVector = (sector.corners[12 + 1] - sector.corners[12]);
                        //halfLength = midVector.magnitude / 2;
                        //midVector = sector.corners[12] + halfLength * midVector.normalized;
                        ////Debug.DrawLine(sector.corners[12], midVector, Color.white);
                        ////Debug.DrawLine(midVector, midVector + normalOfTrailingEdge, Color.magenta);

                        //float distFromLeadingEdgeToKart = Vector3.Dot(leadingEdgeToPt, normalOfLeadingEdge);
                        //float distFromTrailingEdgeToKart = Vector3.Dot(trailingEdgeToPt, normalOfTrailingEdge);
                        ////float distFromLeftEdgeToKart = Vector3.Dot(
                        //float totalDistAlongTheSector = distFromLeadingEdgeToKart + distFromTrailingEdgeToKart;

                        //float distance = distFromLeadingEdgeToKart / totalDistAlongTheSector;
                        //if (distance >=0 && distance <=1)
                        //{ 
                        //    //Debug.Log("distance: " + distance + " in " + modularTrack.name);
                        //  //  break;
                        //}
                    }
                   
                    //Sector sector = child.FindChild("Sector");
                    //child.FindChild("Sector");
                    //Something(child.gameObject);
                }
            }
        }

        float Clamp(float steer, float min, float max)
        {
            if (steer < min)
                steer = min;
            else if (steer > max)
                steer = max;
            return steer;
        }

        float SteerToTarget(Vector3 dest)
        {
            Vector3 DestForward, cp;

            DestForward = dest - transform.position;
            DestForward.y = 0.0f;
            DestForward.Normalize();

            // Compute the sine between current & destination
            cp = Vector3.Cross(transform.forward, DestForward);            
            float steer = cp.magnitude * m_FinalStats.Steer;

            // Steer left or right ?
            if (cp.y < 0.0f) { steer = -steer; }
            steer = Clamp(steer,-1.0f, 1.0f);
            return steer;
        }

        Vector3 normalOfOneFace(Vector3 point, Vector3 cornerA, Vector3 cornerB, ref Vector3 v)
        {
            Vector3 b = point - cornerA;
            Vector3 a = cornerB - cornerA;
            a = a.normalized;
            Vector3 projOfBonA = Vector3.Project(b, a); // vector to the point on the line directly under point
            Vector3 normal = point - projOfBonA;
            
            return normal;
        }

        void FixedUpdate()
        {
            //Debug.Log("Begin FixedUpdate");
            UpdateSuspensionParams(FrontLeftWheel);
            UpdateSuspensionParams(FrontRightWheel);
            UpdateSuspensionParams(RearLeftWheel);
            UpdateSuspensionParams(RearRightWheel);

            GatherInputs();

            // apply our powerups to create our finalStats
            TickPowerups();
            
            // apply our physics properties
            Rigidbody.centerOfMass = transform.InverseTransformPoint(CenterOfMass.position);

            int groundedCount = 0;
           
            if (FrontLeftWheel.isGrounded && FrontLeftWheel.GetGroundHit(out WheelHit hit))
            {
                Renderer rend = m_VisualWheels[0].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("Universal Render Pipeline/Lit");

                groundedCount++;
            }
            else
            {
                Renderer rend = m_VisualWheels[0].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("NewSurfaceShader");
            }
            if (FrontRightWheel.isGrounded && FrontRightWheel.GetGroundHit(out hit))
            {
                Renderer rend = m_VisualWheels[1].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("Universal Render Pipeline/Lit");
                groundedCount++;
            }

            else
            {
                Renderer rend = m_VisualWheels[1].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("NewSurfaceShader");
            }
            if (RearLeftWheel.isGrounded && RearLeftWheel.GetGroundHit(out hit))
            {
                Renderer rend = m_VisualWheels[2].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("Universal Render Pipeline/Lit");
                groundedCount++;
            }
            else
            {
                Renderer rend = m_VisualWheels[2].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("NewSurfaceShader");
            }
            if (RearRightWheel.isGrounded && RearRightWheel.GetGroundHit(out hit))
            {
                Renderer rend = m_VisualWheels[3].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("Universal Render Pipeline/Lit");

                groundedCount++;
            }
            else
            {
                Renderer rend = m_VisualWheels[3].GetComponent<Renderer>();
                rend.material.shader = Shader.Find("NewSurfaceShader");
            }
    
            // calculate how grounded and airborne we are
            GroundPercent = (float)groundedCount / 4.0f;
            AirPercent = 1 - GroundPercent;
 
            // apply vehicle physics
            if (m_CanMove)
            {
                float steer;
                bool accel = true; // UnityEngine.Input.GetAxis("Accelerate") > 0 ? true : false;
                bool brake = false;// UnityEngine.Input.GetAxis("Brake") > 0 ? true : false;
                                   //accel = Input.Accelerate || accel;
                                   //brake = Input.Brake || brake;
                bool nextSectorFound = false;
                foreach (Transform child in ovalTrack)
                {
                    //Debug.Log("name: " + transform.name + " pos: " + transform.position);
                    Vector3 firstSector = Vector3.zero;
                  
                    foreach (Transform modularTrack in child)
                    {

                        if (!modularTrack.name.StartsWith("Sector"))
                            continue;
                        //if (modularTrack.name != currentSector)
                        //    continue;

                        if (nextSectorFound)
                        {
                            Debug.DrawLine(transform.position, modularTrack.transform.position, Color.white);
                            steer = SteerToTarget(modularTrack.transform.position);
                            MoveVehicle(accel, brake, steer);// Input.TurnInput);
                            nextSectorFound = false;
                            break;
                        }
                        else if (modularTrack.name == currentSector)
                        {
                            nextSectorFound = true;                            
                            if (modularTrack.name == "Sector28")
                            {
                                firstSector = pSector.transform.position;
                                Debug.DrawLine(transform.position, firstSector, Color.white);
                                if (firstSector!=null)
                                { 
                                    steer = SteerToTarget(firstSector);
                                    MoveVehicle(accel, brake, steer);// Input.TurnInput);
                                    nextSectorFound = false;
                                }
                            }
                        }
                    }
                    //if (nextSectorFound)
                    //{                        
                    //    break;
                    //}
                }
                //MoveVehicle(Input.Accelerate, Input.Brake, Input.TurnInput);
                //MoveVehicle(UnityEngine.Input.GetAxis("Accelerate"), Input.Brake, Input.TurnInput);
            }
   
            GroundAirbourne();
            
            m_PreviousGroundPercent = GroundPercent;

            UpdateDriftVFXOrientation();
           // Debug.Log("End FixedUpdate");
        }

        void GatherInputs()
        {
            //Debug.Log("Begin Gather Inputs");
            // reset input
            Input = new InputData();
            WantsToDrift = false;

            // gather nonzero input from our sources
            for (int i = 0; i < m_Inputs.Length; i++)
            {
                Input = m_Inputs[i].GenerateInput();
                WantsToDrift = Input.Brake && Vector3.Dot(Rigidbody.velocity, transform.forward) > 0.0f;
            }
          //  Debug.Log("End Gather Inputs");
        }

        void TickPowerups()
        {
            // remove all elapsed powerups
            m_ActivePowerupList.RemoveAll((p) => { return p.ElapsedTime > p.MaxTime; });

            // zero out powerups before we add them all up
            var powerups = new Stats();

            // add up all our powerups
            for (int i = 0; i < m_ActivePowerupList.Count; i++)
            {
                var p = m_ActivePowerupList[i];

                // add elapsed time
                p.ElapsedTime += Time.fixedDeltaTime;

                // add up the powerups
               // powerups += p.modifiers;
            }

            // add powerups to our final stats
           // m_FinalStats = baseStats + powerups;

            // clamp values in finalstats
            m_FinalStats.Grip = Mathf.Clamp(m_FinalStats.Grip, 0, 1);
        }

        void GroundAirbourne()
        {
            // while in the air, fall faster
            if (AirPercent >= 1)
            {
                Rigidbody.velocity += Physics.gravity * Time.fixedDeltaTime * m_FinalStats.AddedGravity;
            }
        }

        public void Reset()
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = euler.z = 0f;
            transform.rotation = Quaternion.Euler(euler);
        }

        public float LocalSpeed()
        {
            if (m_CanMove)
            {
                float dot = Vector3.Dot(transform.forward, Rigidbody.velocity);
                if (Mathf.Abs(dot) > 0.1f)
                {
                    float speed = Rigidbody.velocity.magnitude;
                    return dot < 0 ? -(speed / m_FinalStats.ReverseSpeed) : (speed / m_FinalStats.TopSpeed);
                }
                return 0f;
            }
            else
            {
                // use this value to play kart sound when it is waiting the race start countdown.
                return Input.Accelerate ? 1.0f : 0.0f;
            }
        }

        void OnCollisionEnter(Collision collision) => m_HasCollision = true;
        void OnCollisionExit(Collision collision) => m_HasCollision = false;

        void OnCollisionStay(Collision collision)
        {
            m_HasCollision = true;
            m_LastCollisionNormal = Vector3.zero;
            float dot = -1.0f;

            foreach (var contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.up) > dot)
                    m_LastCollisionNormal = contact.normal;
            }
        }

        void MoveVehicle(bool accelerate, bool brake, float turnInput)
        {
            //accelerate ? 1.0f : 0.0f)
            float accel = 1;
            float decel = 0;
            //if (accelerate && UnityEngine.Input.GetAxis("Accelerate") > 0.0f)
            //    accel = UnityEngine.Input.GetAxis("Accelerate");
            //if (brake && UnityEngine.Input.GetAxis("Brake") > 0.0f)
            //{
            //    decel = UnityEngine.Input.GetAxis("Brake");
            //}
            //DriftAdditionalSteer = decel*10;
            //Debug.Log("decel: " + decel); //- (brake ? 1.0f : 0.0f);

            float accelInput = accel - decel;

            // manual acceleration curve coefficient scalar
            float accelerationCurveCoeff = 5;
            Vector3 localVel = transform.InverseTransformVector(Rigidbody.velocity); //Transforms a vector from world space to local space

            bool accelDirectionIsFwd = accelInput >= 0;
            bool localVelDirectionIsFwd = localVel.z >= 0;
            //m_FinalStats = baseStats;
            
            // use the max speed for the direction we are going--forward or reverse.
            //Debug.Log("localVelDirectionIsFwd: " + localVelDirectionIsFwd);
            //Debug.Log("m_FinalStats.TopSpeed: " + m_FinalStats.TopSpeed);
            float maxSpeed = localVelDirectionIsFwd ? m_FinalStats.TopSpeed : m_FinalStats.ReverseSpeed;
            float accelPower = accelDirectionIsFwd ? m_FinalStats.Acceleration : m_FinalStats.ReverseAcceleration;

            //Debug.Log("maxSpeed: " + maxSpeed);
            float currentSpeed = Rigidbody.velocity.magnitude;
            float accelRampT = currentSpeed / maxSpeed;
            float multipliedAccelerationCurve = m_FinalStats.AccelerationCurve * accelerationCurveCoeff;
            //Debug.Log("multipliedAccelerationCurve: " + multipliedAccelerationCurve);
            //Debug.Log("accelRampT: " + accelRampT);
            
            float accelRamp = Mathf.Lerp(multipliedAccelerationCurve, 1, accelRampT * accelRampT);

            bool isBraking = false;// (localVelDirectionIsFwd && brake) || (!localVelDirectionIsFwd && accelerate);

            // if we are braking (moving reverse to where we are going)
            // use the braking accleration instead
            float finalAccelPower = isBraking ? m_FinalStats.Braking : accelPower;
            //Debug.Log("finalAccelPower: " + finalAccelPower);
            //Debug.Log("accelRamp: " + accelRamp);
            float finalAcceleration = finalAccelPower * accelRamp;

            // apply inputs to forward/backward
            float turningPower = IsDrifting ? m_DriftTurningPower : turnInput * m_FinalStats.Steer;
            //Debug.Log("turnInput: " + turnInput);
            Quaternion turnAngle = Quaternion.AngleAxis(turningPower, transform.up);
            Vector3 fwd = turnAngle * transform.forward;
            //Debug.Log("fwd: " + fwd);
            //Debug.Log("final acceleration: " + finalAcceleration);

            Vector3 movement = fwd * accelInput * finalAcceleration * ((m_HasCollision || GroundPercent > 0.0f) ? 1.0f : 0.0f);

            // forward movement
            bool wasOverMaxSpeed = currentSpeed >= maxSpeed;

            // if over max speed, cannot accelerate faster.
            if (wasOverMaxSpeed && !isBraking)
                movement *= 0.0f;

            //Debug.Log("rigid vel: " + Rigidbody.velocity);
            //Debug.Log("movement: " + movement);
            Vector3 newVelocity = Rigidbody.velocity + movement * Time.fixedDeltaTime;
            newVelocity.y = Rigidbody.velocity.y;

            //  clamp max speed if we are on ground
            if (GroundPercent > 0.0f && !wasOverMaxSpeed)
            {
                newVelocity = Vector3.ClampMagnitude(newVelocity, maxSpeed);
            }
            //Debug.Log("accelInput: " + accelInput);
            // coasting is when we aren't touching accelerate
            if (Mathf.Abs(accelInput) < k_NullInput && GroundPercent > 0.0f)
            {
                newVelocity = Vector3.MoveTowards(newVelocity, new Vector3(0, Rigidbody.velocity.y, 0), Time.fixedDeltaTime * m_FinalStats.CoastingDrag);
            }

          
            Rigidbody.velocity = newVelocity;

            // Drift
            if (GroundPercent > 0.0f)
            {
                if (m_InAir)
                {
                    m_InAir = false;
                    Instantiate(JumpVFX, transform.position, Quaternion.identity);
                }

                // manual angular velocity coefficient
                float angularVelocitySteering = 0.4f;
                float angularVelocitySmoothSpeed = 20f;

                // turning is reversed if we're going in reverse and pressing reverse
                if (!localVelDirectionIsFwd && !accelDirectionIsFwd)
                    angularVelocitySteering *= -1.0f;

                var angularVel = Rigidbody.angularVelocity;

                // move the Y angular velocity towards our target (essentially the same as Mathf.Lerp)
                angularVel.y = Mathf.MoveTowards(angularVel.y, turningPower * angularVelocitySteering, Time.fixedDeltaTime * angularVelocitySmoothSpeed);

                // apply the angular velocity
                Rigidbody.angularVelocity = angularVel;

                // rotate rigidbody's velocity as well to generate immediate velocity redirection
                // manual velocity steering coefficient
                float velocitySteering = 25f;

                // If the karts lands with a forward not in the velocity direction, we start the drift
                if (GroundPercent >= 0.0f && m_PreviousGroundPercent < 0.1f)
                {
                    Vector3 flattenVelocity = Vector3.ProjectOnPlane(Rigidbody.velocity, m_VerticalReference).normalized;
                    if (Vector3.Dot(flattenVelocity, transform.forward * Mathf.Sign(accelInput)) < Mathf.Cos(MinAngleToFinishDrift * Mathf.Deg2Rad))
                    {
                        IsDrifting = true;
                        m_CurrentGrip = DriftGrip;
                        m_DriftTurningPower = 0.0f;
                    }
                }

                // Drift Management
                if (!IsDrifting)
                {
                    if ((WantsToDrift || isBraking) && currentSpeed > maxSpeed * MinSpeedPercentToFinishDrift)
                    {
                        IsDrifting = true;
                        m_DriftTurningPower = turningPower + (Mathf.Sign(turningPower) * DriftAdditionalSteer);
                        m_CurrentGrip = DriftGrip;

                        ActivateDriftVFX(true);
                    }
                }

                if (IsDrifting)
                {
                    float turnInputAbs = Mathf.Abs(turnInput);
                    if (turnInputAbs < k_NullInput)
                        m_DriftTurningPower = Mathf.MoveTowards(m_DriftTurningPower, 0.0f, Mathf.Clamp01(DriftDampening * Time.fixedDeltaTime));

                    // Update the turning power based on input
                    float driftMaxSteerValue = m_FinalStats.Steer + DriftAdditionalSteer * decel;
                    m_DriftTurningPower = Mathf.Clamp(m_DriftTurningPower + (turnInput * Mathf.Clamp01(DriftControl * Time.fixedDeltaTime)), -driftMaxSteerValue, driftMaxSteerValue);

                    bool facingVelocity = Vector3.Dot(Rigidbody.velocity.normalized, transform.forward * Mathf.Sign(accelInput)) > Mathf.Cos(MinAngleToFinishDrift * Mathf.Deg2Rad);

                    bool canEndDrift = true;
                    if (isBraking)
                        canEndDrift = false;
                    else if (!facingVelocity)
                        canEndDrift = false;
                    else if (turnInputAbs >= k_NullInput && currentSpeed > maxSpeed * MinSpeedPercentToFinishDrift)
                        canEndDrift = false;

                    if (canEndDrift || currentSpeed < k_NullSpeed)
                    {
                        // No Input, and car aligned with speed direction => Stop the drift
                        IsDrifting = false;
                        m_CurrentGrip = m_FinalStats.Grip;
                    }

                }

                // rotate our velocity based on current steer value

                Rigidbody.velocity = Quaternion.AngleAxis(turningPower * Mathf.Sign(localVel.z) * velocitySteering * m_CurrentGrip * Time.fixedDeltaTime, transform.up) * Rigidbody.velocity;

            }
            else
            {
                m_InAir = true;
            }

            bool validPosition = false;
            if (Physics.Raycast(transform.position + (transform.up * 0.1f), -transform.up, out RaycastHit hit, 3.0f, 1 << 9 | 1 << 10 | 1 << 11)) // Layer: ground (9) / Environment(10) / Track (11)
            {
                Vector3 lerpVector = (m_HasCollision && m_LastCollisionNormal.y > hit.normal.y) ? m_LastCollisionNormal : hit.normal;
                m_VerticalReference = Vector3.Slerp(m_VerticalReference, lerpVector, Mathf.Clamp01(AirborneReorientationCoefficient * Time.fixedDeltaTime * (GroundPercent > 0.0f ? 10.0f : 1.0f)));    // Blend faster if on ground
            }
            else
            {
                Vector3 lerpVector = (m_HasCollision && m_LastCollisionNormal.y > 0.0f) ? m_LastCollisionNormal : Vector3.up;
                m_VerticalReference = Vector3.Slerp(m_VerticalReference, lerpVector, Mathf.Clamp01(AirborneReorientationCoefficient * Time.fixedDeltaTime));
            }

            validPosition = GroundPercent > 0.7f && !m_HasCollision && Vector3.Dot(m_VerticalReference, Vector3.up) > 0.9f;

            // Airborne / Half on ground management
            if (GroundPercent < 0.7f)
            {
                Rigidbody.angularVelocity = new Vector3(0.0f, Rigidbody.angularVelocity.y * 0.98f, 0.0f);
                Vector3 finalOrientationDirection = Vector3.ProjectOnPlane(transform.forward, m_VerticalReference);
                finalOrientationDirection.Normalize();
                if (finalOrientationDirection.sqrMagnitude > 0.0f)
                {
                    Rigidbody.MoveRotation(Quaternion.Lerp(Rigidbody.rotation, Quaternion.LookRotation(finalOrientationDirection, m_VerticalReference), Mathf.Clamp01(AirborneReorientationCoefficient * Time.fixedDeltaTime)));
                }
            }
            else if (validPosition)
            {
                m_LastValidPosition = transform.position;
                m_LastValidRotation.eulerAngles = new Vector3(0.0f, transform.rotation.y, 0.0f);
            }

            ActivateDriftVFX(IsDrifting && GroundPercent > 0.0f);
        }
    }
}
