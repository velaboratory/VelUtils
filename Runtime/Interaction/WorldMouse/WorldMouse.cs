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
		/// The distance of closest UI object that was hit.
		/// This is set from the WM InputModule
		/// </summary>
		[ReadOnly]
		public float worldRayDistance;

		public RaycastHit? lastHit { get; private set; }
		public Vector2 textureHitPoint { get; private set; }
		public Vector3 worldHitPoint { get; private set; }
		public GameObject hitGameObject { get; private set; }
		public Collider hitCollider { get; private set; }

		protected virtual void Start()
		{
			WorldMouseInputModule.Instance.AddWorldMouse(this);
		}

		protected virtual void Update()
		{
			worldRayDistance = Mathf.Infinity;
			if (Physics.Raycast(new Ray(transform.position, transform.forward), out RaycastHit hit))
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