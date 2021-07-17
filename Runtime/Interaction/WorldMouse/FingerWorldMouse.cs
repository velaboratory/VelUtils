using UnityEngine;
using UnityEngine.UI;

namespace unityutilities
{
	public class FingerWorldMouse : WorldMouse
	{
		/// <summary>
		/// Used for vibration
		/// </summary>
		public Side side;

		[Header("On Click")]
		public float vibrateOnClick = .5f;
		public AudioSource soundOnClick;

		[Header("On Hover")]
		public float vibrateOnHover = .1f;
		public AudioSource soundOnHover;

		[Space]

		public float activationDistance = .02f;
		private bool wasActivated;

		protected void Start()
		{
			ClickDown += OnClicked;
			HoverEntered += OnHover;
		}

		private void OnHover(GameObject obj)
		{
			if (obj.GetComponent<Selectable>() != null)
			{
				InputMan.Vibrate(side, vibrateOnHover);
				if (soundOnHover != null) soundOnHover.Play();
			}
		}

		private void OnClicked(GameObject obj)
		{
			if (obj.GetComponent<Selectable>() != null)
			{
				InputMan.Vibrate(side, vibrateOnClick);
				if (soundOnClick != null) soundOnClick.Play();
			}
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