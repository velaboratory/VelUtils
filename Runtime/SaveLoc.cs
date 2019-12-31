using UnityEditor;
using UnityEngine;

namespace unityutilities {

	/// <summary>
	/// Saves the location and rotation of an object to playerprefs. Local or global coordinates. Requires a unique object name.
	/// </summary>
	[AddComponentMenu("unityutilities/SaveLoc")]
	public class SaveLoc : MonoBehaviour {

		public bool save = true;
		public bool load = true;
		[Space]
		public Space coordinateSystem = Space.Self;

		private void Start() {
			if (load) {
				Load();
			}
		}

		private void OnApplicationQuit() {
			if (save) {
				Save();
			}
		}

		public void Load() {
			switch (coordinateSystem) {
				case Space.Self:
					var localPosition = transform.localPosition;
					localPosition = new Vector3(
						PlayerPrefs.GetFloat(name + "_xLPos", localPosition.x),
						PlayerPrefs.GetFloat(name + "_yLPos", localPosition.y),
						PlayerPrefs.GetFloat(name + "_zLPos", localPosition.z));
					transform.localPosition = localPosition;
					var localEulerAngles = transform.localEulerAngles;
					localEulerAngles = new Vector3(
						PlayerPrefs.GetFloat(name + "_xLRot", localEulerAngles.x),
						PlayerPrefs.GetFloat(name + "_yLRot", localEulerAngles.y),
						PlayerPrefs.GetFloat(name + "_zLRot", localEulerAngles.z));
					transform.localEulerAngles = localEulerAngles;
					break;
				case Space.World:
					var position = transform.position;
					position = new Vector3(
						PlayerPrefs.GetFloat(name + "_xPos", position.x),
						PlayerPrefs.GetFloat(name + "_yPos", position.y),
						PlayerPrefs.GetFloat(name + "_zPos", position.z));
					transform.position = position;
					var eulerAngles = transform.eulerAngles;
					eulerAngles = new Vector3(
						PlayerPrefs.GetFloat(name + "_xRot", eulerAngles.x),
						PlayerPrefs.GetFloat(name + "_yRot", eulerAngles.y),
						PlayerPrefs.GetFloat(name + "_zRot", eulerAngles.z));
					transform.eulerAngles = eulerAngles;
					break;
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public void Save() {
			switch (coordinateSystem) {
				case Space.Self:
					var localPosition = transform.localPosition;
					PlayerPrefs.SetFloat(name + "_xLPos", localPosition.x);
					PlayerPrefs.SetFloat(name + "_yLPos", localPosition.y);
					PlayerPrefs.SetFloat(name + "_zLPos", localPosition.z);
					var localEulerAngles = transform.localEulerAngles;
					PlayerPrefs.SetFloat(name + "_xLRot", localEulerAngles.x);
					PlayerPrefs.SetFloat(name + "_yLRot", localEulerAngles.y);
					PlayerPrefs.SetFloat(name + "_zLRot", localEulerAngles.z);
					break;
				case Space.World:
					var position = transform.position;
					PlayerPrefs.SetFloat(name + "_xPos", position.x);
					PlayerPrefs.SetFloat(name + "_yPos", position.y);
					PlayerPrefs.SetFloat(name + "_zPos", position.z);
					var eulerAngles = transform.eulerAngles;
					PlayerPrefs.SetFloat(name + "_xRot", eulerAngles.x);
					PlayerPrefs.SetFloat(name + "_yRot", eulerAngles.y);
					PlayerPrefs.SetFloat(name + "_zRot", eulerAngles.z);
					break;
			}
		}
	}

#if UNITY_EDITOR
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

			if (!GUILayout.Button("Load from PlayerPrefs")) return;
			if (sl != null) sl.Load();

		}
	}
#endif
}