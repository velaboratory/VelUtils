using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	[RequireComponent(typeof(Rigidbody))]
	public class VRPointForceGrabbable : VRGrabbable
	{

		Rigidbody rb;
		public float forceMultiplier = 2000;
		private List<Vector3> localHandGrabPositions = new List<Vector3>();

		void Start()
		{
			rb = GetComponent<Rigidbody>();
		}

		// Update is called once per frame
		void Update()
		{
			if (grabbedBy != null)
			{
				for (int i = 0; i < listOfGrabbedByHands.Count; i++)
				{
					Vector3 diff = transform.InverseTransformPoint(listOfGrabbedByHands[i].transform.position) - localHandGrabPositions[i];
					rb.AddForceAtPosition(forceMultiplier * Time.deltaTime * transform.TransformVector(diff), transform.TransformPoint(localHandGrabPositions[i]));

				}
			}
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);
			localHandGrabPositions.Add(transform.InverseTransformPoint(h.transform.position));
		}

		public override int HandleRelease(VRGrabbableHand h = null)
		{
			int index = base.HandleRelease(h);
			localHandGrabPositions.RemoveAt(index);
			return index;
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
