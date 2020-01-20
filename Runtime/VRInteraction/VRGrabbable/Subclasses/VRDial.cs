using System;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	[RequireComponent(typeof(Rigidbody))]
	public class VRDial : VRGrabbable
	{
		public Vector3 dialAxis = Vector3.forward;
		private Rigidbody rb;
		public Quaternion lastGrabbedRotation;
		public Vector3 lastGrabbedPosition;
		public float minAngle = -100;
		public float maxAngle = 100;
		public bool useLimits = false;
		/// <summary>
		/// float currentAngleDeg, float deltaAngleDeg, bool localInput
		/// </summary>
		public Action<float, float, bool> DialTurned;
		public float multiplier = 1;

		[Tooltip("How much to use position of hand rather than rotation")]
		[Range(0, 1)]
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

		//[HideInInspector]
		public float currentAngle;
		[Tooltip("Set this in inspector to change the starting angle\n" +
			"The object should still be at 0deg")]
		public float goalAngle;

		private float vibrationDelta = 10;
		private float vibrationDeltaSum = 0;


		// Use this for initialization
		private void Start()
		{
			rb = GetComponent<Rigidbody>();
		}

		private void Update()
		{

			if (!locallyOwned)
			{
				float angleDifference = goalAngle - currentAngle;
				if (Mathf.Abs(angleDifference) > 0)
				{
					currentAngle = currentAngle + angleDifference / 10f;
					transform.Rotate(dialAxis, angleDifference / 10f, Space.Self);
				}
			}


			if (grabbedBy != null)
			{
				GrabInput();
			}

		}

		protected void GrabInput()
		{

			#region Position of the hand

			// the direction vectors to the two hand positions
			Vector3 posDiff = grabbedBy.position - transform.position;
			Vector3 lastPosDiff = lastGrabbedPosition - transform.position;

			Vector3 localDialAxis = transform.TransformDirection(dialAxis);

			// remove the rotation-axis component of the vectors
			posDiff = Vector3.ProjectOnPlane(posDiff, localDialAxis);
			lastPosDiff = Vector3.ProjectOnPlane(lastPosDiff, localDialAxis);

			// convert them into a rotation
			float angleDiff = Vector3.SignedAngle(lastPosDiff, posDiff, localDialAxis);
			if (Vector3.Angle(Vector3.up, localDialAxis) > 90)
			{
				//angleDiff *= -1;
			}

			if (useLimits)
			{
				float nextAngle = currentAngle + angleDiff;

				if (nextAngle > maxAngle)
				{
					angleDiff = maxAngle - currentAngle;
				}
				else if (nextAngle < minAngle)
				{
					angleDiff = minAngle - currentAngle;
				}
			}

			float rotationBasedOnHandPosition = currentAngle + angleDiff;

			lastGrabbedPosition = grabbedBy.position;

			#endregion


			#region Rotation of the hand

			// get the rotation of the hand in the last frame
			Quaternion diff = grabbedBy.rotation * Quaternion.Inverse(lastGrabbedRotation);

			// convert to angle axis
			diff.ToAngleAxis(out float angle, out Vector3 axis);

			// adjust speed
			angle *= multiplier;

			float rotationBaseOnHandRotation = currentAngle;

			// angle should be > 0 if there is a change
			if (angle > 0)
			{
				if (angle >= 180)
				{
					angle = angle - 360;
				}

				Vector3 moment = angle * axis;

				//project the moment vector onto the dial axis
				float newAngle = Vector3.Dot(moment, transform.localToWorldMatrix.MultiplyVector(dialAxis)) / transform.lossyScale.x;

				if (useLimits)
				{
					float nextAngle = currentAngle + newAngle;

					if (nextAngle > maxAngle)
					{
						newAngle = maxAngle - currentAngle;
					}
					else if (nextAngle < minAngle)
					{
						newAngle = minAngle - currentAngle;
					}
				}

				lastGrabbedRotation = grabbedBy.rotation;

				rotationBaseOnHandRotation = currentAngle + newAngle;
			}

			#endregion


			// set mix
			if (dynamicPositionMix)
			{
				positionMix = Mathf.Clamp01(Vector3.Distance(transform.position, grabbedBy.position) * dynamicPositionMixDistanceMultiplier);
			}


			float finalAngle = 0;

			switch (mixingMethod)
			{
				case MixingMethod.WeightedAvg:
					finalAngle = positionMix * rotationBasedOnHandPosition + (1 - positionMix) * rotationBaseOnHandRotation;
					break;
				case MixingMethod.Max:
					finalAngle = Mathf.Abs(rotationBasedOnHandPosition) > Mathf.Abs(rotationBaseOnHandRotation) ?
						rotationBasedOnHandPosition :
						rotationBaseOnHandRotation;
					break;
				case MixingMethod.Min:
					finalAngle = Mathf.Abs(rotationBasedOnHandPosition) < Mathf.Abs(rotationBaseOnHandRotation) ?
						rotationBasedOnHandPosition :
						rotationBaseOnHandRotation;
					break;
				case MixingMethod.Sum:
					finalAngle = rotationBasedOnHandPosition + rotationBaseOnHandRotation;
					break;
			}


			SetData(finalAngle, true);
		}

		public virtual void SetData(float updatedAngle, bool localInput)
		{

			locallyOwned = localInput;

			if (localInput)
			{
				float angleDifference = updatedAngle - currentAngle;
				currentAngle = updatedAngle;
				transform.Rotate(dialAxis, angleDifference, Space.Self);
				DialTurned?.Invoke(currentAngle, angleDifference, localInput);

				// vibrate
				// vibrate only when rotated by a certain amount
				if (vibrationDeltaSum > vibrationDelta)
				{
					if (listOfGrabbedByHands[0])
					{
						//InputMan.Vibrate(listOfGrabbedByHands[0].thisController, 1f, .2f);
					}
					vibrationDeltaSum = 0;
				}

				vibrationDeltaSum += Mathf.Abs(angleDifference);

			}
			else
			{
				DialTurned?.Invoke(updatedAngle, updatedAngle - goalAngle, false);
				goalAngle = updatedAngle;
			}

		}



		public override void HandleGrab(VRGrabbableHand h)
		{
			if (grabbedBy != null)
			{
				HandleRelease();
			}
			base.HandleGrab(h);

			locallyOwned = true;

			lastGrabbedRotation = grabbedBy.rotation;
			lastGrabbedPosition = grabbedBy.position;
		}

		public override int HandleRelease(VRGrabbableHand h = null)
		{

			//if has inertia, keep turning, otherwise, stop
			base.HandleRelease(h);
			HandleDeselection();
			return 0;
		}

		public override byte[] PackData()
		{
			return BitConverter.GetBytes(currentAngle);
		}

		public override void UnpackData(byte[] data)
		{
			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				SetData(reader.ReadSingle(), false);

			}
		}
	}
}