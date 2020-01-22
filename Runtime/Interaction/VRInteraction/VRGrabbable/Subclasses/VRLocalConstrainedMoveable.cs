using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class VRLocalConstrainedMoveable : VRGrabbable
	{

		Rigidbody rb;
		public delegate void MovedHandler(Vector3 currentPosition, Quaternion currentRotation, Vector3 deltaPosition, Quaternion deltaRotation);
		public Vector2 x_range;
		public Vector2 y_range;
		public Vector2 z_range;
		public float multiplier = 1;
		public event MovedHandler moved = delegate { };
		public Transform localStartPos;
		private Vector3 localStartPosition;
		private Vector3 startPos;
		private Vector3 handStartPos;
		private Vector3 oldPos;

		void Start()
		{
			rb = this.GetComponent<Rigidbody>();
			if (!localStartPos)
			{
				localStartPosition = transform.localPosition;
			}
			else
			{
				localStartPosition = transform.InverseTransformPoint(localStartPos.position);
			}
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			if (GrabbedBy != null)
			{

				Vector3 newPos = GrabbedBy.transform.position;
				Quaternion newRot = GrabbedBy.transform.rotation;
				Vector3 posDiff = (newPos - startPos);
				posDiff *= multiplier;


				//map the positional movement into the local plane
				Vector3 localPosOffset = transform.worldToLocalMatrix.MultiplyVector(posDiff);
				localPosOffset = new Vector3(
					Mathf.Clamp(localPosOffset.x, x_range.x, x_range.y),
					Mathf.Clamp(localPosOffset.y, y_range.x, y_range.y),
					Mathf.Clamp(localPosOffset.z, z_range.x, z_range.y));


				Quaternion rotDiff = newRot * Quaternion.Inverse(this.transform.rotation);
				if (rb != null && !rb.isKinematic)
				{
					Vector3 vel = posDiff / Time.fixedDeltaTime;
					rb.velocity = vel;

					float angle; Vector3 axis;
					rotDiff.ToAngleAxis(out angle, out axis);
					Vector3 angularVel = axis * angle * Mathf.Deg2Rad / Time.fixedDeltaTime;
					rb.angularVelocity = angularVel;
				}
				else if (rb != null && rb.isKinematic)
				{
					rb.position = startPos + transform.localToWorldMatrix.MultiplyVector(localPosOffset);
					//rb.transform.Translate(localPosOffset);
					//rb.rotation = grabbedBy.rotation;

				}
				else
				{
					//just move it
					this.transform.position = GrabbedBy.transform.position;
					this.transform.rotation = GrabbedBy.transform.rotation;
				}
				moved(newPos, newRot, posDiff, rotDiff);

				oldPos = newPos;
			}
		}

		override public void HandleGrab(VRGrabbableHand h)
		{
			if (GrabbedBy != null)
			{
				HandleRelease();
			}
			handStartPos = GrabbedBy.transform.position;
			startPos = transform.TransformPoint(localStartPosition);
			oldPos = GrabbedBy.transform.position;
		}

		override public void HandleRelease(VRGrabbableHand h = null)
		{
			// There seems to be a Unity bug that this fixes.
			// It doesn't do anything otherwise.
			transform.Translate(0, 1, 0);
			transform.Translate(0, -1, 0);
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
			var oldPos = transform.position;
			var oldRot = transform.rotation;

			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				transform.localPosition = reader.ReadVector3();
				transform.localRotation = reader.ReadQuaternion();


				moved(transform.position, transform.rotation, transform.position - oldPos, transform.rotation * Quaternion.Inverse(oldRot));

			}
		}
	}
}