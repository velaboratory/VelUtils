using UnityEditor;
using UnityEngine;

namespace unityutilities {

	/// <summary>
	/// Saves the location and rotation of an object to playerprefs. Local or global coordinates. Requires a unique object name.
	/// </summary>
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
			if (coordinateSystem == Space.Self) {
				transform.localPosition = new Vector3(
					PlayerPrefs.GetFloat(name + "_xLPos", transform.localPosition.x),
					PlayerPrefs.GetFloat(name + "_yLPos", transform.localPosition.y),
					PlayerPrefs.GetFloat(name + "_zLPos", transform.localPosition.z));
				transform.localEulerAngles = new Vector3(
					PlayerPrefs.GetFloat(name + "_xLRot", transform.localEulerAngles.x),
					PlayerPrefs.GetFloat(name + "_yLRot", transform.localEulerAngles.y),
					PlayerPrefs.GetFloat(name + "_zLRot", transform.localEulerAngles.z));
			}
			else if (coordinateSystem == Space.World) {
				transform.position = new Vector3(
					PlayerPrefs.GetFloat(name + "_xPos", transform.position.x),
					PlayerPrefs.GetFloat(name + "_yPos", transform.position.y),
					PlayerPrefs.GetFloat(name + "_zPos", transform.position.z));
				transform.eulerAngles = new Vector3(
					PlayerPrefs.GetFloat(name + "_xRot", transform.eulerAngles.x),
					PlayerPrefs.GetFloat(name + "_yRot", transform.eulerAngles.y),
					PlayerPrefs.GetFloat(name + "_zRot", transform.eulerAngles.z));
			}
		}

		public void Save() {
			if (coordinateSystem == Space.Self) {
				PlayerPrefs.SetFloat(name + "_xLPos", transform.localPosition.x);
				PlayerPrefs.SetFloat(name + "_yLPos", transform.localPosition.y);
				PlayerPrefs.SetFloat(name + "_zLPos", transform.localPosition.z);
				PlayerPrefs.SetFloat(name + "_xLRot", transform.localEulerAngles.x);
				PlayerPrefs.SetFloat(name + "_yLRot", transform.localEulerAngles.y);
				PlayerPrefs.SetFloat(name + "_zLRot", transform.localEulerAngles.z);
			}
			else if (coordinateSystem == Space.World) {
				PlayerPrefs.SetFloat(name + "_xPos", transform.position.x);
				PlayerPrefs.SetFloat(name + "_yPos", transform.position.y);
				PlayerPrefs.SetFloat(name + "_zPos", transform.position.z);
				PlayerPrefs.SetFloat(name + "_xRot", transform.eulerAngles.x);
				PlayerPrefs.SetFloat(name + "_yRot", transform.eulerAngles.y);
				PlayerPrefs.SetFloat(name + "_zRot", transform.eulerAngles.z);
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

			if (GUILayout.Button("Load from PlayerPrefs")) {
				sl.Load();
			}

		}
	}
#endif
}
