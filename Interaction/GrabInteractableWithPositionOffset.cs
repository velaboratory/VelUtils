using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace unityutilities {

	[AddComponentMenu("unityutilities/Interaction/Grab Interactable with Position Offset")]
	public class GrabInteractableWithPositionOffset : XRGrabInteractable {
		private bool initialAttachTransformSet;
		private Transform initialAttachTransform;
		private bool grabbedRemotely;

		[Tooltip("How far the hand has to be from the object before \"Remote Grabbing\" mode kicks in")]
		public float distanceOffsetForRemoteGrab = .05f;

		[Tooltip("Overrides the rigidbody maxAngularVelocity if set to something other than 0.")]
		public float maxAngularVelocity;

		protected override void Awake() {
			base.Awake();
			if (attachTransform != null) {
				initialAttachTransform = attachTransform;
				initialAttachTransformSet = true;
			}
		}

		private void Start() {
			if (maxAngularVelocity!= 0) {
				GetComponent<Rigidbody>().maxAngularVelocity = maxAngularVelocity;
			}
		}

		protected override void OnSelectEnter(XRBaseInteractor interactor) {
			if (!initialAttachTransformSet) {
				Vector3 interactorPos = interactor.attachTransform.position;
				bool isWithin = colliders.TrueForAll(c => (c.ClosestPoint(interactorPos) - interactorPos).sqrMagnitude < distanceOffsetForRemoteGrab + Mathf.Epsilon);
				if (isWithin) {
					Transform t = new GameObject("Temporary attach point").transform;
					t.SetParent(transform);
					t.position = interactorPos;
					t.rotation = interactor.attachTransform.rotation;
					attachTransform = t;
					grabbedRemotely = false;
				}
				else {
					grabbedRemotely = true;
				}
			}
			base.OnSelectEnter(interactor);
		}

		protected override void OnSelectExit(XRBaseInteractor interactor) {
			base.OnSelectExit(interactor);
			if (!initialAttachTransformSet && !grabbedRemotely) {
				Destroy(attachTransform.gameObject);
				attachTransform = transform;
			}
		}
	}
}