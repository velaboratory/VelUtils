using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	private float lockTime = .5f;
	private float lockTimer;

	// The distance behind the button when the finger is no longer considered touching the button
	float pushThroughDistance = .04f;

	// The distance behind the button at which to activate the press
	float activationDistance = .01f;

	// The distance above the button to come up before allowing another button press
	float resetDistance = .01f;


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
		leftCollider.isLeft = false;
	}

	// Update is called once per frame
	void Update() {
		lockTimer += Time.deltaTime;
		return;
		if (selected) {
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
		}
		
	}

	void ClickSelectedButton() {
		selected.onClick.Invoke();
	}

	public void SelectableEnter(Button i, TouchMenuFingerCollider finger) {
		i.Select();
		selected = i;
		lastFinger = finger;
		
		if (!(lockTimer > lockTime)) return;
		i.onClick.Invoke();
		lockTimer = 0;
		lastFinger.audioSource.Play();
	}

	public void SelectableExit(Button i, TouchMenuFingerCollider finger) {
		Invoke(nameof(SetSelectedToNull), .2f);
	}

	void SetSelectedToNull() {
		selected = null;
	}
}