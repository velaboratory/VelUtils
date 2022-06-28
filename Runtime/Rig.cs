using System;
using UnityEngine;

namespace unityutilities
{
	[AddComponentMenu("unityutilities/Rig")]
	public class Rig : MonoBehaviour
	{
		public Rigidbody rb;
		public Transform head;
		public Transform leftHand;
		public Transform rightHand;

		public Transform GetHand(Side side)
		{
			switch (side)
			{
				case Side.Left:
					return leftHand;
				case Side.Right:
					return rightHand;
				case Side.Both:
				case Side.Either:
				case Side.None:
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, null);
			}
		}
	}
}