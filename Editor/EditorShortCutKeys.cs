using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace VelUtils.Editor
{
	/// <summary>
	/// Adds `F5` shortcut to enter play mode.
	/// </summary>
	public class EditorShortCutKeys : ScriptableObject
	{
		static Scene lastOpenedScene;

		[MenuItem("Edit/Run _F5")] // shortcut key F5 to Play (and exit playmode also)
		static void PlayGame()
		{
			if (!Application.isPlaying)
			{
				EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);
				OpenMainScene();
			}

			EditorApplication.ExecuteMenuItem("Edit/Play");

			//if (Application.isPlaying) {
			//	ReloadLastScene();
			//}
		}

		static void OpenMainScene()
		{
			lastOpenedScene = SceneManager.GetActiveScene();
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
			if (SceneManager.GetActiveScene().path != SceneUtility.GetScenePathByBuildIndex(0))
			{
				EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(0));
			}
		}

		static void ReloadLastScene()
		{
			EditorSceneManager.OpenScene(lastOpenedScene.path);
		}
	}

	/// <summary>
	/// Adds the [ReadOnly] inspector attribute.
	/// Add [ReadOnly] to any serialized variable, and it will show up in the inspector, but not be editable. 
	/// </summary>
	public class ReadOnlyAttribute : PropertyAttribute
	{
	}

	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnly : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = true;
		}
	}

	// not yet functional
	public class ReadOnlyPlayModeAttribute : PropertyAttribute
	{
	}

	[CustomPropertyDrawer(typeof(ReadOnlyPlayModeAttribute))]
	public class ReadOnlyPlayMode : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (Application.isPlaying)
			{
				GUI.enabled = false;
				EditorGUI.PropertyField(position, property, label, true);
				GUI.enabled = true;
			}
			else
			{
				base.OnGUI(position, property, label);
			}
		}
	}
}