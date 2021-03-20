using UnityEngine;

namespace unityutilities
{
	public class CursorWorldMouse : WorldMouse
	{
		public Camera screenCamera;

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