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

	public class AddToolsToFingers : MonoBehaviour
	{

		[SerializeField]
		private Rig rig;

		[ReadOnly, SerializeField]
		private OVRSkeleton leftHandSkele;
		[ReadOnly, SerializeField]
		private OVRSkeleton rightHandSkele;

		[ReadOnly, SerializeField]
		private OvrAvatar avatar;
	

		public FingerTool[] tools;


		private void Start()
		{
			if (tools != null && tools.Length > 0)
			{
				StartCoroutine(AttachToolsToHands(tools, true));
				StartCoroutine(AttachToolsToHands(tools, false));
			}

			leftHandSkele = rig.leftHand.GetComponentInChildren<OVRSkeleton>();
			rightHandSkele = rig.rightHand.GetComponentInChildren<OVRSkeleton>();

		}


		private IEnumerator AttachToolsToHands(FingerTool[] toolObjects, bool tracked)
		{
			yield return null;

			foreach (FingerTool tool in toolObjects)
			{
				// skip this tool if it doesn't have a prefab
				if (tool.toolPrefab == null) continue;


				// TODO add timeout. What if we never find them?
				if (tracked && tool.trackedHands)
				{
					while (leftHandSkele.Bones == null || rightHandSkele.Bones == null ||
						leftHandSkele.Bones.Count == 0 || rightHandSkele.Bones.Count == 0)
					{
						yield return null;
					}

					// override if we found hands
					InputMan.controllerStyle = HeadsetControllerStyle.QuestHands;
				}
				if (!tracked && tool.avatarHands)
				{
					do
					{
						yield return null;
						// TODO Very slow, improve
						avatar = FindObjectOfType<OvrAvatar>();
					}
					while (avatar == null);
					while (avatar.HandLeft == null || avatar.HandRight == null)
					{
						yield return null;
					}
				}


				// twice for left and right hands
				for (int i = 0; i < 2; i++)
				{
					bool isRight = (i == 0);
					if ((isRight && tool.rightHand) || (!isRight && tool.leftHand))
					{
						if (tracked && tool.trackedHands)
						{
							Transform parent = null;
							OVRSkeleton h = isRight ? rightHandSkele : leftHandSkele;

							if (h == null)
							{
								Debug.LogError("Couldn't find hands at all.");
								continue;
							}

							switch (tool.finger)
							{
								case OVRPlugin.HandFinger.Thumb:
									parent = h.Bones[(int)OVRPlugin.BoneId.Hand_ThumbTip].Transform;
									break;
								case OVRPlugin.HandFinger.Index:
									parent = h.Bones[(int)OVRPlugin.BoneId.Hand_IndexTip].Transform;
									break;
								case OVRPlugin.HandFinger.Middle:
									parent = h.Bones[(int)OVRPlugin.BoneId.Hand_MiddleTip].Transform;
									break;
								case OVRPlugin.HandFinger.Ring:
									parent = h.Bones[(int)OVRPlugin.BoneId.Hand_RingTip].Transform;
									break;
								case OVRPlugin.HandFinger.Pinky:
									parent = h.Bones[(int)OVRPlugin.BoneId.Hand_PinkyTip].Transform;
									break;
								case OVRPlugin.HandFinger.Max:
									break;
							}

							if (parent != null)
							{
								Instantiate(tool.toolPrefab, parent);
							}
							else
							{
								Debug.LogError("Couldn't find hand finger.");
							}
						}
						
						if (!tracked && tool.avatarHands)
						{
							if (isRight)
							{
								var obj = Instantiate(tool.toolPrefab, avatar.HandRight.RenderParts[0].bones[7]);
								if (obj.GetComponent<TouchMenuFingerCollider>())
								{
									obj.GetComponent<TouchMenuFingerCollider>().isLeft = !isRight;
								}
							} else
							{
								var obj = Instantiate(tool.toolPrefab, avatar.HandLeft.RenderParts[0].bones[7]);
								if (obj.GetComponent<TouchMenuFingerCollider>())
								{
									obj.GetComponent<TouchMenuFingerCollider>().isLeft = !isRight;
								}
							}
						}
					}
				}
			}
		}
	}
}