#define OCULUS_UTILITIES_AVAILABLE

using UnityEngine;
using UnityEngine.UI;
using unityutilities;

public class TouchMenuFingerCollider : MonoBehaviour {
	public TouchMenuHandModule handModule;
	public bool isLeft;
	public AudioSource audioSource;

	private Vector3[] lastPos = new Vector3[2];
	private int lastPosIndex;
	private bool fingerEnabled = true;


	private void Start() {

		MenuTablet.instance.ShowTabletEvent += DisableCollider;
		MenuTablet.instance.HideTabletEvent += EnableCollider;
	}

	private void FixedUpdate() {
		lastPos[lastPosIndex++ % lastPos.Length] = transform.position;
	}

	private void DisableCollider(Side side) {
		if (side == Side.Left && isLeft) {
			fingerEnabled = false;
		} else if (side == Side.Right && !isLeft) {
			fingerEnabled = false;
		}
		Debug.Log("" + side + " finger disabled");
	}

	private void EnableCollider(Side side) {
		fingerEnabled = true;
		Debug.Log("fingers enabled");
	}

	private void OnTriggerEnter(Collider c) {
		if (!fingerEnabled) 
			return;
		Button s = c.GetComponent<Button>();
		if (s != null) {
			handModule.SelectableEnter(s, this, (lastPos[lastPosIndex % lastPos.Length] - transform.position)/Time.fixedDeltaTime);
		}
	}

	private void OnTriggerExit(Collider c) {
		if (!fingerEnabled) return;
		Button s = c.GetComponent<Button>();
		if (s != null) {
			handModule.SelectableExit(s, this);
		}
	}
}