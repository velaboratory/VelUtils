using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class VRMoveable : VRGrabbable
	{
		private Rigidbody rb;

		/// <summary>
		/// bool: localInput (was this action cause by grabbing?)
		/// </summary>
		public Action<bool> Moved;

		// public Transform centerOfMass;

		[Space]
		public bool useVelocityFollow = true;
		private bool wasKinematic;
		private bool wasUsingGravity;
		private CopyTransform copyTransform;

		public bool releaseVelocitySmoothing = true;

		// Use this for initialization
		private void Start()
		{
			rb = GetComponent<Rigidbody>();
			wasKinematic = rb.isKinematic;
			wasUsingGravity = rb.useGravity;
			copyTransform = GetComponent<CopyTransform>();
		}

		private void Update()
		{
			if (GrabbedBy != null)
			{
				// TODO only move when changed position
				Moved?.Invoke(true);
			}


			// TODO work out better system
			if (!locallyOwned)
			{
				rb.isKinematic = true;
			}
			else
			{
				rb.isKinematic = wasKinematic;
			}
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);

			rb.useGravity = false;

			// add a copytransform if it doesn't exist
			if (!copyTransform)
			{
				copyTransform = gameObject.AddComponent<CopyTransform>();

				if (useVelocityFollow)
				{
					copyTransform.positionFollowType = CopyTransform.FollowType.Velocity;
					copyTransform.rotationFollowType = CopyTransform.FollowType.Velocity;
					copyTransform.useFixedUpdatePos = true;
					copyTransform.useFixedUpdateRot = true;
				}
				else
				{
					copyTransform.positionFollowType = CopyTransform.FollowType.Copy;
					copyTransform.rotationFollowType = CopyTransform.FollowType.Copy;
				}
				copyTransform.snapIfAngleGreaterThan = 90;
				copyTransform.snapIfDistanceGreaterThan = 1f;
				copyTransform.followPosition = true;
				copyTransform.followRotation = true;

			}

			copyTransform.SetTarget(h.transform);
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

			rb.useGravity = wasUsingGravity;

			if (copyTransform)
			{
				copyTransform.SetTarget(null);
			}
			else
			{
				Debug.LogError("No copy transform on release.", this);
			}

			if (releaseVelocitySmoothing)
			{
				if (h != null)
				{
					// get the average
					rb.velocity = h.lastVels.Aggregate(new Vector3(0, 0, 0), (s, v) => s + v) / (float)h.lastVels.Count;
				}
				else
				{
					Debug.LogError("Can't use release velocity smoothing if you don't supply a hand.");
				}
			}
		}

		Vector3 AvgOfVector3s(Vector3[] list)
		{
			Vector3 total = new Vector3();
			foreach (var item in list)
			{
				total += item;
			}
			return total / list.Length;
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