using UnityEngine;
using UnityEngine.UI;

namespace VelUtils.Interaction.WorldMouse
{
	public class CursorWorldMouse : WorldMouse
	{
		public Camera screenCamera;
		public AudioClip soundOnClick;
		public AudioClip soundOnHover;

		protected void Start()
		{
			OnClickDown += OnClicked;
			OnHoverStart += OnHover;
		}

		protected override void Update()
		{
			if (screenCamera != null)
			{
				Ray r = screenCamera.ScreenPointToRay(Input.mousePosition);

				transform.position = screenCamera.transform.position;
				transform.forward = r.direction;
			}

			if (Input.GetMouseButtonDown(0)) Press();
			if (Input.GetMouseButtonUp(0)) Release();
			base.Update();
		}

		private void OnHover(GameObject obj)
		{
			if (obj != null && obj.GetComponent<Selectable>() != null)
			{
				if (soundOnHover != null)
				{
					AudioSource.PlayClipAtPoint(soundOnHover, obj.transform.position, .5f);
				}
			}
		}

		private void OnClicked(GameObject obj)
		{
			if (obj != null && obj.GetComponent<Selectable>() != null)
			{
				if (soundOnClick != null)
				{
					AudioSource.PlayClipAtPoint(soundOnClick, obj.transform.position, .5f);
				}
			}
		}
	}
}