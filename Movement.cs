using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Adds several movement techniques while maintaining compatibility with many rig setups.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class Movement : MonoBehaviour
	{
		public InputMan inputMan;
		public Rigidbody rigRB;
		public Transform leftHand;
		public Transform rightHand;

		private GameObject leftHandGrabPos;
		private GameObject rightHandGrabPos;

		private Vector3[] lastVels = new Vector3[5];
		private int lastVelsIndex = 0;

		private CopyTransform cpt;


		void Start()
		{
			cpt = GetComponent<CopyTransform>();
			if (cpt == null)
			{
				cpt = gameObject.AddComponent<CopyTransform>();
			}

			cpt.followPosition = true;
			cpt.positionFollowType = CopyTransform.FollowType.Velocity;
		}

		void Update()
		{

			// left hand
			if (inputMan.GripDown(Side.Left))
			{
				if (leftHandGrabPos != null)
				{
					Destroy(leftHandGrabPos.gameObject);
				}

				leftHandGrabPos = new GameObject("Left Hand Grab Pos");
				leftHandGrabPos.transform.position = leftHand.position;
				cpt.target = leftHandGrabPos.transform;
				cpt.positionOffset = rigRB.position - leftHand.position;
			}
			else if (inputMan.Grip(Side.Left))
			{
				cpt.positionOffset = rigRB.position - leftHand.position;
			}else
			{
				if (leftHandGrabPos != null)
				{
					Destroy(leftHandGrabPos.gameObject);

					rigRB.velocity = MedianAvg(lastVels);
				}
			}

			// right hand
			if (inputMan.GripDown(Side.Right))
			{
				if (rightHandGrabPos != null)
				{
					Destroy(rightHandGrabPos.gameObject);
				}

				rightHandGrabPos = new GameObject("Right Hand Grab Pos");
				rightHandGrabPos.transform.position = rightHand.position;
				cpt.target = rightHandGrabPos.transform;
				cpt.positionOffset = rigRB.position - rightHand.position;
			}
			else if (inputMan.Grip(Side.Right))
			{
				cpt.positionOffset = rigRB.position - rightHand.position;
			}
			else
			{
				if (rightHandGrabPos != null)
				{
					Destroy(rightHandGrabPos.gameObject);

					rigRB.velocity = MedianAvg(lastVels);
				}
			}

			
			// update lastvels
			lastVels[lastVelsIndex] = rigRB.velocity;
			lastVelsIndex = ++lastVelsIndex % 5;
		}

		Vector3 MedianAvg(Vector3[] inputArray)
		{
			List<Vector3> list = new List<Vector3>(inputArray);
			list = list.OrderBy(x => x.magnitude).ToList();
			list.RemoveAt(0);
			list.RemoveAt(list.Count-1);
			Vector3 result = new Vector3(
				list.Average(x => x.x),
				list.Average(x => x.y),
				list.Average(x => x.z));
			return result;
		}
	}
}