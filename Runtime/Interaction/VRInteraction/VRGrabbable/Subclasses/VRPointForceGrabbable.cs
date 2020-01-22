using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	[RequireComponent(typeof(Rigidbody))]
	public class VRPointForceGrabbable : VRGrabbable
	{

		Rigidbody rb;

		[Space]
		public float forceMultiplier = 1;
		private Dictionary<VRGrabbableHand, Vector3> localHandGrabPositions = new Dictionary<VRGrabbableHand, Vector3>();

		private new void Awake()
		{
			base.Awake();
			canBeGrabbedByMultipleHands = true;
		}

		void Start()
		{
			rb = GetComponent<Rigidbody>();
		}

		// Update is called once per frame
		void Update()
		{
			if (GrabbedBy != null)
			{
				foreach (var hand in listOfGrabbedByHands)
				{
					Vector3 diff = transform.InverseTransformPoint(hand.transform.position) - localHandGrabPositions[hand];
					rb.AddForceAtPosition(forceMultiplier * 10000 * Time.deltaTime * transform.TransformVector(diff), transform.TransformPoint(localHandGrabPositions[hand]));

				}
			}
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);
			localHandGrabPositions.Add(h, transform.InverseTransformPoint(h.transform.position));
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);
			localHandGrabPositions.Remove(h);
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
			}
		}
	}
}
