using System;
using UnityEngine.Events;
namespace unityutilities.VRInteraction

{
	public class VRGrabbableJustSendEvent : VRGrabbable
	{
		[Serializable]
		public class GrabEvent : UnityEvent { }

		public GrabEvent grabEvent;

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);
			grabEvent?.Invoke();
		}


		public override byte[] PackData()
		{
			return new byte[0];
		}

		public override void UnpackData(byte[] data)
		{
		}
	}
}