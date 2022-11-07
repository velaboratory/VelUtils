using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	/// <summary>
	/// Spins 🔄
	/// In VRDial2, the goal position is entirely virtual,
	/// and both remote and local dials try to achieve the goal position with physics in some way.
	/// Limits are defined by hinge joint/physics
	/// </summary>
	[AddComponentMenu("unityutilities/Interaction/VR Hinge Dial")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(HingeJoint))]
	public class VRHingeDial : VRGrabbable
	{
		private Rigidbody rb;
		private HingeJoint hinge;
		private Quaternion lastGrabbedRotation;
		private Vector3 lastGrabbedPosition;
		private float lastAngle;

		/// <summary>
		/// whether this dial is being held in position
		/// </summary>
		public bool Active => networkGrabbed || GrabbedBy != null;


		/// <summary>
		/// float currentAngleDeg, float deltaAngleDeg, bool localInput
		/// </summary>
		public Action<float, float, bool> DialTurned;

		public float multiplier = 1;

		[Tooltip("How much to use position of hand rather than rotation")] [Range(0, 1)]
		public float positionMix = 1;

		[Tooltip("Whether to vary the position mix with distance to the center of the object")]
		public bool dynamicPositionMix = false;

		public float dynamicPositionMixDistanceMultiplier = 10f;

		public MixingMethod mixingMethod;

		public enum MixingMethod
		{
			WeightedAvg,
			Max,
			Min,
			Sum
		}

		public float CurrentAngle => hinge.angle;
		public float currentAngle;

		[Tooltip("Set this in inspector to change the starting angle\n" +
		         "The object should still be at 0deg")]
		public float goalAngle;

		public float goalDeadzoneDeg = .1f;

		private float vibrationDelta = 10;
		private float vibrationDeltaSum = 0;


		private Queue<float> lastRotationVels = new Queue<float>();
		private int lastRotationVelsLength = 10;

		private bool wasUsingSpring;
		private bool wasUsingGravity;


		// Use this for initialization
		private void Start()
		{
			rb = GetComponent<Rigidbody>();
			hinge = GetComponent<HingeJoint>();
			if (hinge.motor.force == 0)
			{
				JointMotor m = hinge.motor;
				m.force = 100;
				hinge.motor = m;
			}

			wasUsingSpring = hinge.useSpring;
		}

		private void Update()
		{
			currentAngle = CurrentAngle;

			// local input
			if (GrabbedBy != null)
			{
				GrabInput();

				// update the last velocities
				float angleDelta = CurrentAngle - lastAngle;
				lastRotationVels.Enqueue(angleDelta);
				if (lastRotationVels.Count > lastRotationVelsLength)
					lastRotationVels.Dequeue();

				// vibrate
				// vibrate only when rotated by a certain amount
				if (vibrationDeltaSum > vibrationDelta)
				{
					InputMan.Vibrate(GrabbedBy.side, 1f, .01f);
					vibrationDeltaSum = 0;
				}

				vibrationDeltaSum += Mathf.Abs(angleDelta);
			}

			hinge.useSpring = wasUsingSpring && !Active;
			rb.useGravity = wasUsingGravity && !Active;


			if (Active)
			{
				// actually move the dial to the goal pos
				float clampedGoalAngle = goalAngle;
				if (hinge.useLimits)
				{
					JointLimits limits = hinge.limits;
					clampedGoalAngle = Mathf.Clamp(goalAngle, limits.min, limits.max);
				}

				float angleDifference = clampedGoalAngle - CurrentAngle;
				if (Mathf.Abs(angleDifference) > goalDeadzoneDeg)
				{
					// JointMotor m = hinge.motor;
					// // get there in 1/10 of a second
					// m.targetVelocity = angleDifference * 10;
					// hinge.motor = m;
					// hinge.useMotor = true;
					transform.Rotate(hinge.axis, angleDifference, Space.Self);
					rb.angularVelocity = Vector3.zero;
				}
				else
				{
					// hinge.useMotor = false;
				}
			}

			lastAngle = CurrentAngle;
		}

		/// <summary>
		/// Sets the goal angle based on local user input
		/// </summary>
		protected void GrabInput()
		{
			#region Position of the hand

			// the direction vectors to the two hand positions
			Vector3 posDiff = GrabbedBy.transform.position - transform.position;
			Vector3 lastPosDiff = lastGrabbedPosition - transform.position;

			Vector3 localDialAxis = transform.TransformDirection(hinge.axis);

			// remove the rotation-axis component of the vectors
			posDiff = Vector3.ProjectOnPlane(posDiff, localDialAxis);
			lastPosDiff = Vector3.ProjectOnPlane(lastPosDiff, localDialAxis);

			// convert them into a rotation
			float angleDiff = Vector3.SignedAngle(lastPosDiff, posDiff, localDialAxis);

			float rotationBasedOnHandPosition = goalAngle + angleDiff;

			lastGrabbedPosition = GrabbedBy.transform.position;

			#endregion


			#region Rotation of the hand

			// get the rotation of the hand in the last frame
			Quaternion diff = GrabbedBy.transform.rotation * Quaternion.Inverse(lastGrabbedRotation);

			// convert to angle axis
			diff.ToAngleAxis(out float angle, out Vector3 axis);

			// adjust speed
			angle *= multiplier;

			float rotationBaseOnHandRotation = goalAngle;

			// angle should be > 0 if there is a change
			if (angle > 0)
			{
				if (angle >= 180)
				{
					angle -= 360;
				}

				Vector3 moment = angle * axis;

				//project the moment vector onto the dial axis
				float newAngle = Vector3.Dot(moment, transform.localToWorldMatrix.MultiplyVector(hinge.axis)) / transform.lossyScale.x;

				lastGrabbedRotation = GrabbedBy.transform.rotation;

				rotationBaseOnHandRotation = goalAngle + newAngle;
			}

			#endregion

			// set mix
			if (dynamicPositionMix)
			{
				positionMix = Mathf.Clamp01(Vector3.Distance(transform.position, GrabbedBy.transform.position) * dynamicPositionMixDistanceMultiplier);
			}

			goalAngle = mixingMethod switch
			{
				MixingMethod.WeightedAvg => positionMix * rotationBasedOnHandPosition + (1 - positionMix) * rotationBaseOnHandRotation,
				MixingMethod.Max => Mathf.Abs(rotationBasedOnHandPosition) > Mathf.Abs(rotationBaseOnHandRotation) ? rotationBasedOnHandPosition : rotationBaseOnHandRotation,
				MixingMethod.Min => Mathf.Abs(rotationBasedOnHandPosition) < Mathf.Abs(rotationBaseOnHandRotation) ? rotationBasedOnHandPosition : rotationBaseOnHandRotation,
				MixingMethod.Sum => rotationBasedOnHandPosition + rotationBaseOnHandRotation,
				_ => goalAngle
			};
		}


		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);

			lastGrabbedRotation = GrabbedBy.transform.rotation;
			lastGrabbedPosition = GrabbedBy.transform.position;

			goalAngle = CurrentAngle;
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

			// add velocity
			if (rb && lastRotationVels.Count > 0)
			{
				// y not convert to rad?
				rb.angularVelocity = hinge.axis * lastRotationVels.Average();
			}
		}

		public override byte[] PackData()
		{
			return BitConverter.GetBytes(goalAngle);
		}

		public override void UnpackData(byte[] data)
		{
			using MemoryStream inputStream = new MemoryStream(data);
			BinaryReader reader = new BinaryReader(inputStream);
			goalAngle = reader.ReadSingle();
		}
	}
}