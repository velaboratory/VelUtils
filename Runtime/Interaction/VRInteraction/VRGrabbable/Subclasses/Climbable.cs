using System;
using UnityEngine;

namespace VelUtils.VRInteraction
{
	[AddComponentMenu("VelUtils/Interaction/Climbable")]
	[DisallowMultipleComponent]
	public class Climbable : VRGrabbable
	{
		private Movement m;

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);

			if (!m) m = h.rig.GetComponent<Movement>();
			if (!m) m = h.rig.GetComponentInParent<Movement>();
			if (!m) m = h.rig.GetComponentInChildren<Movement>();

			if (m) m.SetGrabbedObj(transform, h.side);
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

			if (h != null)
			{
				if (!m) m = h.rig.GetComponent<Movement>();
				if (!m) m = h.rig.GetComponentInParent<Movement>();
				if (!m) m = h.rig.GetComponentInChildren<Movement>();
			}

			// if (m) m.Release(h != null ? h.side : Side.None, GetComponent<Rigidbody>());
			if (m) m.SetGrabbedObj(null, h != null ? h.side : Side.None);
		}

		public override byte[] PackData()
		{
			return Array.Empty<byte>();
		}

		public override void UnpackData(byte[] data)
		{
		}
	}
}