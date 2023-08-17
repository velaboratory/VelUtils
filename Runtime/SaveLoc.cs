using System;
using UnityEditor;
using UnityEngine;

namespace VelUtils
{

	/// <summary>
	/// Saves the location and rotation of an object to PlayerPrefsJson. Local or global coordinates. Requires a unique object name.
	/// </summary>
	[AddComponentMenu("VelUtils/SaveLoc")]
	public class SaveLoc : MonoBehaviour
	{

		public bool save = true;
		public bool load = true;
		[Space]
		public Space coordinateSystem = Space.Self;

		[Space]
		public Component customType;

		private void Start()
		{
			if (load)
			{
				Load();
			}
		}

		private void OnApplicationQuit()
		{
			if (save)
			{
				Save();
			}
		}

		public void Load()
		{
			if (customType != null && customType is INetworkPack netpack)
			{
				string loadedVal = PlayerPrefs.GetString(name + "_bytes");
				if (!string.IsNullOrEmpty(loadedVal))
				{
					netpack.UnpackData(Convert.FromBase64String(loadedVal));
				}
			}
			else
			{
				Transform t = transform;
				switch (coordinateSystem)
				{
					case Space.Self:
						t.localPosition = PlayerPrefsJson.GetVector3(name + "_LocalPos", t.localPosition);
						t.localEulerAngles = PlayerPrefsJson.GetVector3(name + "_LocalRot", t.localEulerAngles);
						break;
					case Space.World:
						t.position = PlayerPrefsJson.GetVector3(name + "_Pos", t.position);
						t.eulerAngles = PlayerPrefsJson.GetVector3(name + "_Rot", t.eulerAngles);
						break;
					default:
						throw new ArgumentOutOfRangeException("Needs to be either Space.Self or Space.World");
				}
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public void Save()
		{
			if (customType != null && customType is INetworkPack netpack)
			{
				PlayerPrefs.SetString(name + "_bytes", Convert.ToBase64String(netpack.PackData()));
			}
			else
			{
				switch (coordinateSystem)
				{
					case Space.Self:
						PlayerPrefsJson.SetVector3(name+"_LocalPos", transform.localPosition);
						PlayerPrefsJson.SetVector3(name+"_LocalRot", transform.localEulerAngles);
						break;
					case Space.World:
						PlayerPrefsJson.SetVector3(name+"_Pos", transform.position);
						PlayerPrefsJson.SetVector3(name+"_Rot", transform.eulerAngles);
						break;
					default:
						throw new ArgumentOutOfRangeException("Needs to be either Space.Self or Space.World");
				}
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Allows for loading from playerprefs in the Editor.
	/// </summary>
	[CustomEditor(typeof(SaveLoc))]
	public class SaveLocEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			var sl = target as SaveLoc;

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (!GUILayout.Button("Load from PlayerPrefs")) return;
			if (sl != null) sl.Load();

		}
	}
#endif
}