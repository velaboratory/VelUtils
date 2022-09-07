using System;
using UnityEngine;

namespace unityutilities
{
	[AddComponentMenu("unityutilities/Don't Destroy On Load")]
	public class DontDestroyOnLoad : MonoBehaviour {
		private void Awake() {
			try {
				DontDestroyOnLoad(gameObject);
			} catch (Exception) {
				Debug.LogError("Couldn't dontdestroyonload object", gameObject);
			}
		}
	}
}
