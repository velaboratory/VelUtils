using UnityEngine;

namespace unityutilities
{
	public class FingerWorldMouse : WorldMouse
	{
		/// <summary>
		/// Used for vibration
		/// </summary>
		public Side side;

		[Space] public float vibrateOnClick = .5f;
		public float vibrateOnHover = .1f;

		public float activationDistance = .02f;
		private bool wasActivated;

		protected void Start()
		{
			ClickDown += OnClicked;
			HoverEntered += OnHover;
		}

		private void OnHover(GameObject obj)
		{
			InputMan.Vibrate(side, vibrateOnHover);
		}

		private void OnClicked(GameObject obj)
		{
			InputMan.Vibrate(side, vibrateOnClick);
		}

		public override bool PressDown()
		{
			if (rayDistance < activationDistance)
			{
				if (!wasActivated)
				{
					wasActivated = true;
					return true;
				}
			}

			return false;
		}

		public override bool PressUp()
		{
			if (rayDistance > activationDistance || float.IsInfinity(rayDistance))
			{
				if (wasActivated)
				{
					wasActivated = false;
					return true;
				}
			}

			return false;
		}
	}
}