using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class VRLocalConstrainedMoveable : VRGrabbable
	{

		/// <summary>
		/// 🏃‍ bool: localInput (was this action cause by grabbing?)
		/// </summary>
		public Action<bool> Moved;

		[Header("Local Position Limits (-,+)")]
		public Vector2 x_range;
		public Vector2 y_range;
		public Vector2 z_range;
		[Space]
		public float multiplier = 1;
		public Transform localStartTransform;
		private Vector3 localStartPosition;
		private Vector3 handOffset;

		void Start()
		{
			if (!localStartTransform)
			{
				localStartPosition = transform.localPosition;
			}
			else
			{
				localStartPosition = transform.InverseTransformPoint(localStartTransform.position);
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (GrabbedBy != null)
			{
				// the local position offset in the space of this object
				Vector3 newPos;
				if (transform.parent)
				{
					newPos = transform.parent.InverseTransformPoint(GrabbedBy.transform.TransformPoint(handOffset));
				}
				else
				{
					newPos = GrabbedBy.transform.TransformPoint(handOffset);
				}

				Vector3 posDiff = (newPos - localStartPosition);
				posDiff *= multiplier;


				// only clamp distance away from the starting position
				posDiff = new Vector3(
					Mathf.Clamp(posDiff.x, x_range.x, x_range.y),
					Mathf.Clamp(posDiff.y, y_range.x, y_range.y),
					Mathf.Clamp(posDiff.z, z_range.x, z_range.y));

				//just move it
				transform.localPosition = localStartPosition + posDiff;

				Moved?.Invoke(true);
			}
		}

		override public void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);

			handOffset = GrabbedBy.transform.InverseTransformPoint(transform.position);
		}

		override public void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

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