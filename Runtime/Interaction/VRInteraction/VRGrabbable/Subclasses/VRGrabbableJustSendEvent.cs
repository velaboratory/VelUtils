using System;
using UnityEngine;
using UnityEngine.Events;
namespace unityutilities.VRInteraction
{
	public class VRGrabbableJustSendEvent : VRGrabbable
	{
		[Serializable]
		public class GrabEvent : UnityEvent { }

		[Space]
		[Tooltip("Optional. Still sends the normal actions")]
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