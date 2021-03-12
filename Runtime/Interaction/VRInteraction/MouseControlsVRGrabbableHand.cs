using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class MouseControlsVRGrabbableHand : MonoBehaviour
	{
		public Camera mainCamera;
		public VRGrabbableHand targetHand;
		public LayerMask layerMask = ~0;
		public float scrollSpeedMultiplier = 1;
		public float extraDeepness = .05f;

		private float distance;


		// Update is called once per frame
		private void Update()
		{
			Vector2 mousePos;
			if (Input.touchSupported && Input.touchCount > 0)
			{
				mousePos = Input.GetTouch(0).position;
			}
			else
			{
				mousePos = Input.mousePosition.ToVector2();
			}

			Ray mouseRay = mainCamera.ScreenPointToRay(mousePos);

			// move the hand around with the mouse
			if (Input.GetMouseButton(0) ||
			    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved))
			{
				targetHand.transform.position =
					mainCamera.transform.position + mouseRay.direction.normalized * distance;
			}
			else //if (!Input.GetMouseButton(2))
			{
				if (Physics.Raycast(mouseRay, out RaycastHit hit, 100, layerMask, QueryTriggerInteraction.Ignore))
				{
					targetHand.transform.position = hit.point + mouseRay.direction.normalized * extraDeepness;
					distance = Vector3.Distance(mainCamera.transform.position, targetHand.transform.position);
				}
			}

			// move the hand with scrolling
			distance += Input.mouseScrollDelta.y * .025f * scrollSpeedMultiplier * distance;

			// Grab and release
			if (Input.GetMouseButtonDown(0) ||
			    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
			{
				targetHand.Grab();
			}

			if (Input.GetMouseButtonUp(0) ||
			    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
			{
				targetHand.Release();
			}
		}
	}
}