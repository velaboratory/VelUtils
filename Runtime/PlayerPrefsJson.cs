using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
			if (instance == null)
			{
				instance = new GameObject("PlayerPrefsJson").AddComponent<PlayerPrefsJson>();
			}
			instance.filename = Path.Combine(Application.persistentDataPath, "PlayerPrefsJson.json");
			instance.Load();
		}

		private void Awake()
		{
			if (!init) Init();
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
			if (!init) Init();
			instance.data[id] = value;
			instance.dirty = true;
		}

		public static void SetInt(string id, int value)
		{
			if (!init) Init();
			instance.data[id] = value;
			instance.dirty = true;
		}

		public static void SetVector3(string id, Vector3 value)
		{
			if (!init) Init();
			instance.data[id] = JToken.FromObject(value);
			instance.dirty = true;
		}

		public static void SetQuaternion(string id, Quaternion value)
		{
			if (!init) Init();
			instance.data[id] = JToken.FromObject(value);
			instance.dirty = true;
		}
		#endregion


		#region Get Data
		public static float GetFloat(string id, float defaultValue = 0)
		{
			if (!init) Init();
			if (instance.data.ContainsKey(id))
			{
				return (float)instance.data[id];
			}
			else
			{
				return defaultValue;
			}
		}

		public static int GetInt(string id, int defaultValue = 0)
		{
			if (!init) Init();
			if (instance.data.ContainsKey(id))
			{
				return (int)instance.data[id];
			}
			else
			{
				return defaultValue;
			}
		}

		public static Vector3 GetVector3(string id, Vector3 defaultValue = new Vector3())
		{
			if (!init) Init();
			if (instance.data.ContainsKey(id))
			{
				return instance.data[id].ToObject<Vector3>();
			}
			else
			{
				return defaultValue;
			}
		}

		public static Quaternion GetQuaternion(string id, Quaternion defaultValue = new Quaternion())
		{
			if (!init) Init();
			if (instance.data.ContainsKey(id))
			{
				return instance.data[id].ToObject<Quaternion>();
			}
			else
			{
				return defaultValue;
			}
		}
		#endregion

		private void Load()
		{
			if (File.Exists(filename))
			{
				data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(filename));
			}
		}

		private void Save()
		{
			File.WriteAllText(filename, JsonConvert.SerializeObject(data));

			dirty = false;
		}
	}
}