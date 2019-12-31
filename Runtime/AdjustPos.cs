using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Nudges an object with assignable keyboard shortcuts.
	/// </summary>
	[AddComponentMenu("unityutilities/Adjust Pos")]
	public class AdjustPos : MonoBehaviour
	{

		public float amount = .01f;
		public float rotationAmount = 10f;

		public string modifierKey = "left shift";

		[Header("Positions")]
		public string up = "e";
		public string down = "q";
		public string left = "a";
		public string right = "d";
		public string forward = "w";
		public string back = "s";

		[Header("Rotations (global)")]
		public string rotateLeft = "";
		public string rotateRight = "";

		// Update is called once per frame
		private void Update()
		{
			float actualAmount;
			float actualRotationAmount;
			if (modifierKey != "" && Input.GetKey(modifierKey))
			{
				actualAmount = amount / 2;
				actualRotationAmount = rotationAmount / 2;
			}
			else
			{
				actualAmount = amount;
				actualRotationAmount = rotationAmount;
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

			if (rotateLeft != "" && Input.GetKeyDown(rotateLeft))
			{
				transform.Rotate(0, -actualRotationAmount, 0, Space.World);
			}

			if (rotateRight != "" && Input.GetKeyDown(rotateRight))
			{
				transform.Rotate(0, actualRotationAmount, 0, Space.World);
			}
		}
	}
}
