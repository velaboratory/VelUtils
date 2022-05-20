using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	[AddComponentMenu("unityutilities/Interaction/VRMoveable")]
	[DisallowMultipleComponent]
	public class VRMoveable : VRGrabbable
	{
		private Rigidbody rb;

		/// <summary>
		/// 🏃‍ bool: localInput (was this action cause by grabbing?)
		/// </summary>
		public Action<bool> Moved;

		// public Transform centerOfMass;

		[Space] public bool useVelocityFollow = true;
		private bool wasKinematic;
		private bool wasUsingGravity;
		private CopyTransform copyTransform;
		public bool setKinematic = true;

		public bool releaseVelocitySmoothing = true;

		public bool useFixedPositionOffset;
		public Vector3 fixedPositionOffset;
		public bool useFixedRotationOffset;
		public Quaternion fixedRotationOffset = Quaternion.identity;

		//public bool allowMultiHandGrabbing = false;	// TODO

		// Use this for initialization
		private new void Awake()
		{
			base.Awake();
			rb = GetComponent<Rigidbody>();
			if (rb != null)
			{
				wasKinematic = rb.isKinematic;
				wasUsingGravity = rb.useGravity;
			}

			copyTransform = GetComponent<CopyTransform>();
		}

		private void Update()
		{
			if (GrabbedBy != null)
			{
				// TODO only move when changed position
				Moved?.Invoke(true);
			}


			// TODO work out better system
			if (setKinematic && rb != null)
			{
				if (!locallyOwned && networkGrabbed)
				{
					rb.isKinematic = true;
				}
				else
				{
					rb.isKinematic = wasKinematic;
				}
			}


			// this is to prevent objects from going near infinity
			if (Mathf.Abs(transform.position.x) > 10000 ||
			    Mathf.Abs(transform.position.y) > 10000 ||
			    Mathf.Abs(transform.position.z) > 10000)
			{
				Debug.Log("Had to reset the position of an object.", this);
				transform.position = Vector3.zero;
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);

			if (rb != null)
			{
				rb.useGravity = false;
			}

			// add a CopyTransform if it doesn't exist
			if (!copyTransform)
			{
				copyTransform = gameObject.AddComponent<CopyTransform>();

				if (useVelocityFollow)
				{
					copyTransform.positionFollowType = CopyTransform.FollowType.Velocity;
					copyTransform.rotationFollowType = CopyTransform.FollowType.Velocity;
					copyTransform.useFixedUpdatePos = true;
					copyTransform.useFixedUpdateRot = true;
				}
				else
				{
					copyTransform.positionFollowType = CopyTransform.FollowType.Copy;
					copyTransform.rotationFollowType = CopyTransform.FollowType.Copy;
				}

				copyTransform.snapIfAngleGreaterThan = 90;
				copyTransform.snapIfDistanceGreaterThan = 1f;
				copyTransform.followPosition = true;
				copyTransform.followRotation = true;
			}

			copyTransform.SetTarget(h.transform);

			if (useFixedPositionOffset)
			{
				copyTransform.positionOffset = fixedPositionOffset;
			}

			if (useFixedRotationOffset)
			{
				copyTransform.rotationOffset = fixedRotationOffset;
			}

			// this obj has been grabbed by raycast, snap
			if (h.raycastedObjs.Contains(this))
			{
				copyTransform.positionOffset = fixedPositionOffset;
			}
		}

		public override void HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

			if (rb != null)
			{
				rb.useGravity = wasUsingGravity;
			}

			if (copyTransform)
			{
				copyTransform.SetTarget(null);
			}
			else
			{
				Debug.LogError("No copy transform on release.", this);
			}

			if (releaseVelocitySmoothing)
			{
				if (h != null)
				{
					// get the average
					//rb.velocity = h.lastVels.Aggregate(new Vector3(0, 0, 0), (s, v) => s + v) / h.lastVels.Count;

					// get the median
					//rb.velocity = h.lastVels.OrderBy(e => e.sqrMagnitude).ToList()[h.lastVels.Count / 2];

					// get the last vel
					if (h.lastVels.Count > 0 && rb != null)
					{
						rb.velocity = h.lastVels.Last();
					}
					else
					{
						Debug.LogError("No velocities. Can't throw!");
					}
				}
				else
				{
					Debug.LogError("Can't use release velocity smoothing if you don't supply a hand.");
				}
			}
		}

		public override byte[] PackData()
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(outputStream);

				writer.Write(transform.localPosition);
				writer.Write(transform.localRotation);

				return outputStream.ToArray();
			}
		}

		public override void UnpackData(byte[] data)
		{
			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				transform.localPosition = reader.ReadVector3();
				transform.localRotation = reader.ReadQuaternion();

				Moved?.Invoke(false);
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Sets up the interface for the VRMoveable script.
	/// </summary>
	[CustomEditor(typeof(VRMoveable))]
	public class VRMoveableEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			VRMoveable vrm = target as VRMoveable;

			if (vrm != null && vrm.useVelocityFollow && !vrm.gameObject.GetComponent<Rigidbody>())
			{
				EditorGUILayout.HelpBox("Set to follow with velocity, but no rigidbody attached.", MessageType.Error);
				if (GUILayout.Button("Add Rigidbody"))
				{
					vrm.gameObject.AddComponent<Rigidbody>();
				}
			}
		}
	}
#endif
}