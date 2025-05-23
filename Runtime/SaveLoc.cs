using System;
using UnityEditor;
using UnityEngine;

namespace VelUtils
{
	/// <summary>
	/// Saves the location and rotation of an object to PlayerPrefsJson. Local or global coordinates. This is deprecated. Use SaveComponent instead.
	/// </summary>
	[AddComponentMenu("VelUtils/SaveLoc")]
	[Obsolete("Use SaveComponent instead")]
	public class SaveLoc : MonoBehaviour
	{
		public bool save = true;
		public bool load = true;
		[Space] public Space coordinateSystem = Space.Self;

		[Space] public Component customType;

		[ReadOnly] public string id;

		private void Start()
		{
			if (load)
			{
				Load();
			}
		}

		private void OnApplicationFocus(bool focus)
		{
			if (!focus)
			{
				Save();
			}
		}

		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				Save();
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
				string loadedVal = PlayerPrefs.GetString(FieldToKey("bytes"));
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
						t.localPosition = PlayerPrefsJson.GetVector3(FieldToKey("LocalPos"), t.localPosition);
						t.localEulerAngles = PlayerPrefsJson.GetVector3(FieldToKey("LocalRot"), t.localEulerAngles);
						break;
					case Space.World:
						t.position = PlayerPrefsJson.GetVector3(FieldToKey("Pos"), t.position);
						t.eulerAngles = PlayerPrefsJson.GetVector3(FieldToKey("Rot"), t.eulerAngles);
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
				PlayerPrefs.SetString(FieldToKey("bytes"), Convert.ToBase64String(netpack.PackData()));
			}
			else
			{
				switch (coordinateSystem)
				{
					case Space.Self:
						PlayerPrefsJson.SetVector3(FieldToKey("LocalPos"), transform.localPosition);
						PlayerPrefsJson.SetVector3(FieldToKey("LocalRot"), transform.localEulerAngles);
						break;
					case Space.World:
						PlayerPrefsJson.SetVector3(FieldToKey("Pos"), transform.position);
						PlayerPrefsJson.SetVector3(FieldToKey("Rot"), transform.eulerAngles);
						break;
					default:
						throw new ArgumentOutOfRangeException("Needs to be either Space.Self or Space.World");
				}
			}
		}

		private string FieldToKey(string fieldName)
		{
			return $"{name}_{id}_{fieldName}";
		}

		public void GenerateID()
		{
			id = Guid.NewGuid().ToString();
		}
	}


#if UNITY_EDITOR
	/// <summary>
	/// Allows for loading from playerprefs in the Editor.
	/// </summary>
	[CustomEditor(typeof(SaveLoc))]
	[Obsolete]
	public class SaveLocEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			SaveLoc sl = target as SaveLoc;
			if (sl == null) return;

			if (string.IsNullOrEmpty(sl.id)) sl.GenerateID();

			if (GUILayout.Button("Regenerate"))
			{
				sl.GenerateID();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (!GUILayout.Button("Load from PlayerPrefs")) return;
			if (sl != null) sl.Load();
		}
	}
#endif
}