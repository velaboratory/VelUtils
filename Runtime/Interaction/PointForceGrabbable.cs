using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


namespace unityutilities {
	[SelectionBase]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody))]
	[AddComponentMenu("unityutilities/Interaction/Point Force Interactable")]
	public class PointForceGrabbable : XRBaseInteractable {

		const float k_VelocityPredictionFactor = 0.6f;
		const float k_AngularVelocityDamping = 0.95f;
		const int k_ThrowSmoothingFrameCount = 20;
		const float k_DefaultThrowSmoothingDuration = 0.25f;
		const float k_DefaultThrowVelocityScale = 1.5f;
		const float k_DefaultThrowAngularVelocityScale = 0.25f;

		[SerializeField]
		bool m_TrackPosition = true;
		/// <summary>Gets or sets whether this interactable should track the position of the interactor.</summary>
		public bool trackPosition { get { return m_TrackPosition; } set { m_TrackPosition = value; } }

		public float positionPredictionFactor = 1;

		[SerializeField]
		bool m_TrackRotation = true;
		/// <summary>Gets or sets whether this interactable should track the rotation of the interactor.</summary>
		public bool trackRotation { get { return m_TrackRotation; } set { m_TrackRotation = value; } }

		public bool rotationUsesForce = true;
		public float rotationStrength = 1f;
		public float rotationPredictionFactor = 1;


		[Header("Throwing")]

		[SerializeField]
		bool m_ThrowOnDetach = true;
		/// <summary>Gets or sets whether the object inherits the interactor's velocity when released.</summary>
		public bool throwOnDetach { get { return m_ThrowOnDetach; } set { m_ThrowOnDetach = value; } }

		[SerializeField]
		float m_ThrowSmoothingDuration = k_DefaultThrowSmoothingDuration;
		/// <summary>Gets or sets the time period to average thrown velocity over</summary>
		public float throwSmoothingDuration { get { return m_ThrowSmoothingDuration; } set { m_ThrowSmoothingDuration = value; } }

		[SerializeField]
		[Tooltip("The curve to use to weight velocity smoothing (most recent frames to the right.")]
		AnimationCurve m_ThrowSmoothingCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);

		[SerializeField]
		float m_ThrowVelocityScale = k_DefaultThrowVelocityScale;
		/// <summary>Gets or set the the velocity scale used when throwing.</summary>
		public float throwVelocityScale { get { return m_ThrowVelocityScale; } set { m_ThrowVelocityScale = value; } }

		[SerializeField]
		float m_ThrowAngularVelocityScale = k_DefaultThrowAngularVelocityScale;
		/// <summary>Gets or set the the angular velocity scale used when throwing.</summary>
		public float throwAngularVelocityScale { get { return m_ThrowAngularVelocityScale; } set { m_ThrowAngularVelocityScale = value; } }

		// RigidBody's previous settings
		bool m_WasKinematic;
		bool m_UsedGravity;
		float m_UsedDrag;
		float m_UsedAngularDrag;

		// Interactor
		XRBaseInteractor m_SelectingInteractor;

		// Point we are attaching to on this Interactable in local space
		Vector3 positionOffset;
		Quaternion rotationOffset;

		// Point we are moving towards each frame (eventually will be at Interactor's attach point)
		Vector3 goalPosition;
		Quaternion goalRotation;

		bool m_DetachInLateUpdate;
		Vector3 m_DetachVelocity;
		Vector3 m_DetachAngularVelocity;

		int m_ThrowSmoothingCurrentFrame;
		float[] m_ThrowSmoothingFrameTimes = new float[k_ThrowSmoothingFrameCount];
		Vector3[] m_ThrowSmoothingVelocityFrames = new Vector3[k_ThrowSmoothingFrameCount];
		Vector3[] m_ThrowSmoothingAngularVelocityFrames = new Vector3[k_ThrowSmoothingFrameCount];

		Rigidbody rb;
		Vector3 lastInteractorPosition;
		Quaternion lastInteractorRotation;

		Vector3 lastPointPos;
		Quaternion lastRotation;

		protected override void Awake() {
			base.Awake();

			if (rb == null)
				rb = GetComponent<Rigidbody>();
			if (rb == null)
				Debug.LogWarning("Grab Interactable does not have a required RigidBody.", this);
		}

		public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase) {
			switch (updatePhase) {
				//
				// during Fixed update we want to perform any physics based updates (eg: Kinematic or VelocityTracking)
				// the call to MoveToTarget will perform the correct the type of update depending on the update phase
				//
				case XRInteractionUpdateOrder.UpdatePhase.Fixed: {
						if (isSelected) {
							PerformPointForceTrackingUpdate(Time.unscaledDeltaTime, updatePhase);
						}
					}
					break;
				//
				// during Dynamic update we want to perform any GameObject based manipulation (eg: Instantaneous) 
				// the call to MoveToTarget will perform the correct the type of update depending on the update phase
				//
				case XRInteractionUpdateOrder.UpdatePhase.Dynamic: {
						if (isSelected) {
							UpdateTarget(Time.unscaledDeltaTime);
							SmoothVelocityUpdate();
						}
					}
					break;
				//
				// during OnBeforeUpdate we want to perform any last minute GameObject position changes before rendering. (eg: Instantaneous) 
				// the call to MoveToTarget will perform the correct the type of update depending on the update phase
				//
				case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender: {
						if (isSelected) {
							UpdateTarget(Time.unscaledDeltaTime);
						}
					}
					break;
				//
				// Late update is only used to handle detach as late as possible.
				//
				case XRInteractionUpdateOrder.UpdatePhase.Late: {
						if (m_DetachInLateUpdate) {
							if (!m_SelectingInteractor)
								Detach();
							m_DetachInLateUpdate = false;
						}
					}
					break;
			}
		}
		
		// Could do smoothing of goal position here
		void UpdateTarget(float timeDelta) {
			goalPosition = m_SelectingInteractor.attachTransform.position;
			goalRotation = m_SelectingInteractor.attachTransform.rotation * rotationOffset;
		}

		void PerformPointForceTrackingUpdate(float timeDelta, XRInteractionUpdateOrder.UpdatePhase updatePhase) {
			if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed) {
				// Do position tracking
				if (trackPosition) {
					// global position of the grabbing point on the object
					Vector3 pointPos = transform.TransformPoint(positionOffset);
					Vector3 lastPosDelta = lastPointPos != Vector3.zero ? pointPos - lastPointPos : Vector3.zero;
					Vector3 predictedPosDelta = goalPosition - (pointPos + positionPredictionFactor * lastPosDelta);
					Vector3 force = predictedPosDelta * timeDelta;

					// max "distance" the hand can be from the object
					force = Vector3.ClampMagnitude(force, .2f);

					rb.AddForceAtPosition(100000 * force, pointPos);

					lastPointPos = pointPos;
				}

				// Do angular velocity tracking
				if (trackRotation) {
					var rotationDelta = goalRotation * Quaternion.Inverse(rb.rotation);

					if (rotationUsesForce) {

						// how close we would get if we just continued on the current velocity
						Quaternion lastRotDelta = lastRotation != Quaternion.identity ? rb.rotation * Quaternion.Inverse(lastRotation): Quaternion.identity;
						lastRotDelta.ToAngleAxis(out float angle1, out var axis1);
						angle1 *= rotationPredictionFactor;
						var predictedRotDelta = goalRotation * Quaternion.Inverse(rb.rotation) * Quaternion.Inverse(Quaternion.AngleAxis(angle1, axis1));

						predictedRotDelta.ToAngleAxis(out float angle2, out Vector3 axis2);
						angle2 *= timeDelta * rotationStrength;
						//angle2 = Mathf.Clamp(angle2, -1000, 1000);
						Vector3 torque = axis2 * (angle2);

						//rotationDelta.ToAngleAxis(out float angle, out var axis);
						//Vector3 angularVel = axis * (angle * Mathf.Deg2Rad / timeDelta);
						//var angularTorq = angularVel * rotationStrength;
						rb.AddTorque(torque);

						lastRotation = rb.rotation;
					}
					else {
						// scale initialized velocity by prediction factor
						rb.angularVelocity *= k_VelocityPredictionFactor;
						rotationDelta.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
						if (angleInDegrees > 180)
							angleInDegrees -= 360;

						if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon) {
							var angularVelocity = (rotationAxis * angleInDegrees * Mathf.Deg2Rad) / timeDelta;
							if (!float.IsNaN(angularVelocity.x))
								rb.angularVelocity += angularVelocity * k_AngularVelocityDamping;
						}

					}



				}
			}
		}

		void Detach() {
			if (m_ThrowOnDetach) {
				rb.velocity = m_DetachVelocity;
				rb.angularVelocity = m_DetachAngularVelocity;
			}
		}

		// In order to move the Interactable to the Interactor we need to
		// calculate the Interactable attach point in the coordinate system of the
		// Interactor's attach point.
		void SetupGrabOffsets(XRBaseInteractor interactor) {
			var attachPosition = interactor.attachTransform.position;
			var localAttachOffset = transform.InverseTransformPoint(attachPosition);

			positionOffset = localAttachOffset;
			rotationOffset = Quaternion.Inverse(interactor.attachTransform.rotation) * rb.rotation;
		}

		/// <summary>This method is called by the interaction manager 
		/// when the interactor first initiates selection of an interactable.</summary>
		/// <param name="interactor">Interactor that is initiating the selection.</param>
		protected override void OnSelectEnter(XRBaseInteractor interactor) {
			if (!interactor)
				return;
			base.OnSelectEnter(interactor);

			m_SelectingInteractor = interactor;

			// remember RigidBody settings and setup to move
			m_WasKinematic = rb.isKinematic;
			m_UsedGravity = rb.useGravity;
			m_UsedDrag = rb.drag;
			m_UsedAngularDrag = rb.angularDrag;

			// forget if we have previous detach velocities
			m_DetachVelocity = m_DetachAngularVelocity = Vector3.zero;

			lastPointPos = Vector3.zero;

			SetupGrabOffsets(interactor);

			SmoothVelocityStart();
		}

		/// <summary>This method is called by the interaction manager 
		/// when the interactor ends selection of an interactable.</summary>
		/// <param name="interactor">Interactor that is ending the selection.</param>
		protected override void OnSelectExit(XRBaseInteractor interactor) {
			base.OnSelectExit(interactor);

			// reset RididBody settings
			rb.isKinematic = m_WasKinematic;
			rb.useGravity = m_UsedGravity;
			rb.drag = m_UsedDrag;
			rb.angularDrag = m_UsedAngularDrag;
			rb.maxAngularVelocity = 1000f;

			m_SelectingInteractor = null;
			m_DetachInLateUpdate = true;
			SmoothVelocityEnd();
		}

		/// <summary>
		/// Determines if this interactable can be hovered by a given interactor.
		/// </summary>
		/// <param name="interactor">Interactor to check for a valid hover state with.</param>
		/// <returns>True if hovering is valid this frame, False if not.</returns>
		public override bool IsHoverableBy(XRBaseInteractor interactor) {
			return true;
		}

		/// <summary>
		/// Determines if this interactable can be selected by a given interactor.
		/// </summary>
		/// <param name="interactor">Interactor to check for a valid selection with.</param>
		/// <returns>True if selection is valid this frame, False if not.</returns>
		public override bool IsSelectableBy(XRBaseInteractor interactor) {
			return !m_SelectingInteractor || m_SelectingInteractor == interactor || !m_SelectingInteractor.isSelectExclusive;
		}

		#region Velocity Smoothing
		void SmoothVelocityStart() {
			if (!m_SelectingInteractor)
				return;
			lastInteractorPosition = m_SelectingInteractor.attachTransform.position;
			lastInteractorRotation = m_SelectingInteractor.attachTransform.rotation;
			Array.Clear(m_ThrowSmoothingFrameTimes, 0, m_ThrowSmoothingFrameTimes.Length);
			Array.Clear(m_ThrowSmoothingVelocityFrames, 0, m_ThrowSmoothingVelocityFrames.Length);
			Array.Clear(m_ThrowSmoothingAngularVelocityFrames, 0, m_ThrowSmoothingAngularVelocityFrames.Length);
			m_ThrowSmoothingCurrentFrame = 0;
		}

		void SmoothVelocityEnd() {
			if (m_ThrowOnDetach) {
				Vector3 smoothedVelocity = getSmoothedVelocityValue(m_ThrowSmoothingVelocityFrames);
				Vector3 smoothedAngularVelocity = getSmoothedVelocityValue(m_ThrowSmoothingAngularVelocityFrames);
				m_DetachVelocity = smoothedVelocity * m_ThrowVelocityScale;
				m_DetachAngularVelocity = smoothedAngularVelocity * m_ThrowAngularVelocityScale;
			}
		}

		void SmoothVelocityUpdate() {
			if (!m_SelectingInteractor)
				return;
			m_ThrowSmoothingFrameTimes[m_ThrowSmoothingCurrentFrame] = Time.time;
			m_ThrowSmoothingVelocityFrames[m_ThrowSmoothingCurrentFrame] = (m_SelectingInteractor.attachTransform.position - lastInteractorPosition) / Time.deltaTime;

			Quaternion VelocityDiff = (m_SelectingInteractor.attachTransform.rotation * Quaternion.Inverse(lastInteractorRotation));
			m_ThrowSmoothingAngularVelocityFrames[m_ThrowSmoothingCurrentFrame] = (new Vector3(Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.x), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.y), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.z))
				/ Time.deltaTime) * Mathf.Deg2Rad;

			m_ThrowSmoothingCurrentFrame = (m_ThrowSmoothingCurrentFrame + 1) % k_ThrowSmoothingFrameCount;
			lastInteractorPosition = m_SelectingInteractor.attachTransform.position;
			lastInteractorRotation = m_SelectingInteractor.attachTransform.rotation;
		}

		Vector3 getSmoothedVelocityValue(Vector3[] velocityFrames) {
			Vector3 calcVelocity = new Vector3();
			int frameCounter = 0;
			float totalWeights = 0.0f;
			for (; frameCounter < k_ThrowSmoothingFrameCount; frameCounter++) {
				int frameIdx = (((m_ThrowSmoothingCurrentFrame - frameCounter - 1) % k_ThrowSmoothingFrameCount) + k_ThrowSmoothingFrameCount) % k_ThrowSmoothingFrameCount;
				if (m_ThrowSmoothingFrameTimes[frameIdx] == 0.0f)
					break;

				float timeAlpha = (Time.time - m_ThrowSmoothingFrameTimes[frameIdx]) / m_ThrowSmoothingDuration;
				float velocityWeight = m_ThrowSmoothingCurve.Evaluate(Mathf.Clamp(1.0f - timeAlpha, 0.0f, 1.0f));
				calcVelocity += velocityFrames[frameIdx] * velocityWeight;
				totalWeights += velocityWeight;
				if (Time.time - m_ThrowSmoothingFrameTimes[frameIdx] > m_ThrowSmoothingDuration)
					break;
			}
			if (totalWeights > 0.0f)
				return calcVelocity / totalWeights;
			else
				return Vector3.zero;
		}
		#endregion
	}
}