using UnityEngine;

namespace unityutilities.VRInteraction
{
	[DisallowMultipleComponent]
	public class Climbable : VRGrabbable
	{
		private Movement m;

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);

			if (!m)
				m = h.rig.GetComponent<Movement>();

			if (m)
				m.SetGrabbedObj(transform, h.side);
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

			if (!m)
				m = h.rig.GetComponent<Movement>();

			if (m)
				m.SetGrabbedObj(null, h.side);
		}



		public override byte[] PackData()
		{
			return new byte[0];
		}

		public override void UnpackData(byte[] data) { }
	}
}