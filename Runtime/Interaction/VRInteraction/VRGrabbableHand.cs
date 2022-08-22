using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	/// <summary>
	/// ✋ Handles grab and release of VRGrabbable objects. Needs to be able to receive OnTriggerEnter events.
	/// </summary>
	public class VRGrabbableHand : MonoBehaviour
	{
		public Rig rig;
		public Side side;

		[Tooltip("Turning this to false prevents this hand from grabbing objects itself.")]
		public bool canGrab = true;

		[Tooltip("Can be set to none to use external input sources by using actions.")]
		public VRInput grabInput = VRInput.Grip;

		[Tooltip("The normal grab input also needs to be held.")]
		public VRInput distanceGrabInput = VRInput.Trigger;

		public bool vibrateOnGrab = true;

		[Tooltip("Grabs the first thing that is touched after after holding the grab button.")]
		public bool autograb = true;

		[Tooltip("After holding grab for this long, autograb turns off.")]
		public float autograbTimeout = 0;

		private float autograbTimer = 0;

		public bool enableRemoteGrabbing = true;
		public float remoteGrabbingDistance = 5f;

		[Tooltip("Which layers to interact with")]
		public LayerMask layerMask = ~0;

		[Header("Debug")] [ReadOnly] public VRGrabbable grabbedVRGrabbable;

		/// <summary>
		/// The current best grabbable object. The one that's highlighted.
		/// </summary>
		[ReadOnly] public VRGrabbable selectedVRGrabbable;

		[ReadOnly] public List<VRGrabbable> touchedObjs = new List<VRGrabbable>();
		[ReadOnly] public List<VRGrabbable> remoteTouchedObjs = new List<VRGrabbable>();

		[ReadOnly] public List<VRGrabbable> raycastedObjs = new List<VRGrabbable>();


		public Queue<Vector3> lastVels = new Queue<Vector3>();
		private int lastVelsLength = 5;

		public Action<VRGrabbable> GrabEvent;
		public Action<VRGrabbable> ReleaseEvent;

		private void Update()
		{
			if (canGrab)
			{
				// Grab ✊
				if (InputMan.GetDown(grabInput, side))
				{
					Grab();
				}
				// Release 🤚
				else if (InputMan.GetUp(grabInput, side))
				{
					Release();
				}
				// autograb				
				else if (autograb && (autograbTimeout == 0 || autograbTimer < autograbTimeout) && InputMan.Get(grabInput, side))
				{
					Grab();
				}

				if (InputMan.Get(grabInput, side) && InputMan.GetDown(distanceGrabInput, side))
				{
					Grab(true);
				}
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
				VRGrabbable best = GetBestGrabbable(InputMan.Get(grabInput, side));
				if (selectedVRGrabbable != best)
				{
					if (selectedVRGrabbable)
						selectedVRGrabbable.HandleDeselection();

					selectedVRGrabbable = best;
					if (best != null)
					{
						selectedVRGrabbable.HandleSelection();
					}
				}
			}


			// Add remote objects to the touched list
			RaycastHit[] hitList;
			if (enableRemoteGrabbing)
			{
				hitList = Util.ConeCastAll(transform.position, transform.forward, 1.5f, remoteGrabbingDistance, layerMask);
			}
			else
			{
				hitList = Array.Empty<RaycastHit>();
			}

			List<VRGrabbable> newRaycastedObjs = new List<VRGrabbable>();
			foreach (RaycastHit hit in hitList)
			{
				Collider other = hit.collider;

				// Find VRGrabbable
				VRGrabbable vrGrabbable;
				if (other.attachedRigidbody)
					vrGrabbable = other.attachedRigidbody.GetComponent<VRGrabbable>();
				else
					vrGrabbable = other.GetComponent<VRGrabbable>();


				// Select the object
				if (vrGrabbable && vrGrabbable.remoteGrabbable)
				{
					newRaycastedObjs.Add(vrGrabbable);

					if (!remoteTouchedObjs.Contains(vrGrabbable))
					{
						remoteTouchedObjs.Add(vrGrabbable);
					}
				}
			}

			// stop selecting the previous objs that aren't currently raycasted at
			foreach (VRGrabbable obj in raycastedObjs)
			{
				if (!newRaycastedObjs.Contains(obj))
				{
					remoteTouchedObjs.Remove(obj);
					if (obj == selectedVRGrabbable)
					{
						selectedVRGrabbable.HandleDeselection();
						selectedVRGrabbable = null;
					}
				}
			}

			raycastedObjs = newRaycastedObjs;

			// update the last velocities ➡➡
			if (rig)
			{
				lastVels.Enqueue(rig.transform.TransformVector(InputMan.ControllerVelocity(side)));
				if (lastVels.Count > lastVelsLength) lastVels.Dequeue();
			}

			autograbTimer += Time.deltaTime;
		}

		/// <summary>
		/// Call this from somewhere to try to grab anything that is being hovered.
		/// Allows for remote sources if input, such as for tracked hands.
		/// </summary>
		public void Grab(bool includeRemote = false)
		{
			if (!grabbedVRGrabbable)
			{
				VRGrabbable best = GetBestGrabbable(includeRemote);
				if (best != null)
				{
					Grab(best);
				}
			}
		}

		/// <summary>
		/// Call this from somewhere to try to release the currently grabbed object.
		/// Allows for remote sources if input, such as for tracked hands.
		/// </summary>
		public void Release()
		{
			if (grabbedVRGrabbable)
			{
				grabbedVRGrabbable.HandleRelease(this);
				if (touchedObjs.Contains(grabbedVRGrabbable) || remoteTouchedObjs.Contains(grabbedVRGrabbable))
				{
					selectedVRGrabbable = grabbedVRGrabbable;
					selectedVRGrabbable.HandleSelection();
				}
			}

			ReleaseEvent?.Invoke(grabbedVRGrabbable);

			grabbedVRGrabbable = null;
		}


		/// <summary>
		/// Call this from somewhere to grab the passed-in object.
		/// Allows for manually grabbing an object, such as for a spawner.
		/// </summary>
		/// <param name="grabbable">The object to be grabbed</param>
		public void Grab(VRGrabbable grabbable)
		{
			autograbTimer = 0;
			grabbable.HandleDeselection();
			grabbable.HandleGrab(this);
			grabbedVRGrabbable = grabbable;
			if (vibrateOnGrab)
			{
				InputMan.Vibrate(side, 1f, .5f);
			}

			GrabEvent?.Invoke(grabbable);
		}

		/// <summary>
		/// Finds the VRGrabbable obj being collided with that has the highest priority (or some other algorithm)
		/// </summary>
		/// <returns>The VRGrabbable</returns>
		private VRGrabbable GetBestGrabbable(bool includeRemote = false)
		{
			// remove any null objects 😊👌
			touchedObjs.RemoveAll(item => item == null);
			remoteTouchedObjs.RemoveAll(item => item == null);

			// Cancel if not allowed to grab
			if (!canGrab) return null;

			// Cancel if not touching anything
			if (includeRemote)
			{
				if (touchedObjs.Count <= 0 && remoteTouchedObjs.Count <= 0) return null;
			}
			else
			{
				if (touchedObjs.Count <= 0) return null;
			}


			if (touchedObjs.Count > 0)
			{
				// Sort the list of grabbables by priority, then distance
				touchedObjs.Sort((a, b) =>
				{
					if (a.priority != b.priority)
						return b.priority.CompareTo(a.priority);
					else
					{
						// combine both distance and angular distance from the center for remote grabbing
						float dist = Vector3.Distance(a.transform.position, transform.position)
							.CompareTo(Vector3.Distance(b.transform.position, transform.position));
						float angle = Vector3.Angle(transform.forward, a.transform.position - transform.position)
							.CompareTo(Vector3.Angle(transform.forward, b.transform.position - transform.position));
						return (int)(0 * dist + angle);
					}
				});
				// return the first element on the list
				return touchedObjs[0];
			}

			if (includeRemote)
			{
				// Sort the list of grabbables by priority, then distance
				remoteTouchedObjs.Sort((a, b) =>
				{
					if (a.priority != b.priority)
						return b.priority.CompareTo(a.priority);
					else
					{
						// combine both distance and angular distance from the center for remote grabbing
						float dist = Vector3.Distance(a.transform.position, transform.position)
							.CompareTo(Vector3.Distance(b.transform.position, transform.position));
						float angle = Vector3.Angle(transform.forward, a.transform.position - transform.position)
							.CompareTo(Vector3.Angle(transform.forward, b.transform.position - transform.position));
						return (int)(0 * dist + angle);
					}
				});
				// return the first element on the list
				return remoteTouchedObjs[0];
			}

			return null;
		}

		private void OnTriggerStay(Collider other)
		{
			// ignore if not included in the layermask
			if (layerMask != (layerMask | 1 << other.gameObject.layer)) return;

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
			// ignore if not included in the layermask
			if (layerMask != (layerMask | 1 << other.gameObject.layer)) return;

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