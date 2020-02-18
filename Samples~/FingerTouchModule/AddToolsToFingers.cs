using OculusSampleFramework;
using System;
using System.Collections;
using UnityEngine;

namespace unityutilities
{

	[Serializable]
	public class FingerTool
	{

		public FingerTool()
		{
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

	[RequireComponent(typeof(OvrAvatar))]
	public class AddToolsToFingers : MonoBehaviour
	{

		public FingerTool[] tools;


		private OculusSampleFramework.Hand[] foundHands;
		private OvrAvatar avatar;

		private void Awake()
		{
			if (tools != null && tools.Length > 0)
			{
				StartCoroutine(AttachToolsToAvatarHands(tools));
				StartCoroutine(AttachToolsToTrackedHands(tools));
			}
		}

		private IEnumerator AttachToolsToAvatarHands(FingerTool[] toolObjects)
		{
			avatar = GetComponent<OvrAvatar>();
			while (avatar.HandLeft == null)
			{
				yield return null;
			}


			foreach (FingerTool tool in toolObjects)
			{
				var leftHand = avatar.HandLeft;
				var rightHand = avatar.HandRight;

				if (tool.leftHand)
				{
					TouchMenuFingerCollider leftTool = Instantiate(tool.toolPrefab, leftHand.RenderParts[0].bones[7]).GetComponent<TouchMenuFingerCollider>();
					leftTool.isLeft = true;
				}
				if (tool.rightHand)
				{
					TouchMenuFingerCollider rightTool = Instantiate(tool.toolPrefab, rightHand.RenderParts[0].bones[7]).GetComponent<TouchMenuFingerCollider>();
					rightTool.isLeft = false;
				}
			}
		}

		private IEnumerator AttachToolsToTrackedHands(FingerTool[] toolObjects)
		{

			Hands handsObj;
			while ((handsObj = Hands.Instance) == null || !handsObj.IsInitialized())
			{
				yield return null;
			}

			foreach (FingerTool tool in toolObjects)
			{
				while ((handsObj.LeftHand.Skeleton == null || handsObj.LeftHand.Skeleton.Bones == null) &&
					(handsObj.RightHand.Skeleton == null || handsObj.RightHand.Skeleton.Bones == null))
				{
					yield return null;
				}

				if (tool.toolPrefab == null) continue;

				// twice for left and right hands
				for (int i = 0; i < 2; i++)
				{
					bool isRight = (i == 0);
					if ((isRight && tool.rightHand) || (!isRight && tool.leftHand))
					{

						while (!Hands.Instance.IsInitialized())
						{
							yield return null;
						}

						Transform parent = null;
						OculusSampleFramework.Hand h = null;

						if (foundHands == null)
						{
							foundHands = FindObjectsOfType<OculusSampleFramework.Hand>();
						}
						if (!isRight)
						{
							foreach (OculusSampleFramework.Hand hand in foundHands)
							{
								if (hand.HandType == OVRPlugin.Hand.HandLeft)
								{
									h = hand;
									break;
								}
							}
						}
						else
						{
							foreach (OculusSampleFramework.Hand hand in foundHands)
							{
								if (hand.HandType == OVRPlugin.Hand.HandRight)
								{
									h = hand;
									break;
								}
							}
						}

						if (h == null)
						{
							Debug.LogError("Couldn't find hands at all.");
							continue;
						}

						switch (tool.finger)
						{
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

						if (parent != null)
						{
							var newTool = Instantiate(tool.toolPrefab).transform;
							newTool.SetParent(parent);
							newTool.localPosition = Vector3.zero;
							newTool.localRotation = Quaternion.identity;
						}
						else
						{
							Debug.LogError("Couldn't find hand finger.");
						}
					}
				}
			}
		}

	}
}