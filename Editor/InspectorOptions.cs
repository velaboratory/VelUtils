using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RigidbodyFollower))]
public class RigidbodyFollowerEditor : Editor
{
	override public void OnInspectorGUI()
	{
		var rbf = target as RigidbodyFollower;

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		rbf.target = (Transform)EditorGUILayout.ObjectField("Target", rbf.target, typeof(Transform));
		
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
		rbf.followPosition = GUILayout.Toggle(rbf.followPosition, "Follow Position");
		using (new EditorGUI.DisabledScope(!rbf.followPosition))
		{
			rbf.positionFollowType =
				(RigidbodyFollower.FollowType) EditorGUILayout.EnumPopup("Position Follow Type",
					rbf.positionFollowType);
			if (rbf.positionFollowType == RigidbodyFollower.FollowType.Force)
			{
				rbf.positionForceMult = EditorGUILayout.FloatField("Position Force Mult", rbf.positionForceMult);
			}

			rbf.positionOffset = EditorGUILayout.Vector3Field("Position Offset", rbf.positionOffset);
		}

		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
		rbf.followRotation = GUILayout.Toggle(rbf.followRotation, "Follow Rotation");
		using (new EditorGUI.DisabledScope(!rbf.followRotation))
		{
			EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
			rbf.rotationFollowType =
				(RigidbodyFollower.FollowType) EditorGUILayout.EnumPopup("Rotation Follow Type",
					rbf.rotationFollowType);
			if (rbf.rotationFollowType == RigidbodyFollower.FollowType.Force)
			{
				rbf.rotationForceMult = EditorGUILayout.FloatField("Rotation Force Mult", rbf.rotationForceMult);
			}

			rbf.rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rbf.rotationOffset);
		}
	}
}