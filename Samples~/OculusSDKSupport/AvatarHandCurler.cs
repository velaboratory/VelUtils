using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VelUtils.VRInteraction;

public class AvatarHandCurler : MonoBehaviour
{
	public OvrAvatar avatar;
	private bool init;

	public VRGrabbableHand leftHand;
	public VRGrabbableHand rightHand;

	// Start is called before the first frame update
	IEnumerator Start()
	{
		while (avatar.HandLeft == null || avatar.HandRight == null)
		{
			yield return null;
		}
		init = true;
	}

	void LateUpdate()
	{
		if (init)
		{
			if (leftHand.grabbedVRGrabbable != null)
			{
				Transform handOrigin = GameObject.Find("hands:b_l_hand").transform;

				Vector3 rayHandPos = handOrigin.position + (handOrigin.right * .065f);
				Transform movingPart = avatar.HandLeft.transform.GetChild(0);

				if (Physics.Raycast(rayHandPos + handOrigin.forward * -.1f, handOrigin.forward, out var hit, .3f))
				{
					Quaternion.FromToRotation(handOrigin.forward, -hit.normal).ToAngleAxis(out var angle, out var axis);
					movingPart.RotateAround(rayHandPos, axis, angle);
					movingPart.Translate(handOrigin.forward * (hit.distance - .12f), Space.World);
				}


				// Fingers
				Transform[][] fingers = { 
					new Transform[] {
						GameObject.Find("hands:b_l_index1").transform,
						GameObject.Find("hands:b_l_index2").transform,
						GameObject.Find("hands:b_l_index3").transform
					},
					new Transform[] {
						GameObject.Find("hands:b_l_middle1").transform,
						GameObject.Find("hands:b_l_middle2").transform,
						GameObject.Find("hands:b_l_middle3").transform
					},
					new Transform[] {
						GameObject.Find("hands:b_l_pinky1").transform,
						GameObject.Find("hands:b_l_pinky2").transform,
						GameObject.Find("hands:b_l_pinky3").transform
					},
					new Transform[] {
						GameObject.Find("hands:b_l_ring1").transform,
						GameObject.Find("hands:b_l_ring2").transform,
						GameObject.Find("hands:b_l_ring3").transform
					},
					new Transform[] {
						GameObject.Find("hands:b_l_thumb2").transform,
						GameObject.Find("hands:b_l_thumb3").transform
					}
				};

				foreach (var finger in fingers)
				{
					foreach (var joint in finger)
					{
						if (Physics.Raycast(joint.position + joint.right * -.03f + joint.up * -.1f, joint.up, out var hit2, .2f))
						{
							if (hit2.distance > .12f)
							{
								joint.Rotate(joint.forward, -5, Space.World);
							} else if (hit2.distance < .1)
							{
								joint.Rotate(joint.forward, 5, Space.World);
							}
						}
						else
						{
							joint.Rotate(joint.forward, -5, Space.World);
						}
					}
				}
			}
		}
	}

}
