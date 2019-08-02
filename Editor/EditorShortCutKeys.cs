using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds `F5` shortcut to enter play mode.
/// </summary>
public class EditorShortCutKeys : ScriptableObject {

	static Scene lastOpenedScene;

	[MenuItem("Edit/Run _F5")] // shortcut key F5 to Play (and exit playmode also)
	static void PlayGame() {
		if (!Application.isPlaying) {
			EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);
			OpenMainScene();
		}
		EditorApplication.ExecuteMenuItem("Edit/Play");

		//if (Application.isPlaying) {
		//	ReloadLastScene();
		//}
	}

	static void OpenMainScene() {
		lastOpenedScene = SceneManager.GetActiveScene();
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		if (SceneManager.GetActiveScene().path != "Assets/Scenes/_Loading.unity") {
			EditorSceneManager.OpenScene("Assets/Scenes/_Loading.unity");
		}
	}

	static void ReloadLastScene() {
		EditorSceneManager.OpenScene(lastOpenedScene.path);
	}

	[MenuItem("Edit/OpenHubWorld _F6")]
	static void OpenMainGameScene() {
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		if (SceneManager.GetActiveScene().path != "Assets/Scenes/_HubWorld.unity") {
			EditorSceneManager.OpenScene("Assets/Scenes/HubWorld.unity");
		}
	}

}