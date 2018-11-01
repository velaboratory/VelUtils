using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds `F5` shortcut to enter play mode.
/// </summary>
public class EditorShortCutKeys : ScriptableObject {

	[MenuItem("Edit/Run _F5")] // shortcut key F5 to Play (and exit playmode also)
	static void PlayGame() {
		if (!Application.isPlaying) {
			EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);
		}
		EditorApplication.ExecuteMenuItem("Edit/Play");
	}

}