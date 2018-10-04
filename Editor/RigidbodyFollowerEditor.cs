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

		if (rbf.target == null)
		{
			EditorGUILayout.HelpBox("No target assigned. Please assign a target to follow.", MessageType.Error);
		}
		rbf.target = (Transform)EditorGUILayout.ObjectField("Target", rbf.target, typeof(Transform), true);
		
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
		rbf.followPosition = GUILayout.Toggle(rbf.followPosition, "Follow Position");
		using (new EditorGUI.DisabledScope(!rbf.followPosition))
		{
			rbf.positionFollowType =
				(RigidbodyFollower.FollowType) EditorGUILayout.EnumPopup("Position Follow Type",
					rbf.positionFollowType);
			
			if (rbf.positionFollowType != RigidbodyFollower.FollowType.Copy)
			{
				if (!rbf.GetComponent<Rigidbody>())
				{
					EditorGUILayout.HelpBox("No rigidbody attached.", MessageType.Error);
				} else if (rbf.GetComponent<Rigidbody>().drag < 20)
				{
					EditorGUILayout.HelpBox("Rigidbody drag of 30 or so is recommended.", MessageType.Info);
				}
			}
			rbf.useFixedUpdatePos = GUILayout.Toggle(rbf.useFixedUpdatePos, "Use Fixed Update");
			
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
			
			if (rbf.rotationFollowType != RigidbodyFollower.FollowType.Copy)
			{
				if (!rbf.GetComponent<Rigidbody>())
				{
					EditorGUILayout.HelpBox("No rigidbody attached.", MessageType.Error);
				} else if (rbf.GetComponent<Rigidbody>().drag < 20)
				{
					EditorGUILayout.HelpBox("Rigidbody drag of 30 or so is recommended.", MessageType.Info);
				}
			}
			rbf.useFixedUpdateRot = GUILayout.Toggle(rbf.useFixedUpdateRot, "Use Fixed Update");
			if (rbf.rotationFollowType == RigidbodyFollower.FollowType.Force)
			{
				rbf.rotationForceMult = EditorGUILayout.FloatField("Rotation Force Mult", rbf.rotationForceMult);
			}

			rbf.rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rbf.rotationOffset);
		}
	}
}