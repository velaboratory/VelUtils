using UnityEngine;

namespace unityutilities.VRInteraction
{
	[RequireComponent(typeof(Rigidbody))]
	public class VRDialAdjuster : VRDial
	{

		public VRDial parentDial;
		public float factor = .1f;

		public override void SetData(float updatedAngle, bool localInput)
		{

			base.SetData(updatedAngle, localInput);
			parentDial.SetData(updatedAngle * factor, localInput);
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			if (parentDial.currentAngle != currentAngle)
			{
				currentAngle = parentDial.currentAngle / factor;
			}

			base.HandleGrab(h);

		}
	}
}