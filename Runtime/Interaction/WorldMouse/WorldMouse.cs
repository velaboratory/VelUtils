using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace unityutilities.Interaction.WorldMouse
{
	public class WorldMouse : MonoBehaviour
	{
		[Header("Ray settings")] public float raycastLength = 10f;
		public float currentRayLength = 0;
		public LayerMask UILayer;
		public bool autoEnable = true;

		[FormerlySerializedAs("StartSelect")] [Header("Events")] [Tooltip("Called on click down")] public UnityEvent ClickDown;
		[FormerlySerializedAs("ClickRelease")] [FormerlySerializedAs("StopSelect")] [Tooltip("Called on click up")] public UnityEvent ClickUp;
		[FormerlySerializedAs("StartPoint")] [Tooltip("Called on hover start")] public UnityEvent HoverStart;
		[FormerlySerializedAs("StopPoint")] [Tooltip("Called on hover stop")] public UnityEvent HoverStop;

		public Action<GameObject> OnClickDown;
		public Action OnClickUp;
		public Action<GameObject> OnHoverStart;
		public Action OnHoverStop;

		// Internal variables
		private bool hover = false;

		internal int pointerIndex;

		private void OnEnable()
		{
			if (autoEnable) Enable();
		}

		protected void Enable()
		{
			StartCoroutine(AddPointerDelayed());
		}

		private IEnumerator AddPointerDelayed()
		{
			yield return null;

			WorldMouseInputModule.AddWorldMouse(this);
		}

		private void OnDisable()
		{
			if (autoEnable) Disable();
		}

		protected void Disable()
		{
			WorldMouseInputModule.RemoveWorldMouse(this);
			Debug.Log("Disabled " + name);
		}

		public void SetIndex(int index)
		{
			pointerIndex = index;
		}

		internal virtual Vector3 GetPosition()
		{
			return transform.position;
		}

		internal virtual Vector3 GetForward()
		{
			return transform.forward;
		}

		internal virtual float GetMaxDistance()
		{
			return raycastLength;
		}

		public void Press()
		{
			// Handle the UI events
			GameObject obj = WorldMouseInputModule.ProcessPress(pointerIndex);

			// Fire the Unity event
			ClickDown?.Invoke();

			OnClickDown?.Invoke(obj);
		}

		public void Release()
		{
			// Handle the UI events
			WorldMouseInputModule.ProcessRelease(pointerIndex);

			// Fire the Unity event
			ClickUp?.Invoke();

			OnClickUp?.Invoke();
		}

		protected virtual void Update()
		{
			if (!gameObject.activeInHierarchy) return;
			
			PointerEventData data = WorldMouseInputModule.GetData(pointerIndex);

			currentRayLength = data.pointerCurrentRaycast.distance;
			if (currentRayLength > raycastLength) currentRayLength = 0;

			if (currentRayLength != 0 && !hover)
			{
				// Fire the Unity event
				HoverStart?.Invoke();

				hover = true;

				OnHoverStart?.Invoke(data.pointerCurrentRaycast.gameObject);
			}
			else if (currentRayLength == 0 && hover)
			{
				// Fire the Unity event
				HoverStop?.Invoke();

				hover = false;

				OnHoverStop?.Invoke();
			}
		}
	}
}