using System;
using System.Collections.Generic;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	/// <summary>
	/// Handles grab and release of VRGrabbable objects. Needs to be able to receive OnTriggerEnter events.
	/// </summary>
	public class VRGrabbableHand : MonoBehaviour
	{
		public Rig rig;
		public Side side;

		[Tooltip("Can be set to none to use external input sources by using actions.")]
		public VRInput grabInput = VRInput.Trigger;

		public bool vibrateOnGrab = true;

		/// <summary>
		/// Call this from somewhere to try to grab anything that is being hovered.
		/// Allows for remote sources if input, such as for tracked hands.
		/// </summary>
		public Action GrabInput;
		/// <summary>
		/// Call this from somewhere to try to release the currently grabbed object.
		/// Allows for remote sources if input, such as for tracked hands.
		/// </summary>
		public Action ReleaseInput;

		[Header("Debug")]
		[ReadOnly]
		public VRGrabbable grabbedVRGrabbable;
		[ReadOnly]
		public VRGrabbable selectedVRGrabbable;
		[ReadOnly]
		public List<VRGrabbable> touchedObjs = new List<VRGrabbable>();

		[NonSerialized]
		public Vector3[] lastVels = new Vector3[5];
		int lastVelsIndex;

		private void Awake()
		{
			GrabInput += HandleGrabInput;
			ReleaseInput += HandleReleaseInput;
		}

		private void Update()
		{
			// Grab ✊
			if (InputMan.GetDown(grabInput, side))
			{
				GrabInput();
			}
			// Release 🤚
			else if (InputMan.GetUp(grabInput, side))
			{
				ReleaseInput();
			}

			// Highlight 🖊
			if (grabbedVRGrabbable)
			{
				if (selectedVRGrabbable)
				{
					selectedVRGrabbable.HandleDeselection();
					selectedVRGrabbable = null;
				}
			}
			else
			{
				var best = GetBestGrabbable();
				if (selectedVRGrabbable != best)
				{
					if (selectedVRGrabbable)
						selectedVRGrabbable.HandleDeselection();
					selectedVRGrabbable = best;
					selectedVRGrabbable.HandleSelection();
				}
			}

			// update the last velocities ➡➡
			lastVels[++lastVelsIndex % lastVels.Length] = rig.transform.TransformVector(InputMan.ControllerVelocity(side));
		}

		private void HandleReleaseInput()
		{
			if (grabbedVRGrabbable)
			{
				grabbedVRGrabbable.HandleRelease(this);
				if (touchedObjs.Contains(grabbedVRGrabbable))
				{
					selectedVRGrabbable = grabbedVRGrabbable;
					selectedVRGrabbable.HandleSelection();
				}
			}

			grabbedVRGrabbable = null;
		}

		private void HandleGrabInput()
		{
			if (!grabbedVRGrabbable)
			{
				VRGrabbable best = GetBestGrabbable();
				if (best != null)
				{
					ManualGrab(best);
				}
			}
		}

		public void ManualGrab(VRGrabbable grabbable)
		{
			grabbable.HandleDeselection();
			grabbable.HandleGrab(this);
			grabbedVRGrabbable = grabbable;
			if (vibrateOnGrab)
			{
				InputMan.Vibrate(side, 1, .5f);
			}
		}

		public void ManualRelease(VRGrabbable grabbable)
		{
			HandleReleaseInput();
		}

		/// <summary>
		/// Finds the VRGrabbable obj being collided with that has the highest priority (or some other algorithm)
		/// </summary>
		/// <returns>The VRGrabbable</returns>
		private VRGrabbable GetBestGrabbable()
		{
			// Cancel if not touching anything
			if (touchedObjs.Count <= 0) return null;

			// remove any null objects 😊👌
			touchedObjs.RemoveAll(item => item == null);

			// Sort the list of grabbables by priority, then distance
			touchedObjs.Sort((a, b) =>
			{
				if (a.priority != b.priority)
					return b.priority.CompareTo(a.priority);
				else
					return Vector3.Distance(a.transform.position, transform.position)
						.CompareTo(Vector3.Distance(b.transform.position, transform.position));
			});

			// return the first element on the list
			return touchedObjs[0];
		}

		private void OnTriggerEnter(Collider other)
		{
			// Find VRGrabbable
			VRGrabbable vrGrabbable;
			if (other.attachedRigidbody)
				vrGrabbable = other.attachedRigidbody.GetComponent<VRGrabbable>();
			else
				vrGrabbable = other.GetComponent<VRGrabbable>();

			// Select the object
			if (vrGrabbable)
			{
				if (!touchedObjs.Contains(vrGrabbable))
				{
					touchedObjs.Add(vrGrabbable);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			// Find VRGrabbable
			VRGrabbable vrGrabbable;
			if (other.attachedRigidbody)
				vrGrabbable = other.attachedRigidbody.GetComponent<VRGrabbable>();
			else
				vrGrabbable = other.GetComponent<VRGrabbable>();

			// Deselect the object
			if (vrGrabbable)
			{
				touchedObjs.Remove(vrGrabbable);
				if (vrGrabbable == selectedVRGrabbable)
				{
					selectedVRGrabbable.HandleDeselection();
					selectedVRGrabbable = null;
				}
			}
		}
	}
}