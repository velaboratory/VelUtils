using UnityEditor;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Sets up the interface for the CopyTransform script.
	/// </summary>
	[CustomEditor(typeof(CopyTransform))]
	public class CopyTransformEditor : Editor
	{
		override public void OnInspectorGUI()
		{
			var rbf = target as CopyTransform;

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (rbf.target == null)
			{
				EditorGUILayout.HelpBox("No target assigned. Please assign a target to follow.", MessageType.Error);
			}

			rbf.target = (Transform) EditorGUILayout.ObjectField("Target", rbf.target, typeof(Transform), true);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
			rbf.followPosition = GUILayout.Toggle(rbf.followPosition, "Follow Position");
			using (new EditorGUI.DisabledScope(!rbf.followPosition))
			{
				rbf.positionFollowType =
					(CopyTransform.FollowType) EditorGUILayout.EnumPopup("Position Follow Type",
						rbf.positionFollowType);

				if (rbf.positionFollowType != CopyTransform.FollowType.Copy)
				{
					if (!rbf.GetComponent<Rigidbody>())
					{
						EditorGUILayout.HelpBox("No rigidbody attached.", MessageType.Error);
						// TODO add button to automatically add a rigidbody
					}
					else
					{
						float mass = rbf.GetComponent<Rigidbody>().mass;
						float drag = rbf.GetComponent<Rigidbody>().drag;
						if (drag / mass < 20)
						{
							EditorGUILayout.HelpBox("Rigidbody drag of " + 40 * mass + " or so is recommended.",
								MessageType.Info);
						}
					}
				}

				rbf.useFixedUpdatePos = GUILayout.Toggle(rbf.useFixedUpdatePos, "Use Fixed Update");

				if (rbf.positionFollowType == CopyTransform.FollowType.Force)
				{
					rbf.positionForceMult = EditorGUILayout.FloatField("Position Force Mult", rbf.positionForceMult);
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
				rbf.positionOffsetCoordinateSystem = (Space) EditorGUILayout.EnumPopup("Offset Coordinate System",
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
					(CopyTransform.FollowType) EditorGUILayout.EnumPopup("Rotation Follow Type",
						rbf.rotationFollowType);

				if (rbf.rotationFollowType != CopyTransform.FollowType.Copy)
				{
					if (!rbf.GetComponent<Rigidbody>())
					{
						EditorGUILayout.HelpBox("No rigidbody attached.", MessageType.Error);
						// TODO add button to automatically add a rigidbody
					}
					else
					{
						float mass = rbf.GetComponent<Rigidbody>().mass;
						float angDrag = rbf.GetComponent<Rigidbody>().angularDrag;
						if (angDrag / mass < 10)
						{
							EditorGUILayout.HelpBox("Rigidbody angular drag of " + 20 * mass + " or so is recommended.",
								MessageType.Info);
						}
					}
				}

				rbf.useFixedUpdateRot = GUILayout.Toggle(rbf.useFixedUpdateRot, "Use Fixed Update");
				if (rbf.rotationFollowType == CopyTransform.FollowType.Force)
				{
					rbf.rotationForceMult = EditorGUILayout.FloatField("Rotation Force Mult", rbf.rotationForceMult);
				}

				if (rbf.rotationFollowType == CopyTransform.FollowType.Velocity ||
				    rbf.rotationFollowType == CopyTransform.FollowType.Force)
				{
					rbf.snapIfAngleGreaterThan = EditorGUILayout.FloatField(
						new GUIContent("Snap Angle",
							"If the object needs to rotate farther than this angle in one frame, it will snap immediately. 0 to disable."),
						rbf.snapIfAngleGreaterThan);
				}

				//rbf.rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rbf.rotationOffset);
				rbf.rotationOffsetCoordinateSystem = (Space) EditorGUILayout.EnumPopup("Offset Coordinate System",
					rbf.rotationOffsetCoordinateSystem);
			}
		}
	}
}