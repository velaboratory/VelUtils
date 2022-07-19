using UnityEngine;
using UnityEngine.UI;

namespace unityutilities.Interaction.WorldMouse
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
			OnClickDown += OnClicked;
			OnHoverStart += OnHover;
		}

		protected override void Update()
		{
			if (currentRayLength < activationDistance)
			{
				if (!wasActivated)
				{
					wasActivated = true;
					Press();
				}
			}
			
			
			if (currentRayLength > activationDistance || float.IsInfinity(currentRayLength))
			{
				if (wasActivated)
				{
					wasActivated = false;
					Release();
				}
			}
			
			base.Update();
		}

		private void OnHover(GameObject obj)
		{
			if (obj != null && obj.GetComponent<Selectable>() != null)
			{
				InputMan.Vibrate(side, vibrateOnHover);
				if (soundOnHover != null) soundOnHover.Play();
			}
		}

		private void OnClicked(GameObject obj)
		{
			if (obj != null && obj.GetComponent<Selectable>() != null)
			{
				InputMan.Vibrate(side, vibrateOnClick);
				if (soundOnClick != null) soundOnClick.Play();
			}
		}
	}
}