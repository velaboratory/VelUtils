using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VelUtils
{
	/// <summary>
	/// Saves a variety of components of an object to PlayerPrefsJson. Local or global coordinates. Requires a unique object name.
	/// </summary>
	[AddComponentMenu("VelUtils/SaveComponent")]
	public class SaveComponent : MonoBehaviour
	{
		public Component target;
		[Space] public bool save = true;
		public bool load = true;

		[Tooltip("Only used for Transform types")] [Space]
		public Space coordinateSystem = Space.Self;

		private static readonly List<SaveComponent> allInstances = new List<SaveComponent>();

		private void Awake()
		{
			allInstances.Add(this);

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

		public static void SaveAll()
		{
			allInstances
				.Where(i => i != null)
				.Where(i => i.save)
				.ToList().ForEach(i => i.Save());
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
				case TMP_InputField inputField:
					string key = name + "_tmpinputfield";
					if (PlayerPrefsJson.HasKey(key))
						inputField.text = PlayerPrefsJson.GetString(key);
					break;
				case InputField inputField:
					key = name + "_inputfield";
					if (PlayerPrefsJson.HasKey(key))
						inputField.text = PlayerPrefsJson.GetString(key);
					break;
				case Toggle toggle:
					key = name + "_toggle";
					if (PlayerPrefsJson.HasKey(key))
						toggle.isOn = PlayerPrefsJson.GetBool(name + "_toggle");
					break;
				case TMP_Dropdown dropdown:
					key = name + "_tmpdropdown";
					if (PlayerPrefsJson.HasKey(key))
						dropdown.value = PlayerPrefsJson.GetInt(name + "_tmpdropdown");
					break;
				case Dropdown dropdown:
					key = name + "_dropdown";
					if (PlayerPrefsJson.HasKey(key))
						dropdown.value = PlayerPrefsJson.GetInt(name + "_dropdown");
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
				case TMP_InputField inputField:
					PlayerPrefsJson.SetString(name + "_tmpinputfield", inputField.text);
					break;
				case InputField inputField:
					PlayerPrefsJson.SetString(name + "_inputfield", inputField.text);
					break;
				case Toggle toggle:
					PlayerPrefsJson.SetBool(name + "_toggle", toggle.isOn);
					break;
				case TMP_Dropdown dropdown:
					PlayerPrefsJson.SetInt(name + "_tmpdropdown", dropdown.value);
					break;
				case Dropdown dropdown:
					PlayerPrefsJson.SetInt(name + "_dropdown", dropdown.value);
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
			if (target is SaveComponent sl && target != null)
			{
				if (sl.target == null && sl.GetComponent<InputField>())
				{
					sl.target = sl.GetComponent<InputField>();
				}

				if (sl.target == null && sl.GetComponent<TMP_Dropdown>())
				{
					sl.target = sl.GetComponent<TMP_Dropdown>();
				}

				if (sl.target == null && sl.GetComponent<Dropdown>())
				{
					sl.target = sl.GetComponent<Dropdown>();
				}

				if (sl.target == null && sl.GetComponent<Toggle>())
				{
					sl.target = sl.GetComponent<Toggle>();
				}

				if (sl.target == null)
				{
					sl.target = sl.transform;
				}
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