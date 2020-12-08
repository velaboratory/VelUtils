using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace unityutilities
{
	public class PlayerPrefsJson : MonoBehaviour
	{
		public static PlayerPrefsJson instance;
		private JObject data = new JObject();
		private bool dirty;
		private string filename;

		private static bool init;

		private static void Init()
		{
			init = true;
			if (instance == null) instance = new GameObject("PlayerPrefsJson").AddComponent<PlayerPrefsJson>();
			instance.filename = Path.Combine(Application.persistentDataPath, "PlayerPrefsJson.json");
			instance.Load();
		}


		private static void InitIfNot()
		{
			if (!init) Init();
		}

		private void Awake()
		{
			InitIfNot();
		}

		private void Update()
		{
			if (dirty)
			{
				Save();
			}
		}

		private void OnApplicationQuit()
		{
			Save();
		}

		#region Set Data

		public static void SetFloat(string id, float value)
		{
			InitIfNot();
			instance.data[id] = value;
			instance.dirty = true;
		}

		public static void SetInt(string id, int value)
		{
			InitIfNot();
			instance.data[id] = value;
			instance.dirty = true;
		}

		public static void SetVector3(string id, Vector3 value)
		{
			InitIfNot();
			instance.data[id] = new JObject
			{
				{"x", value.x},
				{"y", value.y},
				{"z", value.z},
			};
			instance.dirty = true;
		}

		public static void SetQuaternion(string id, Quaternion value)
		{
			InitIfNot();
			instance.data[id] = new JObject
			{
				{"x", value.x},
				{"y", value.y},
				{"z", value.z},
				{"w", value.w},
			};
			instance.dirty = true;
		}

		public static void SetString(string id, string value)
		{
			InitIfNot();
			instance.data[id] = value;
			instance.dirty = true;
		}

		public static void SetBool(string id, bool value)
		{
			InitIfNot();
			instance.data[id] = value;
			instance.dirty = true;
		}

		public static void SetDictionary(string id, Dictionary<string,string> value)
		{
			InitIfNot();
			instance.data[id] = JObject.FromObject(value);
			instance.dirty = true;
		}

		#endregion


		#region Get Data

		public static float GetFloat(string id, float defaultValue = 0)
		{
			InitIfNot();
			return HasKey(id) ? (float) instance.data[id] : defaultValue;
		}

		public static int GetInt(string id, int defaultValue = 0)
		{
			InitIfNot();
			return HasKey(id) ? (int) instance.data[id] : defaultValue;
		}

		public static Vector3 GetVector3(string id, Vector3 defaultValue = new Vector3())
		{
			InitIfNot();
			return HasKey(id) ? instance.data[id].ToObject<Vector3>() : defaultValue;
		}

		public static Quaternion GetQuaternion(string id, Quaternion defaultValue = new Quaternion())
		{
			InitIfNot();
			return HasKey(id) ? instance.data[id].ToObject<Quaternion>() : defaultValue;
		}

		public static string GetString(string id, string defaultValue = "")
		{
			InitIfNot();
			return HasKey(id) ? (string) instance.data[id] : defaultValue;
		}

		public static bool GetBool(string id, bool defaultValue = false)
		{
			InitIfNot();
			return HasKey(id) ? (bool) instance.data[id] : defaultValue;
		}
		public static Dictionary<string,string> GetDictionary(string id, Dictionary<string,string> defaultValue = null)
		{
			InitIfNot();
			return HasKey(id) ? instance.data[id]?.ToObject<Dictionary<string,string>>() : defaultValue;
		}

		#endregion

		#region Check Data

		public static bool HasKey(string id)
		{
			InitIfNot();
			return instance.data.ContainsKey(id);
		}

		#endregion

		private void Load()
		{
			if (File.Exists(filename))
			{
				data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(filename));
			}
		}

		public void Save()
		{
			File.WriteAllText(filename, JsonConvert.SerializeObject(data, Formatting.Indented));

			dirty = false;
		}
	}


	internal static class TupleToVectorExtensions
	{
		public static Vector3 ToVector3(this (float x, float y, float z) val)
		{
			(float x, float y, float z) = val;
			return new Vector3(x, y, z);
		}

		public static Quaternion ToQuaternion(this (float x, float y, float z, float w) val)
		{
			(float x, float y, float z, float w) = val;
			return new Quaternion(x, y, z, w);
		}

		public static (float x, float y, float z) ToTuple(this Vector3 val)
		{
			return (val.x, val.y, val.z);
		}

		public static (float x, float y, float z, float w) ToTuple(this Quaternion val)
		{
			return (val.x, val.y, val.z, val.w);
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Allows for loading from playerprefsjson in the Editor.
	/// </summary>
	[CustomEditor(typeof(PlayerPrefsJson))]
	public class PlayerPrefsJsonEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			PlayerPrefsJson t = target as PlayerPrefsJson;

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (GUILayout.Button("Show PlayerPrefsJson.json in Explorer"))
			{
				EditorUtility.RevealInFinder(Path.Combine(Application.persistentDataPath, "PlayerPrefsJson.json"));
			}
		}
	}
#endif
}