using UnityEditor;
using UnityEngine;

namespace unityutilities {
	/// <summary>
	/// Allows for loading from playerprefs in the Editor.
	/// </summary>
	[CustomEditor(typeof(SaveLoc))]
	public class SaveLocEditor : Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var sl = target as SaveLoc;

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (GUILayout.Button("Load from PlayerPrefs")) {
				sl.Load();
			}

		}
	}
}