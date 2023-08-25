using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace VelUtils.Editor
{
	[InitializeOnLoad]
	public class IncrementApplicationVersion
	{
		private const string autoIncrementKey = "AutoIncrementPlayerVersion";
		private const string autoIncrementMenuPath = "Window/Automatically increment player version";

		private static bool IsSettingEnabled
		{
			get => EditorPrefs.GetBool(autoIncrementKey);
			set => EditorPrefs.SetBool(autoIncrementKey, value);
		}

		[MenuItem(autoIncrementMenuPath)]
		private static void Setting()
		{
			IsSettingEnabled = !IsSettingEnabled;
		}

		[MenuItem(autoIncrementMenuPath, true)]
		private static bool SettingValidate()
		{
			Menu.SetChecked(autoIncrementMenuPath, IsSettingEnabled);
			return true;
		}

		[PostProcessBuild(1)]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
			Debug.Log("Build v" + PlayerSettings.bundleVersion);
			if (!EditorPrefs.GetBool(autoIncrementKey, false)) return;
			IncreaseBuild();
			Debug.Log("Incrementing player version: " + PlayerSettings.bundleVersion);
		}

		[MenuItem("Window/Increment Player Version Now")]
		private static void IncreaseBuild()
		{
			string[] pieces = PlayerSettings.bundleVersion.Split('.');

			pieces[^1] = (int.Parse(pieces[^1]) + 1).ToString();

			PlayerSettings.bundleVersion = string.Join('.', pieces);

			PlayerSettings.Android.bundleVersionCode += 1;
		}
	}
}