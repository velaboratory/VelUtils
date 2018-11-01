using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Nudges an object with assignable keyboard shortcuts.
	/// </summary>
	public class AdjustPos : MonoBehaviour
	{

		public float amount = .01f;

		public string up = "e";
		public string down = "q";
		public string left = "a";
		public string right = "d";
		public string forward = "w";
		public string back = "s";

		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			float actualAmount;
			if (Input.GetKey(KeyCode.LeftShift))
			{
				actualAmount = amount / 2;
			}
			else
			{
				actualAmount = amount;
			}

			if (up != "" && Input.GetKeyDown(up))
			{
				transform.Translate(0, 0, actualAmount, Space.World);
			}

			if (left != "" && Input.GetKeyDown(left))
			{
				transform.Translate(-actualAmount, 0, 0, Space.World);
			}

			if (down != "" && Input.GetKeyDown(down))
			{
				transform.Translate(0, 0, -actualAmount, Space.World);
			}

			if (right != "" && Input.GetKeyDown(right))
			{
				transform.Translate(actualAmount, 0, 0, Space.World);
			}

			if (back != "" && Input.GetKeyDown(back))
			{
				transform.Translate(0, -actualAmount, 0, Space.World);
			}

			if (forward != "" && Input.GetKeyDown(forward))
			{
				transform.Translate(0, actualAmount, 0, Space.World);
			}
		}
	}
}