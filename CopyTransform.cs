using System;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// One object copies the position and/or rotation of another using a variety of techniques. Global or local offsets can be set.
	/// </summary>
	public class CopyTransform : MonoBehaviour
	{
		public enum FollowType
		{
			Copy,
			Velocity,
			Force
		}

		public Transform target;

		[Header("Position")] public bool followPosition;
		public FollowType positionFollowType;
		public float positionForceMult = 1000f;
		public bool useFixedUpdatePos = true;

		public Vector3 positionOffset;
		public Space positionOffsetCoordinateSystem;
		
		public float snapIfDistanceGreaterThan = 0;


		[Header("Rotation")] public bool followRotation;
		public FollowType rotationFollowType;
		public float rotationForceMult = 1;
		public bool useFixedUpdateRot = true;

		public Quaternion rotationOffset;
		public Space rotationOffsetCoordinateSystem;

		public float snapIfAngleGreaterThan = 0;

		private Rigidbody rb;

		private void Start()
		{
			if (GetComponent<Rigidbody>())
			{
				rb = GetComponent<Rigidbody>();
				rb.maxAngularVelocity = 1000f;
			}
		}


		private void Update()
		{
			if (!target)
			{
				//Debug.Log("No target set!");
				return;
			}

			if (followRotation && !useFixedUpdateRot)
			{
				UpdateRotation(Time.smoothDeltaTime);
			}

			if (followPosition && !useFixedUpdatePos)
			{
				UpdatePosition(Time.smoothDeltaTime);
			}
		}

		void FixedUpdate()
		{
			if (!target)
			{
				//Debug.Log("No target set!");
				return;
			}

			if (followPosition && useFixedUpdatePos)
			{
				UpdatePosition(Time.fixedDeltaTime);
			}

			if (followRotation && useFixedUpdateRot)
			{
				UpdateRotation(Time.fixedDeltaTime);
			}
		}

		private void UpdatePosition(float timeStep)
		{
			Vector3 t;
			if (positionOffsetCoordinateSystem == Space.World)
			{
				t = target.position + positionOffset;
			} else
			{
				t = target.TransformPoint(positionOffset);
			}
			
			switch (positionFollowType)
			{
				case FollowType.Copy:
					transform.position = t;
					if (rb)
					{
						rb.velocity = Vector3.zero;
					}
					break;
				case FollowType.Velocity:
					if (Mathf.Abs(snapIfDistanceGreaterThan) > .001f && Vector3.Distance(t, transform.position) > snapIfDistanceGreaterThan)
					{
						transform.position = t;
					}
					rb.velocity = Vector3.ClampMagnitude((t - transform.position) / timeStep, 100);
					break;
				case FollowType.Force:
					if (Mathf.Abs(snapIfDistanceGreaterThan) > .001f && Vector3.Distance(t, transform.position) > snapIfDistanceGreaterThan)
					{
						transform.position = t;
					}
					Vector3 dir = t - transform.position;
					rb.AddForce(
						Vector3.ClampMagnitude(dir * Vector3.Magnitude(dir) * positionForceMult * rb.mass / timeStep,
							5000 * rb.mass));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateRotation(float timeStep)
		{
			Quaternion t;
			if (rotationOffsetCoordinateSystem == Space.Self) {
				t = target.rotation * Quaternion.Inverse(rotationOffset);
			} else
			{
				t = target.rotation;
			}
			switch (rotationFollowType)
			{
				case FollowType.Copy:
					transform.rotation = t;
					break;
				case FollowType.Velocity:
					float angle1;
					var angularVel1 = AngularVel(timeStep, t, out angle1);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle1 > snapIfAngleGreaterThan)
					{
						transform.rotation = t;
					}
					rb.angularVelocity = Vector3.ClampMagnitude(angularVel1, 100);
					break;
				case FollowType.Force:
					float angle2;
					var angularVel2 = AngularVel(timeStep, t, out angle2);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle2 > snapIfAngleGreaterThan)
					{
						transform.rotation = t;
					}
					var angularTorq = angularVel2 * rotationForceMult;
					rb.AddTorque(Vector3.ClampMagnitude(angularTorq, 100));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Vector3 AngularVel(float timeStep, Quaternion t, out float angle)
		{
			Quaternion rot = t * Quaternion.Inverse(transform.rotation);
			Vector3 axis;
			rot.ToAngleAxis(out angle, out axis);
			Vector3 angularVel = axis * angle * Mathf.Deg2Rad / timeStep;
			return angularVel;
		}
	}
}