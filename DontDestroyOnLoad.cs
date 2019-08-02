using System;
using UnityEngine;
public class DontDestroyOnLoad : MonoBehaviour {
	void Start() {
		try {
			DontDestroyOnLoad(gameObject);
		} catch (Exception e) {
			Debug.LogError("Couldn't dontdestroyonload object", gameObject);
		}
	}
}
