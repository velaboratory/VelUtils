using System;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour {
	private void Awake() {
		try {
			DontDestroyOnLoad(gameObject);
		} catch (Exception) {
			Debug.LogError("Couldn't dontdestroyonload object", gameObject);
		}
	}
}
