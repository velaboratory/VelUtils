using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace unityutilities
{
	public class PlayerPrefsJson : MonoBehaviour
	{
		public static PlayerPrefsJson instance;

		private readonly Dictionary<string, Dictionary<string, object>> data =
			new Dictionary<string, Dictionary<string, object>>();

		private const string filename = "PlayerPrefsJson.json";
		private static string defaultPath;
		private bool dirty;
		private static bool init;
		private float lastSaveTime;
		private float saveInterval = 1f; // save at most once a second

		private static void Init()
		{
			if (instance == null) instance = FindObjectOfType<PlayerPrefsJson>();
			if (instance == null) instance = new GameObject("PlayerPrefsJson").AddComponent<PlayerPrefsJson>();
			instance.data["default"] = new Dictionary<string, object>();
			defaultPath = Path.Combine(Application.persistentDataPath, filename);
			instance.Load();

			init = true;
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
			if (dirty && Time.time - lastSaveTime > saveInterval)
			{
				Save();
				lastSaveTime = Time.time;
			}
		}

		private void OnApplicationQuit()
		{
			SaveComponent.SaveAll();
			Save();
		}

		#region Set Data

		public static void SetFloat(string id, float value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();
			instance.data[path][id] = value;
			instance.dirty = true;
		}

		public static void SetInt(string id, int value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();
			instance.data[path][id] = value;
			instance.dirty = true;
		}

		public static void SetVector3(string id, Vector3 value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();
			instance.data[path][id] = value.ToDictionary();
			instance.dirty = true;
		}

		public static void SetQuaternion(string id, Quaternion value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();
			instance.data[path][id] = value.ToDictionary();
			instance.dirty = true;
		}

		public static void SetString(string id, string value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();
			instance.data[path][id] = value;
			instance.dirty = true;
		}

		public static void SetBool(string id, bool value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();
			instance.data[path][id] = value;
			instance.dirty = true;
		}

		public static void SetDictionary(string id, Dictionary<string, object> value, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.data[path] = new Dictionary<string, object>();

			// convert vector3 and quaternion to serializable types
			Dictionary<string, object> newValues = new Dictionary<string, object>();
			foreach (KeyValuePair<string, object> valuePair in value)
			{
				switch (valuePair.Value)
				{
					case Vector3 vec:
						newValues[valuePair.Key] = vec.ToDictionary();
						break;
					case Quaternion quat:
						newValues[valuePair.Key] = quat.ToDictionary();
						break;
					default:
						newValues[valuePair.Key] = valuePair.Value;
						break;
				}
			}

			instance.data[path][id] = newValues;
			instance.dirty = true;
		}

		#endregion


		#region Get Data

		public static float GetFloat(string id, float defaultValue = 0, string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
				return (float) instance.data[path][id];
			SetFloat(id, defaultValue, path);
			return defaultValue;
		}

		public static int GetInt(string id, int defaultValue = 0, string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
				return (int) (long) instance.data[path][id];
			SetInt(id, defaultValue, path);
			return defaultValue;
		}

		public static Vector3 GetVector3(string id, Vector3 defaultValue = new Vector3(), string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
				return ((Dictionary<string, object>) instance.data[path][id]).ToVector3();

			SetVector3(id, defaultValue, path);
			return defaultValue;
		}

		// public static Vector3 GetVector3(List<string> ids, Vector3 defaultValue = new Vector3(),
		// 	string path = "default")
		// {
		// 	InitIfNot();
		// 	foreach (var id in ids)
		// 	{
		// 		if (HasKey(id, path))
		// 			return ((JObject) instance.data[path][id]).ToObject<Vector3>();
		// 	}
		//
		// 	SetVector3(id, defaultValue, path);
		// 	return defaultValue;
		// }

		public static Quaternion GetQuaternion(string id, Quaternion defaultValue = new Quaternion(),
			string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
				return ((Dictionary<string, object>) instance.data[path][id]).ToQuaternion();

			SetQuaternion(id, defaultValue, path);
			return defaultValue;
		}

		public static string GetString(string id, string defaultValue = "", string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
				return (string) instance.data[path][id];

			SetString(id, defaultValue, path);
			return defaultValue;
		}

		public static bool GetBool(string id, bool defaultValue = false, string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
				return (bool) instance.data[path][id];

			SetBool(id, defaultValue, path);
			return defaultValue;
		}

		public static Dictionary<string, object> GetDictionary(string id,
			Dictionary<string, object> defaultValue = null, string path = "default")
		{
			InitIfNot();
			if (HasKey(id, path))
			{
				switch (instance.data[path][id])
				{
					case Dictionary<string, object> dict:
						return dict;
					default:
						throw new Exception("What type?");
				}

				// return ((JObject) instance.data[path][id]);
				return (Dictionary<string, object>) instance.data[path][id];
			}

			SetDictionary(id, defaultValue, path);
			return defaultValue;
		}

		// public static (Vector3 pos, Vector3 rot, Vector3 scale) GetTransform(string id, string path = "default")
		// {
		// 	return GetTransform(id, (Vector3.zero, Vector3.zero, Vector3.one), path);
		// }
		//
		// public static (Vector3 pos, Vector3 rot, Vector3 scale) GetTransform(string id,
		// 	(Vector3 pos, Vector3 rot, Vector3 scale) defaultValue, string path = "default")
		// {
		// 	InitIfNot();
		// 	if (HasKey(id, path))
		// 		return ((JObject) instance.data[path][id]).ToObject<Dictionary<string, object>>();
		//
		// 	SetDictionary(id, defaultValue, path);
		// 	return defaultValue;
		// }

		#endregion

		#region Check Data

		public static bool HasKey(string id, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.Load(path);
			if (!instance.data.ContainsKey(path)) return false;
			if (instance.data[path] == null) instance.data[path] = new Dictionary<string, object>();
			return instance.data[path].ContainsKey(id);
		}

		public static bool HasKey(string id, string subkey, string path = "default")
		{
			InitIfNot();
			if (!instance.data.ContainsKey(path)) instance.Load(path);
			return instance.data.ContainsKey(path) && instance.data[path].ContainsKey(id) &&
			       ((Dictionary<string, object>) instance.data[path][id]).ContainsKey(subkey);
		}

		#endregion

		private void Load()
		{
			string[] paths = data.Keys.ToArray();
			foreach (string key in paths)
			{
				Load(key);
			}
		}

		private void Load(string key)
		{
			string path = key == "default" ? defaultPath : key;
			if (!File.Exists(path)) return;
			try
			{
				data[key] = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path),
					new NestedDictsConverter());
			}
			catch (Exception)
			{
				File.Delete(path);
			}
		}

		public void Save()
		{
			Task.Run(SaveTask);
		}

		private void SaveTask()
		{
			foreach (string key in data.Keys)
			{
				string path = key == "default" ? defaultPath : key;
				File.WriteAllText(path, JsonConvert.SerializeObject(data[key], Formatting.Indented));

				dirty = false;
			}
		}
	}


	public static class TupleToVectorExtensions
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


		public static Vector3 ToVector3(this Dictionary<string, float> val)
		{
			return new Vector3(val["x"], val["y"], val["z"]);
		}

		public static Quaternion ToQuaternion(this Dictionary<string, float> val)
		{
			return new Quaternion(val["x"], val["y"], val["z"], val["w"]);
		}

		public static Vector3 ToVector3(this Dictionary<string, object> val)
		{
			return new Vector3(Convert.ToSingle(val["x"]), Convert.ToSingle(val["y"]), Convert.ToSingle(val["z"]));
		}

		public static Quaternion ToQuaternion(this Dictionary<string, object> val)
		{
			return new Quaternion((float) val["x"], (float) val["y"], (float) val["z"], (float) val["w"]);
		}

		public static Vector3 ToVector3(this object val)
		{
			switch (val)
			{
				case System.Numerics.Vector3 vec:
					return new Vector3(vec.X,vec.Y,vec.Z);
				case Vector3 vec:
					return vec;
				case Dictionary<string, object> dict:
					return new Vector3(Convert.ToSingle(dict["x"]), Convert.ToSingle(dict["y"]),
						Convert.ToSingle(dict["z"]));
				default:
					throw new Exception("Can't convert to Vector3");
			}
		}

		/// <summary>
		/// Drops the z value
		/// </summary>
		public static Vector2 ToVector2(this Vector3 val)
		{
			return new Vector2(val.x, val.y);
		}

		public static Quaternion ToQuaternion(this object val)
		{
			switch (val)
			{
				case System.Numerics.Quaternion q:
					return new Quaternion(q.X, q.Y,q.Z,q.W);
				case Quaternion q:
					return q;
				case Dictionary<string, object> dict:
					return new Quaternion(
						Convert.ToSingle(dict["x"]), Convert.ToSingle(dict["y"]),
						Convert.ToSingle(dict["z"]), Convert.ToSingle(dict["w"]));
				default:
					throw new Exception("Can't convert to Vector3");
			}
		}

		public static Dictionary<string, object> ToDictionary(this Vector3 val)
		{
			return new Dictionary<string, object>
			{
				{"x", val.x},
				{"y", val.y},
				{"z", val.z},
			};
		}

		public static Dictionary<string, object> ToDictionary(this Quaternion val)
		{
			return new Dictionary<string, object>
			{
				{"x", val.x},
				{"y", val.y},
				{"z", val.z},
				{"w", val.w},
			};
		}
	}

	class NestedDictsConverter : CustomCreationConverter<IDictionary<string, object>>
	{
		public override IDictionary<string, object> Create(Type objectType)
		{
			return new Dictionary<string, object>();
		}

		public override bool CanConvert(Type objectType)
		{
			// in addition to handling IDictionary<string, object>
			// we want to handle the deserialization of dict value
			// which is of type object
			return objectType == typeof(object) || base.CanConvert(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
			JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject
			    || reader.TokenType == JsonToken.Null)
				return base.ReadJson(reader, objectType, existingValue, serializer);

			// if the next token is not an object
			// then fall back on standard deserializer (strings, numbers etc.)
			return serializer.Deserialize(reader);
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