using System;
using UnityEngine;

namespace VelUtils
{
	[AddComponentMenu("VelUtils/Don't Destroy On Load")]
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
