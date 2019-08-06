using System;
using UnityEngine;

namespace unityutilities {
	/// <summary>
	/// One object copies the position and/or rotation of another using a variety of techniques. Global or local offsets can be set.
	/// </summary>
	public class CopyTransform : MonoBehaviour {
		public enum FollowType {
			Copy,
			Velocity,
			Force
		}

		public Transform target;

		//----------------------//
		[Header("Position")] public bool followPosition;
		public FollowType positionFollowType;
		public float positionForceMult = 1000f;
		public bool useFixedUpdatePos;
		public Vector3 positionOffset;
		public Space positionOffsetCoordinateSystem = Space.Self;
		public float snapIfDistanceGreaterThan;


		//----------------------//
		[Header("Rotation")] public bool followRotation;
		public FollowType rotationFollowType;
		public float rotationForceMult = 1;
		public bool useFixedUpdateRot;
		public Quaternion rotationOffset = Quaternion.identity;
		public Vector3 rotationOffsetVector3;
		public bool useVector3RotationOffset;
		public Space rotationOffsetCoordinateSystem = Space.Self;
		public float snapIfAngleGreaterThan;

		public float smoothness;

		private Rigidbody rb;

		private void Start() {
			if (GetComponent<Rigidbody>()) {
				rb = GetComponent<Rigidbody>();
				rb.maxAngularVelocity = 1000f;
			}
		}


		private void Update() {
			if (!target) {
				//Debug.Log("No target set!");
				return;
			}

			if (followRotation && !useFixedUpdateRot) {
				UpdateRotation(Time.smoothDeltaTime);
			}

			if (followPosition && !useFixedUpdatePos) {
				UpdatePosition(Time.smoothDeltaTime);
			}
		}

		private void FixedUpdate() {
			if (!target) {
				//Debug.Log("No target set!");
				return;
			}

			if (followPosition && useFixedUpdatePos) {
				UpdatePosition(Time.fixedDeltaTime);
			}

			if (followRotation && useFixedUpdateRot) {
				UpdateRotation(Time.fixedDeltaTime);
			}
		}

		private void UpdatePosition(float timeStep) {
			Vector3 t;
			if (positionOffsetCoordinateSystem == Space.World) {
				t = target.position + positionOffset;
			}
			else {
				t = target.TransformPoint(positionOffset);
			}

			switch (positionFollowType) {
				case FollowType.Copy:
					transform.position = Vector3.Lerp(transform.position, t, 1 - smoothness);
					if (rb) {
						rb.velocity = Vector3.zero;
					}

					break;
				case FollowType.Velocity:
					if (Mathf.Abs(snapIfDistanceGreaterThan) > .001f &&
					    Vector3.Distance(t, transform.position) > snapIfDistanceGreaterThan) {
						transform.position = t;
					}

					rb.velocity = Vector3.ClampMagnitude(((t - transform.position) / timeStep) * (1 - smoothness), 100);
					break;
				case FollowType.Force:
					if (Mathf.Abs(snapIfDistanceGreaterThan) > .001f &&
					    Vector3.Distance(t, transform.position) > snapIfDistanceGreaterThan) {
						transform.position = t;
					}

					Vector3 dir = t - transform.position;
					float mass = rb.mass;
					rb.AddForce(
						Vector3.ClampMagnitude(dir * Vector3.Magnitude(dir) * positionForceMult * (mass) / timeStep,
							5000 * mass));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateRotation(float timeStep) {
			Quaternion t;
			if (rotationOffsetCoordinateSystem == Space.Self) {
				if (useVector3RotationOffset) {
					t = target.rotation * Quaternion.Euler(rotationOffsetVector3);
				}
				else {
					t = target.rotation * rotationOffset;
				}
			}
			else {
				t = target.rotation;
			}

			switch (rotationFollowType) {
				case FollowType.Copy:
					transform.rotation = Quaternion.Slerp(transform.rotation, t, 1 - smoothness);
					break;
				case FollowType.Velocity:
					float angle1;
					var angularVel1 = AngularVel(timeStep, t, out angle1);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle1 > snapIfAngleGreaterThan) {
						transform.rotation = t;
					}

					rb.angularVelocity = Vector3.ClampMagnitude(angularVel1 * (1 - smoothness), 100);
					break;
				case FollowType.Force:
					float angle2;
					var angularVel2 = AngularVel(timeStep, t, out angle2);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle2 > snapIfAngleGreaterThan) {
						transform.rotation = t;
					}

					var angularTorq = angularVel2 * rotationForceMult;
					rb.AddTorque(Vector3.ClampMagnitude(angularTorq, 100));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Vector3 AngularVel(float timeStep, Quaternion t, out float angle) {
			Quaternion rot = t * Quaternion.Inverse(transform.rotation);
			Vector3 axis;
			rot.ToAngleAxis(out angle, out axis);
			Vector3 angularVel = axis * angle * Mathf.Deg2Rad / timeStep;
			return angularVel;
		}

		/// <summary>
		/// Set the target transform and set offsets at the same time, so that the obj doesn't move.
		/// Also sets the obj to follow pos and rot.
		/// </summary>
		/// <param name="target">The target to follow</param>
		public void SetTarget(Transform target) {
			this.target = target;
			positionOffsetCoordinateSystem = Space.Self;
			positionOffset = target.InverseTransformPoint(transform.position);


			rotationOffsetCoordinateSystem = Space.Self;
			rotationOffset = Quaternion.Inverse(target.transform.rotation) * transform.rotation;
		}
	}
}