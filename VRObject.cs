using UnityEngine;

#define OCULUS_UTILITES_AVAILABLE

namespace unityutilities {
	#if OCULUS_UTILITES_AVAILABLE
	public class VRObject : MonoBehaviour {
		void Update() {
			Vector3 pos = OVRPlugin.GetNodePose(OVRPlugin.Node.DeviceObjectZero, OVRPlugin.Step.Render).ToOVRPose().position;
			Quaternion rot = OVRPlugin.GetNodePose(OVRPlugin.Node.DeviceObjectZero, OVRPlugin.Step.Render).ToOVRPose().orientation;

			bool garbageUpdate = pos == Vector3.zero;

			if (garbageUpdate) return;
			var t = transform;
			t.localPosition = pos;
			t.localRotation = rot;
		}
	}
	#endif
}