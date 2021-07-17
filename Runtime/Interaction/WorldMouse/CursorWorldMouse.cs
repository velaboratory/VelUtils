using UnityEngine;
using UnityEngine.UI;

namespace unityutilities
{
	public class CursorWorldMouse : WorldMouse
	{
		public Camera screenCamera;
		public AudioClip soundOnClick;
		public AudioClip soundOnHover;

		protected void Start()
		{
			ClickDown += OnClicked;
			HoverEntered += OnHover;
		}

		private void OnHover(GameObject obj)
		{
			if (obj.GetComponent<Selectable>() != null)
			{
				if (soundOnHover != null)
				{
					AudioSource.PlayClipAtPoint(soundOnHover, obj.transform.position, .5f);
				}
			}
		}

		private void OnClicked(GameObject obj)
		{
			if (obj.GetComponent<Selectable>() != null)
			{
				if (soundOnClick != null)
				{
					AudioSource.PlayClipAtPoint(soundOnClick, obj.transform.position, .5f);
				}
			}
		}

		public override bool PressDown()
		{
			return Input.GetMouseButtonDown(0);
		}

		public override bool PressUp()
		{
			return Input.GetMouseButtonUp(0);
		}

		protected override void Update()
		{
			if (screenCamera != null)
			{
				Ray r = screenCamera.ScreenPointToRay(Input.mousePosition);

				transform.position = screenCamera.transform.position;
				transform.forward = r.direction;
			}
			base.Update();
		}
	}
}