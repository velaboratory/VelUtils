using OculusSampleFramework;
using System;
using System.Collections;
using UnityEngine;

namespace unityutilities {

	[Serializable]
	public class FingerTool {

		public FingerTool() {
			finger = OVRPlugin.HandFinger.Index;
			leftHand = true;
			rightHand = true;
			avatarHands = true;
			trackedHands = true;
		}

		public Transform toolPrefab;

		public OVRPlugin.HandFinger finger;
		[Space]
		public bool leftHand;
		public bool rightHand;
		[Space]
		public bool avatarHands;
		public bool trackedHands;
	}

	public class AddToolsToFingers : MonoBehaviour {

		public FingerTool[] tools;


		private Hand[] foundHands;

		private void Awake() {
			if (tools != null && tools.Length > 0) {
				StartCoroutine(AttachToolsToHands(tools));
			}
		}


		private IEnumerator AttachToolsToHands(FingerTool[] toolObjects) {
			Hands handsObj;
			while ((handsObj = Hands.Instance) == null || !handsObj.IsInitialized()) {
				yield return null;
			}

			foreach (FingerTool tool in toolObjects) {
				while ((handsObj.LeftHand.Skeleton == null || handsObj.LeftHand.Skeleton.Bones == null) &&
					(handsObj.RightHand.Skeleton == null || handsObj.RightHand.Skeleton.Bones == null)) {
					yield return null;
				}

				if (tool.toolPrefab == null) continue;

				// twice for left and right hands
				for (int i = 0; i < 2; i++) {
					bool isRight = (i == 0);
					if ((isRight && tool.rightHand) || (!isRight && tool.leftHand)) {

						while (!Hands.Instance.IsInitialized()) {
							yield return null;
						}

						Transform parent = null;
						Hand h = null;

						if (foundHands == null) {
							foundHands = FindObjectsOfType<Hand>();
						}
						if (!isRight) {
							foreach (var hand in foundHands) {
								if (hand.HandType == OVRPlugin.Hand.HandLeft) {
									h = hand;
									break;
								}
							}
						}
						else {
							foreach (var hand in foundHands) {
								if (hand.HandType == OVRPlugin.Hand.HandRight) {
									h = hand;
									break;
								}
							}
						}

						if (h == null) {
							Debug.LogError("Couldn't find hands at all.");
							continue;
						}

						switch (tool.finger) {
							case OVRPlugin.HandFinger.Thumb:
								parent = h.Skeleton.Bones[(int)OVRPlugin.BoneId.Hand_ThumbTip];
								break;
							case OVRPlugin.HandFinger.Index:
								parent = h.Skeleton.Bones[(int)OVRPlugin.BoneId.Hand_IndexTip];
								break;
							case OVRPlugin.HandFinger.Middle:
								parent = h.Skeleton.Bones[(int)OVRPlugin.BoneId.Hand_MiddleTip];
								break;
							case OVRPlugin.HandFinger.Ring:
								parent = h.Skeleton.Bones[(int)OVRPlugin.BoneId.Hand_RingTip];
								break;
							case OVRPlugin.HandFinger.Pinky:
								parent = h.Skeleton.Bones[(int)OVRPlugin.BoneId.Hand_PinkyTip];
								break;
							case OVRPlugin.HandFinger.Max:
								break;
						}

						if (parent != null) {
							var newTool = Instantiate(tool.toolPrefab).transform;
							newTool.SetParent(parent);
							newTool.localPosition = Vector3.zero;
							newTool.localRotation = Quaternion.identity;
						} else {
							Debug.LogError("Couldn't find hand finger.");
						}
					}
				}
			}
		}

	}
}