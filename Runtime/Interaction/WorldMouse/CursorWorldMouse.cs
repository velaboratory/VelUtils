using UnityEngine;

namespace unityutilities
{
	public class CursorWorldMouse : WorldMouse
	{
		public Camera myCamera;

		public override bool PressDown()
		{
			return Input.GetMouseButtonDown(0);
		}

		public override bool PressUp()
		{
			return Input.GetMouseButtonUp(0);
		}

		// Update is called once per frame
		protected override void Update()
		{
			Ray r = myCamera.ScreenPointToRay(Input.mousePosition);

			transform.position = myCamera.transform.position;
			transform.forward = r.direction;
			base.Update();
			// Debug.Log(Input.mousePosition);
		}
	}
}