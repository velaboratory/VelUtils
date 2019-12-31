
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using unityutilities;

[RequireComponent(typeof(OvrAvatar))]
public class TouchMenuHandModule : MonoBehaviour {
	private OvrAvatar avatar;
	private OvrAvatarHand leftHand;
	private OvrAvatarHand rightHand;
	private TouchMenuFingerCollider leftCollider;
	private TouchMenuFingerCollider rightCollider;
	public GameObject colliderPrefab;
	private bool isSelectedByLeft;
	private Button selected;
	private TouchMenuFingerCollider lastFinger;
	private bool locked;
	private const float lockTime = .2f;
	private float lockTimer;

	// The distance behind the button when the finger is no longer considered touching the button
	float pushThroughDistance = .04f;

	// The distance behind the button at which to activate the press
	float activationDistance = .01f;

	// The distance above the button to come up before allowing another button press
	float resetDistance = .01f;

	private const float stayTime = .5f;
	private float stayTimer;

	private const float maxVel = 1f;


	// Start is called before the first frame update
	IEnumerator Start() {
		avatar = GetComponent<OvrAvatar>();
		while (avatar.HandLeft == null) {
			yield return null;
		}

		leftHand = avatar.HandLeft;
		rightHand = avatar.HandRight;

		leftCollider = Instantiate(colliderPrefab, leftHand.RenderParts[0].bones[7]).GetComponent<TouchMenuFingerCollider>();
		rightCollider = Instantiate(colliderPrefab, rightHand.RenderParts[0].bones[7]).GetComponent<TouchMenuFingerCollider>();
		leftCollider.handModule = this;
		leftCollider.isLeft = true;
		rightCollider.handModule = this;
		rightCollider.isLeft = false;
	}

	// Update is called once per frame
	void Update() {
		lockTimer += Time.deltaTime;

		if (selected) {
			return;
			float verticalDistance = selected.transform.InverseTransformPoint(
				isSelectedByLeft ? leftCollider.transform.position : rightCollider.transform.position).y;
			if (!locked && verticalDistance > activationDistance) {
				Invoke(nameof(ClickSelectedButton), .1f);
				locked = true;
				lockTimer = 0;
				lastFinger.audioSource.Play();
			}
			else if (verticalDistance < resetDistance && lockTimer > lockTime) {
				locked = false;
			}
			

			/*
			stayTimer += Time.deltaTime;
			if (!locked && stayTime > stayTimer) {
				Invoke(nameof(ClickSelectedButton), .1f);
				locked = true;
				lockTimer = 0;
				lastFinger.audioSource.Play();
			}*/
		}
	}

	void ClickSelectedButton() {
		selected?.onClick?.Invoke();
	}

	public void SelectableEnter(Button i, TouchMenuFingerCollider finger, Vector3 velocity) {
		if (lockTimer < lockTime) return;
		Debug.Log(velocity.magnitude);
		if (velocity.magnitude > maxVel) return;


		i.Select();
		selected = i;
		lastFinger = finger;

		lockTimer = 0;
		stayTimer = 0;
		lastFinger.audioSource.Play();
		Invoke(nameof(ClickSelectedButton), .1f);

		InputMan.Vibrate(finger.isLeft ? Side.Left : Side.Right, 1);
	}

	public void SelectableExit(Button i, TouchMenuFingerCollider finger) {
		Invoke(nameof(SetSelectedToNull), .2f);
	}

	void SetSelectedToNull() {
		selected = null;
	}
}
