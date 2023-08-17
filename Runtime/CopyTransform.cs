﻿using System;
using UnityEditor;
using UnityEngine;

namespace VelUtils
{
	public enum Axis3D
	{
		X,
		Y,
		Z
	}

	/// <summary>
	/// One object copies the position and/or rotation of another using a variety of techniques. Global or local offsets can be set.
	/// </summary>
	[AddComponentMenu("VelUtils/Copy Transform")]
	public class CopyTransform : MonoBehaviour
	{
		public enum FollowType
		{
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
		public bool singleAxisRotation;
		public Axis3D singleAxisRotationAxis;
		public float snapIfAngleGreaterThan;

		public float smoothness;

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
			if (!target) return;

			if (followRotation && !useFixedUpdateRot)
			{
				if (singleAxisRotation)
				{
					UpdateRotationSingleAxis(Time.smoothDeltaTime);
				}
				else
				{
					UpdateRotation(Time.smoothDeltaTime);
				}
			}

			if (followPosition && !useFixedUpdatePos)
			{
				UpdatePosition(Time.smoothDeltaTime);
			}
		}

		private void FixedUpdate()
		{
			if (!target) return;

			if (followPosition && useFixedUpdatePos)
			{
				UpdatePosition(Time.fixedDeltaTime);
			}

			if (followRotation && useFixedUpdateRot)
			{
				if (singleAxisRotation)
				{
					UpdateRotationSingleAxis(Time.fixedDeltaTime);
				}
				else
				{
					UpdateRotation(Time.fixedDeltaTime);
				}
			}
		}

		private void UpdatePosition(float timeStep)
		{
			if (Mathf.Abs(timeStep) <= float.Epsilon) return;

			Vector3 t;
			if (positionOffsetCoordinateSystem == Space.World)
			{
				t = target.position + positionOffset;
			}
			else
			{
				t = target.TransformPoint(positionOffset);
			}

			switch (positionFollowType)
			{
				case FollowType.Copy:
					transform.position = Vector3.Lerp(transform.position, t, 1 - smoothness);
					if (rb)
					{
						rb.velocity = Vector3.zero;
					}

					break;
				case FollowType.Velocity:
					if (Mathf.Abs(snapIfDistanceGreaterThan) > .001f &&
					    Vector3.Distance(t, transform.position) > snapIfDistanceGreaterThan)
					{
						transform.position = t;
					}

					rb.velocity = Vector3.ClampMagnitude(((t - transform.position) / timeStep) * (1 - smoothness), 100);
					break;
				case FollowType.Force:
					if (Mathf.Abs(snapIfDistanceGreaterThan) > .001f &&
					    Vector3.Distance(t, transform.position) > snapIfDistanceGreaterThan)
					{
						transform.position = t;
					}

					Vector3 dir = t - transform.position;
					float mass = rb.mass;
					rb.AddForce(
						Vector3.ClampMagnitude(dir * (Vector3.Magnitude(dir) * positionForceMult * (mass) / timeStep),
							5000 * mass));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateRotation(float timeStep)
		{
			if (Mathf.Abs(timeStep) <= float.Epsilon) return;

			Quaternion t;
			if (rotationOffsetCoordinateSystem == Space.Self)
			{
				if (useVector3RotationOffset)
				{
					t = target.rotation * Quaternion.Euler(rotationOffsetVector3);
				}
				else
				{
					t = target.rotation * rotationOffset;
				}
			}
			else
			{
				t = target.rotation;
			}

			t = t.normalized;

			switch (rotationFollowType)
			{
				case FollowType.Copy:
					transform.rotation = Quaternion.Slerp(transform.rotation, t, 1 - smoothness);
					break;
				case FollowType.Velocity:
					Vector3 angularVel1 = AngularVel(timeStep, t, out float angle1);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle1 > snapIfAngleGreaterThan)
					{
						transform.rotation = t;
					}

					rb.angularVelocity = Vector3.ClampMagnitude(angularVel1 * (1 - smoothness), 100);
					break;
				case FollowType.Force:
					Vector3 angularVel2 = AngularVel(timeStep, t, out float angle2);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle2 > snapIfAngleGreaterThan)
					{
						transform.rotation = t;
					}

					Vector3 angularTorq = angularVel2 * rotationForceMult;
					rb.AddTorque(Vector3.ClampMagnitude(angularTorq, 100));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateRotationSingleAxis(float timeStep)
		{
			if (timeStep == 0) return;

			Quaternion t;
			if (rotationOffsetCoordinateSystem == Space.Self)
			{
				if (useVector3RotationOffset)
				{
					t = target.rotation * Quaternion.Euler(rotationOffsetVector3);
				}
				else
				{
					t = target.rotation * rotationOffset;
				}
			}
			else
			{
				t = target.rotation;
			}

			switch (rotationFollowType)
			{
				case FollowType.Copy:
					Quaternion newAngle = transform.rotation;
					Vector3 v;

					// convert to euler angles and back only in single-axis case because of gimbal lock
					switch (singleAxisRotationAxis)
					{
						case Axis3D.X:
							v = newAngle.eulerAngles;
							v.x = Mathf.LerpAngle(transform.eulerAngles.x, t.eulerAngles.x, 1 - smoothness);
							newAngle = Quaternion.Euler(v);
							break;
						case Axis3D.Y:
							v = newAngle.eulerAngles;
							v.y = Mathf.LerpAngle(transform.eulerAngles.y, t.eulerAngles.y, 1 - smoothness);
							newAngle = Quaternion.Euler(v);
							break;
						case Axis3D.Z:
							v = newAngle.eulerAngles;
							v.z = Mathf.LerpAngle(transform.eulerAngles.z, t.eulerAngles.z, 1 - smoothness);
							newAngle = Quaternion.Euler(v);
							break;
					}

					transform.rotation = newAngle;
					break;
				case FollowType.Velocity:
					Vector3 angularVel1 = AngularVel(timeStep, t, out float angle1);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle1 > snapIfAngleGreaterThan)
					{
						transform.rotation = t;
					}

					rb.angularVelocity = Vector3.ClampMagnitude(angularVel1 * (1 - smoothness), 100);
					break;
				case FollowType.Force:
					Vector3 angularVel2 = AngularVel(timeStep, t, out float angle2);
					if (Mathf.Abs(snapIfAngleGreaterThan) > .01f && angle2 > snapIfAngleGreaterThan)
					{
						transform.rotation = t;
					}

					Vector3 angularTorq = angularVel2 * rotationForceMult;
					rb.AddTorque(Vector3.ClampMagnitude(angularTorq, 100));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Vector3 AngularVel(float timeStep, Quaternion goalRotation, out float angle)
		{
			// calculate the difference between the goal and current
			Quaternion rot = goalRotation * Quaternion.Inverse(transform.rotation);
			rot.ToAngleAxis(out angle, out Vector3 axis);
			// this fixes the stupid glitch where objects would flicker when the hand was at a certain pose
			if (Math.Abs(rot.w - (-1)) < .001f) return Vector3.zero;
			// apply the angular vel portion that should happen this frame.
			Vector3 angularVel = axis * (angle * Mathf.Deg2Rad / timeStep);
			return float.IsNaN(angularVel.x) ? Vector3.zero : angularVel;
		}

		/// <summary>
		/// Set the target transform and set offsets at the same time, so that the obj doesn't move.
		/// Also sets the obj to follow pos and rot.
		/// </summary>
		/// <param name="newTarget">The target to follow</param>
		/// <param name="generateOffsets"></param>
		public void SetTarget(Transform newTarget, bool generateOffsets = true)
		{
			target = newTarget;
			if (!generateOffsets || newTarget == null) return;

			positionOffsetCoordinateSystem = Space.Self;
			positionOffset = newTarget.InverseTransformPoint(transform.position);

			rotationOffsetCoordinateSystem = Space.Self;
			rotationOffset = Quaternion.Inverse(newTarget.transform.rotation) * transform.rotation;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Sets up the interface for the CopyTransform script.
	/// </summary>
	[CustomEditor(typeof(CopyTransform))]
	public class CopyTransformEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var rbf = target as CopyTransform;

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (rbf == null) return;

			if (rbf != null && rbf.target == null)
			{
				EditorGUILayout.HelpBox(
					"No target assigned. Please assign a target to follow.", MessageType.Error);
			}

			rbf.target = (Transform)EditorGUILayout.ObjectField(
				"Target", rbf.target, typeof(Transform), true);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
			rbf.followPosition = GUILayout.Toggle(rbf.followPosition, "Follow Position");
			using (new EditorGUI.DisabledScope(!rbf.followPosition))
			{
				rbf.positionFollowType =
					(CopyTransform.FollowType)EditorGUILayout.EnumPopup("Position Follow Type",
						rbf.positionFollowType);

				if (rbf.positionFollowType != CopyTransform.FollowType.Copy)
				{
					if (!rbf.GetComponent<Rigidbody>())
					{
						EditorGUILayout.HelpBox("No rigidbody attached.", MessageType.Error);
						// add button to automatically add a rigidbody
						if (GUILayout.Button("Add Rigidbody"))
						{
							rbf.gameObject.AddComponent<Rigidbody>();
						}
					}
					else
					{
						float mass = rbf.GetComponent<Rigidbody>().mass;
						float drag = rbf.GetComponent<Rigidbody>().drag;
						if (drag / mass < 20)
						{
							EditorGUILayout.HelpBox(
								"Rigidbody drag of " + 40 * mass + " or so is recommended.",
								MessageType.Info);
						}
					}
				}

				rbf.useFixedUpdatePos = GUILayout.Toggle(rbf.useFixedUpdatePos, "Use Fixed Update");

				if (rbf.positionFollowType == CopyTransform.FollowType.Force)
				{
					rbf.positionForceMult =
						EditorGUILayout.FloatField("Position Force Multiplier", rbf.positionForceMult);
				}

				if (rbf.positionFollowType == CopyTransform.FollowType.Velocity ||
				    rbf.positionFollowType == CopyTransform.FollowType.Force)
				{
					rbf.snapIfDistanceGreaterThan =
						EditorGUILayout.FloatField(
							new GUIContent("Snap Distance",
								"If the object needs to travel farther than this distance in one frame, it will snap immediately. 0 to disable."),
							rbf.snapIfDistanceGreaterThan);
				}

				rbf.positionOffset = EditorGUILayout.Vector3Field("Position Offset", rbf.positionOffset);
				rbf.positionOffsetCoordinateSystem = (Space)EditorGUILayout.EnumPopup("Offset Coordinate System",
					rbf.positionOffsetCoordinateSystem);
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
			rbf.followRotation = GUILayout.Toggle(rbf.followRotation, "Follow Rotation");
			using (new EditorGUI.DisabledScope(!rbf.followRotation))
			{
				EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
				rbf.rotationFollowType =
					(CopyTransform.FollowType)EditorGUILayout.EnumPopup("Rotation Follow Type",
						rbf.rotationFollowType);

				if (rbf.rotationFollowType != CopyTransform.FollowType.Copy)
				{
					if (!rbf.GetComponent<Rigidbody>())
					{
						EditorGUILayout.HelpBox("No rigidbody attached.", MessageType.Error);
						// add button to automatically add a rigidbody
						if (GUILayout.Button("Add Rigidbody"))
						{
							rbf.gameObject.AddComponent<Rigidbody>();
						}
					}
					else
					{
						float mass = rbf.GetComponent<Rigidbody>().mass;
						float angDrag = rbf.GetComponent<Rigidbody>().angularDrag;
						if (angDrag / mass < 10)
						{
							EditorGUILayout.HelpBox(
								"Rigidbody angular drag of " + 20 * mass + " or so is recommended.",
								MessageType.Info);
						}
					}
				}

				rbf.useFixedUpdateRot = GUILayout.Toggle(rbf.useFixedUpdateRot, "Use Fixed Update");
				if (rbf.rotationFollowType == CopyTransform.FollowType.Force)
				{
					rbf.rotationForceMult =
						EditorGUILayout.FloatField("Rotation Force Multiplier", rbf.rotationForceMult);
				}

				if (rbf.rotationFollowType == CopyTransform.FollowType.Velocity ||
				    rbf.rotationFollowType == CopyTransform.FollowType.Force)
				{
					rbf.snapIfAngleGreaterThan = EditorGUILayout.FloatField(
						new GUIContent("Snap Angle",
							"If the object needs to rotate farther than this angle in one frame, it will snap immediately. 0 to disable."),
						rbf.snapIfAngleGreaterThan);
				}

				rbf.useVector3RotationOffset =
					EditorGUILayout.Toggle("Use Vector3 for rotation offset", rbf.useVector3RotationOffset);
				if (rbf.useVector3RotationOffset)
				{
					rbf.rotationOffsetVector3 =
						EditorGUILayout.Vector3Field("Rotation Offset (Vector3 form)", rbf.rotationOffsetVector3);
				}
				else
				{
					rbf.rotationOffset = Vector4ToQuaternion(
						EditorGUILayout.Vector4Field("Rotation Offset (Quaternion form)",
							QuaternionToVector4(rbf.rotationOffset)));
				}

				rbf.rotationOffsetCoordinateSystem =
					(Space)EditorGUILayout.EnumPopup("Offset Coordinate System",
						rbf.rotationOffsetCoordinateSystem);

				rbf.singleAxisRotation = EditorGUILayout.Toggle("Rotate Around Single Axis", rbf.singleAxisRotation);
				if (rbf.singleAxisRotation)
				{
					rbf.singleAxisRotationAxis = (Axis3D)
						EditorGUILayout.EnumPopup("Which Axis to Rotate Around", rbf.singleAxisRotationAxis);
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			using (new EditorGUI.DisabledScope(!rbf.followRotation && !rbf.followPosition))
			{
				rbf.smoothness =
					EditorGUILayout.Slider("Smoothness", rbf.smoothness, 0, 1);
			}
		}

		static Vector4 QuaternionToVector4(Quaternion rot)
		{
			return new Vector4(rot.x, rot.y, rot.z, rot.w);
		}

		static Quaternion Vector4ToQuaternion(Vector4 rot)
		{
			return new Quaternion(rot.x, rot.y, rot.z, rot.w);
		}
	}
#endif
}