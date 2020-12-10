using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace unityutilities
{
	/// <summary>
	/// Saves a variety of components of an object to PlayerPrefsJson. Local or global coordinates. Requires a unique object name.
	/// </summary>
	[AddComponentMenu("unityutilities/SaveComponent")]
	public class SaveComponent : MonoBehaviour
	{
		public Component target;
		[Space] public bool save = true;
		public bool load = true;
		[Space] public Space coordinateSystem = Space.Self;

		private void Awake()
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

		// ReSharper disable once MemberCanBePrivate.Global
		public void Load()
		{
			if (target == null) return;

			switch (target)
			{
				case IDictionaryPackable dictObj:
					Dictionary<string, object> dict = PlayerPrefsJson.GetDictionary(name + "_dict");
					if (dict != null) dictObj.UnpackData(dict);

					break;
				case INetworkPack netpack:
					string loadedVal = PlayerPrefsJson.GetString(name + "_bytes");
					if (!string.IsNullOrEmpty(loadedVal))
					{
						netpack.UnpackData(Convert.FromBase64String(loadedVal));
					}

					break;
				case Transform t:
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

					break;
				case InputField inputField:
					string key = name + "_inputfield";
					if (PlayerPrefsJson.HasKey(key))
						inputField.text = PlayerPrefsJson.GetString(key);
					break;
				case Toggle toggle:
					key = name + "_toggle";
					if (PlayerPrefsJson.HasKey(key))
						toggle.isOn = PlayerPrefsJson.GetBool(name + "_toggle");
					break;
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public void Save()
		{
			if (target == null) return;
			switch (target)
			{
				case IDictionaryPackable dictObj:
					Dictionary<string, object> data = dictObj.PackData();
					if (data != null) PlayerPrefsJson.SetDictionary(name + "_dict", data);
					break;
				case INetworkPack netpack:
					PlayerPrefsJson.SetString(name + "_bytes", Convert.ToBase64String(netpack.PackData()));
					break;
				case Transform t:
					switch (coordinateSystem)
					{
						case Space.Self:
							PlayerPrefsJson.SetVector3(name + "_LocalPos", t.localPosition);
							PlayerPrefsJson.SetVector3(name + "_LocalRot", t.localEulerAngles);
							break;
						case Space.World:
							PlayerPrefsJson.SetVector3(name + "_Pos", t.position);
							PlayerPrefsJson.SetVector3(name + "_Rot", t.eulerAngles);
							break;
						default:
							throw new ArgumentOutOfRangeException("Needs to be either Space.Self or Space.World");
					}

					break;
				case InputField inputField:
					PlayerPrefsJson.SetString(name + "_inputfield", inputField.text);
					break;
				case Toggle toggle:
					PlayerPrefsJson.SetBool(name + "_toggle", toggle.isOn);
					break;
			}

#if UNITY_EDITOR
			PlayerPrefsJson.instance?.Save();
#endif
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Allows for loading from playerprefsjson in the Editor.
	/// </summary>
	[CustomEditor(typeof(SaveComponent))]
	public class SaveComponentEditor : Editor
	{
		private void OnEnable()
		{
			// add a transform as the default if nothing is added yet
			if (target is SaveComponent sl && target != null && sl.target == null)
			{
				sl.target = sl.transform;
			}
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			SaveComponent sl = target as SaveComponent;
			if (sl == null) return;

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (GUILayout.Button("Load from PlayerPrefsJson")) sl.Load();
			if (GUILayout.Button("Save to PlayerPrefsJson")) sl.Save();
		}
	}
#endif
}