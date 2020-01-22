using System;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	/// <summary>
	/// Like the Prism in ENGREDUVR
	/// </summary>
	public class VRMoveableButOnceItsStuckInTheGroundItsADial : VRGrabbable
	{
		public Vector3 dialAxis = Vector3.forward;
		private Vector3 posOffset;

		/// <summary>
		/// bool: localInput (was this action cause by grabbing?)
		/// </summary>
		public Action<bool> Moved;

		private Quaternion rotationOffset;
		public float multiplier = 1;

		[Tooltip("How much to use position of hand rather than rotation")]
		[Range(0, 1)]
		public float positionMix = 0;

		[Tooltip("Whether to vary the position mix with distance to the center of the object")]
		public bool dynamicPositionMix = false;

		public float dynamicPositionMixDistanceMultiplier = 10f;

		private float initialHandToGroundDistance;
		private bool stuckInTheGround;

		//[HideInInspector]
		public float currentAngle;


		public AudioSource lift;
		public AudioSource place;


		private void Update()
		{

			if (GrabbedBy != null)
			{
				GrabInput();
			}

		}

		private void GrabInput()
		{
			#region Stick and Unstick into the ground
			if (!stuckInTheGround)
			{
				// Check if stuck into the ground
				if (Physics.Raycast(transform.position + transform.up, -transform.up, out RaycastHit hit))
				{

					if (hit.distance < 1f)
					{
						stuckInTheGround = true;
						initialHandToGroundDistance = Vector3.Distance(transform.position, GrabbedBy.transform.position);
						place.Play();
					}
				}
			}
			else
			{
				// Check if lifted out of the ground
				if (Vector3.Distance(transform.position, GrabbedBy.transform.position) > initialHandToGroundDistance + .1f)
				{
					stuckInTheGround = false;
					lift.Play();
				}
			}
			#endregion

			#region Move
			if (stuckInTheGround)
			{

				transform.LookAt(GrabbedBy.transform.position, GrabbedBy.transform.forward);
				transform.Rotate(90, 0, 0, Space.Self);
			}

			else
			{

				transform.rotation = GrabbedBy.transform.rotation * rotationOffset;
				transform.position = GrabbedBy.transform.TransformPoint(posOffset);

			}

			Moved?.Invoke(true);
			#endregion

		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			if (GrabbedBy != null)
			{
				HandleRelease();
			}
			base.HandleGrab(h);

			posOffset = GrabbedBy.transform.InverseTransformPoint(transform.position);
			rotationOffset = Quaternion.Inverse(GrabbedBy.transform.rotation) * transform.rotation;

			if (stuckInTheGround)
			{
				initialHandToGroundDistance = Vector3.Distance(transform.position, GrabbedBy.transform.position);
			}
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);
		}

		public override byte[] PackData()
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(outputStream);

				writer.Write(transform.localPosition);
				writer.Write(transform.localRotation);

				return outputStream.ToArray();
			}
		}

		public override void UnpackData(byte[] data)
		{
			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				transform.localPosition = reader.ReadVector3();
				transform.localRotation = reader.ReadQuaternion();

				Moved?.Invoke(false);
			}
		}
	}
}