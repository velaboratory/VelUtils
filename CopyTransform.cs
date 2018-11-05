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


		[Header("Rotation")] public bool followRotation;
		public FollowType rotationFollowType;
		public float rotationForceMult = 1;
		public bool useFixedUpdateRot = true;

		public Vector3 rotationOffset;
		public Space rotationOffsetCoordinateSystem;

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

			if (followPosition && !useFixedUpdatePos)
			{
				UpdatePosition(Time.smoothDeltaTime);
			}

			if (followRotation && !useFixedUpdateRot)
			{
				UpdateRotation(Time.smoothDeltaTime);
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
			Vector3 t = target.position + positionOffset;
			switch (positionFollowType)
			{
				case FollowType.Copy:
					transform.position = t;
					break;
				case FollowType.Velocity:
					rb.velocity = Vector3.ClampMagnitude((t - transform.position) / timeStep, 100);
					break;
				case FollowType.Force:
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
			Quaternion t = Quaternion.Euler(rotationOffset) * target.rotation;
			switch (rotationFollowType)
			{
				case FollowType.Copy:
					transform.rotation = t;
					break;
				case FollowType.Velocity:
					var angularVel = AngularVel(timeStep, t);
					rb.angularVelocity = Vector3.ClampMagnitude(angularVel, 100);
					break;
				case FollowType.Force:
					var angularTorq = AngularVel(timeStep, t) * rotationForceMult;
					rb.AddTorque(Vector3.ClampMagnitude(angularTorq, 100));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Vector3 AngularVel(float timeStep, Quaternion t)
		{
			Quaternion rot = t * Quaternion.Inverse(transform.rotation);
			float angle;
			Vector3 axis;
			rot.ToAngleAxis(out angle, out axis);
			Vector3 angularVel = axis * angle * Mathf.Deg2Rad / timeStep;
			return angularVel;
		}
	}
}