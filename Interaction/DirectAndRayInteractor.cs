using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;

namespace unityutilities {

	/// <summary>
	/// Interactor used for directly interacting with interactables that are touching.  This is handled via trigger volumes
	/// that update the current set of valid targets for this interactor.  This component must have a collision volume that is 
	/// set to be a trigger to work.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("unityutilities/Interaction/Direct and Ray Interactor")]
	public class DirectAndRayInteractor : XRRayInteractor {
		protected override List<XRBaseInteractable> ValidTargets { get { return m_ValidTargetsDirect; } }

		// reusable list of valid targets
		private List<XRBaseInteractable> m_ValidTargetsDirect = new List<XRBaseInteractable>();

		// reusable map of interactables to their distance squared from this interactor (used for sort)
		Dictionary<XRBaseInteractable, float> m_InteractableDistanceSqrMap = new Dictionary<XRBaseInteractable, float>();

		// Sort comparison function used by GetValidTargets
		Comparison<XRBaseInteractable> m_InteractableSortComparison;
		int InteractableSortComparison(XRBaseInteractable x, XRBaseInteractable y) {
			float xDistance = m_InteractableDistanceSqrMap[x];
			float yDistance = m_InteractableDistanceSqrMap[y];
			if (xDistance > yDistance)
				return 1;
			if (xDistance < yDistance)
				return -1;
			else
				return 0;
		}

		protected override void Awake() {
			base.Awake();

			m_InteractableSortComparison = InteractableSortComparison;
			if (!GetComponents<Collider>().Any(x => x.isTrigger))
				Debug.LogWarning("Direct Interactor does not have required Collider set as a trigger.");
		}

		protected void OnTriggerEnter(Collider col) {
			//var interactable = interactionManager.TryGetInteractableForCollider(col);
			// TODO get access to the above function instead of using line below.
			var interactable = col.attachedRigidbody ? col.attachedRigidbody.GetComponent<XRBaseInteractable>() : col.GetComponent<XRBaseInteractable>();
			if (interactable && !m_ValidTargetsDirect.Contains(interactable))
				m_ValidTargetsDirect.Add(interactable);
		}

		protected void OnTriggerExit(Collider col) {
			//var interactable = interactionManager.TryGetInteractableForCollider(col);
			// TODO get access to the above function instead of using line below.
			var interactable = col.attachedRigidbody ? col.attachedRigidbody.GetComponent<XRBaseInteractable>() : col.GetComponent<XRBaseInteractable>();
			if (interactable && m_ValidTargetsDirect.Contains(interactable))
				m_ValidTargetsDirect.Remove(interactable);
		}

		/// <summary>
		/// Retrieve the list of interactables that this interactor could possibly interact with this frame.
		/// This list is sorted by priority (in this case distance).
		/// </summary>
		/// <param name="validTargets">Populated List of interactables that are valid for selection or hover.</param>
		public override void GetValidTargets(List<XRBaseInteractable> validTargets) {
			validTargets.Clear();
			m_InteractableDistanceSqrMap.Clear();

			List<XRBaseInteractable> rayValidTargets = new List<XRBaseInteractable>();
			base.GetValidTargets(rayValidTargets);

			// Calculate distance squared to interactor's attach transform and add to validTargets (which is sorted before returning)
			foreach (var interactable in m_ValidTargetsDirect) {
				m_InteractableDistanceSqrMap[interactable] = interactable.GetDistanceSqrToInteractor(this);
				validTargets.Add(interactable);
			}

			// do the same for the targets from the ray interactor
			foreach (var interactable in rayValidTargets) {
				m_InteractableDistanceSqrMap[interactable] = interactable.GetDistanceSqrToInteractor(this);
				validTargets.Add(interactable);
			}

			validTargets.Sort(m_InteractableSortComparison);
		}

		/// <summary>Determines if the interactable is valid for hover this frame.</summary>
		/// <param name="interactable">Interactable to check.</param>
		/// <returns><c>true</c> if the interactable can be hovered over this frame.</returns>
		public override bool CanHover(XRBaseInteractable interactable) {
			return base.CanHover(interactable) && (selectTarget == null || selectTarget == interactable);
		}

		/// <summary>Determines if the interactable is valid for selection this frame.</summary>
		/// <param name="interactable">Interactable to check.</param>
		/// <returns><c>true</c> if the interactable can be selected this frame.</returns>
		public override bool CanSelect(XRBaseInteractable interactable) {
			return base.CanSelect(interactable) && (selectTarget == null || selectTarget == interactable);
		}
	}
}