using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace unityutilities {

	[AddComponentMenu("unityutilities/Interaction/Interactable Canvas")]
	public class TrackedDeviceGraphicRaycasterThatWorksWithSceneChanges : TrackedDeviceGraphicRaycaster {
		private Canvas canvas;
		public bool isDontDestroyOnLoad;

		protected override void Start() {
			base.Start();
			//DontDestroyOnLoad(gameObject);
			canvas = GetComponent<Canvas>();
			canvas.worldCamera = Camera.main;

			//SceneManager.sceneLoaded += OnSceneChange;
		}

		private void OnSceneChange(Scene scene, LoadSceneMode mode) {
			if (!isDontDestroyOnLoad) {
				Destroy(gameObject);
			}
		}
	}
}
