using UnityEngine;
using UnityEngine.SceneManagement;
using VelUtils.Ar;

namespace VelUtils
{
	public class SaveAlignmentPlayerPrefs : MonoBehaviour
	{
		public ArAlignment arAlignment;

		private void Start()
		{
			SceneManager.sceneLoaded += (_, _) => { Load(); };
			arAlignment.OnManualAlignmentComplete += HandleManualAlignmentComplete;
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				Load();
			}
		}

		private void HandleManualAlignmentComplete(Vector3 point1, Vector3 point2)
		{
			// Convert the points to local rig space
			Vector3 p1 = arAlignment.cameraRig.InverseTransformPoint(point1);
			Vector3 p2 = arAlignment.cameraRig.InverseTransformPoint(point2);

			PlayerPrefsJson.SetVector3("ArAlignmentPoint1", p1);
			PlayerPrefsJson.SetVector3("ArAlignmentPoint2", p2);
			arAlignment.Log("Saved alignment points to playerprefs. " + point1 + " " + point2);
		}

		public void Load()
		{
			if (arAlignment != null && FindObjectOfType<ArAlignmentOrigin>())
			{
				Vector3 localPoint1 = PlayerPrefsJson.GetVector3("ArAlignmentPoint1", Vector3.zero);
				Vector3 localPoint2 = PlayerPrefsJson.GetVector3("ArAlignmentPoint2", Vector3.zero);

				Vector3 point1 = arAlignment.cameraRig.TransformPoint(localPoint1);
				Vector3 point2 = arAlignment.cameraRig.TransformPoint(localPoint2);

				if (localPoint1 != Vector3.zero)
				{
					arAlignment.SpawnPoint(0, point1);
					arAlignment.SpawnPoint(1, point2);
					arAlignment.Align(point1, point2, false);
					arAlignment.Log("Loaded alignment points from playerprefs. " + point1 + " " + point2);
				}
			}
		}
	}
}