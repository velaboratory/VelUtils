#undef OCULUS_UTILITIES_AVAILABLE

using UnityEngine;
using UnityEngine.UI;

public class TouchMenuFingerCollider : MonoBehaviour {
	public TouchMenuHandModule handModule;
	public bool isLeft;
	public AudioSource audioSource;

	private void OnTriggerEnter(Collider c) {
		Button s = c.GetComponent<Button>();
		if (s != null) {
			handModule.SelectableEnter(s, this);
		}
	}

	private void OnTriggerExit(Collider c) {
		Button s = c.GetComponent<Button>();
		if (s != null) {
			handModule.SelectableExit(s, this);
		}
	}
}