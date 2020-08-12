using UnityEngine;

namespace unityutilities.VRInteraction
{
	[RequireComponent(typeof(Rigidbody))]
	public class VRDialAdjuster : VRDial
	{

		public VRDial parentDial;
		public float factor = .1f;

		public void Start()
		{
			ResetToParent();
		}

		public override void SetData(float updatedAngle, bool localInput)
		{
			base.SetData(updatedAngle, localInput);
			parentDial.SetData(updatedAngle * factor, localInput);
		}

		public void ResetToParent()
		{
			currentAngle = parentDial.currentAngle / factor;
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			ResetToParent();

			base.HandleGrab(h);

		}
	}
}