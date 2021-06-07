using System;
using UnityEngine;

namespace unityutilities
{
	public abstract class WorldMouse : MonoBehaviour
	{
		/// <summary>
		/// The distance of closest UI object that was hit.
		/// This is set from the WM InputModule
		/// </summary>
		[ReadOnly]
		public float rayDistance;

		/// <summary>
		/// The distance of closest object that was hit.
		/// </summary>
		[ReadOnly]
		public float worldRayDistance;

		public float minDistance = 0;
		public float maxDistance = Mathf.Infinity;

		public RaycastHit? lastHit { get; private set; }
		public Vector2 textureHitPoint { get; private set; }
		public Vector3 worldHitPoint { get; private set; }
		public GameObject hitGameObject { get; private set; }
		public Collider hitCollider { get; private set; }
		public GameObject lastHoverObject { get; set; }

		// event callbacks
		public Action<GameObject> ClickUp;
		public Action<GameObject> ClickDown;
		public Action<GameObject> HoverEntered;

		protected virtual void OnEnable()
		{
			WorldMouseInputModule.Instance.AddWorldMouse(this);
		}

		protected virtual void OnDisable()
		{
			WorldMouseInputModule.Instance.RemoveWorldMouse(this);
		}

		protected virtual void Update()
		{
			// this auto-creates a worldmousemanager in the scene if there isn't one. 
			// Disable it to disable worldmouse instead of deleting
			if (WorldMouseInputModule.Instance == null) return;

			worldRayDistance = Mathf.Infinity;
			if (Physics.Raycast(new Ray(transform.position, transform.forward), out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(transform.position, transform.forward);
				worldRayDistance = hit.distance;
				lastHit = hit;
				worldHitPoint = hit.point;
				textureHitPoint = hit.textureCoord;
				hitCollider = hit.collider;
				if (worldRayDistance < rayDistance)
				{
					if (hit.collider.attachedRigidbody != null)
					{
						hitGameObject = hit.collider.attachedRigidbody.gameObject;
					}
					else
					{
						hitGameObject = hit.transform.gameObject;
					}
				}
			}
			else
			{
				lastHit = null;
				hitGameObject = null;
			}
		}

		public abstract bool PressDown();

		public abstract bool PressUp();
	}
}