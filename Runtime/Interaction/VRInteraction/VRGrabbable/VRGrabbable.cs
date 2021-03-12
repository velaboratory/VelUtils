using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace unityutilities.VRInteraction
{
	public abstract class VRGrabbable : MonoBehaviour, INetworkPack
	{
		[HideInInspector]
		public List<VRGrabbableHand> listOfGrabbedByHands = new List<VRGrabbableHand>();

		[HideInInspector]
		// Assumes only one hand can grab this object. Consider using something else.
		public VRGrabbableHand GrabbedBy {
			get {
				if (listOfGrabbedByHands.Count > 0) return listOfGrabbedByHands[0];
				else return null;
			}
		}

		[NonSerialized]
		public bool includeInSave = true;

		/// <summary>
		/// Whether multiple hands can grab the same object
		/// </summary>
		protected bool canBeGrabbedByMultipleHands;

		[Tooltip("The importance of this object compared to others when grabbing overlapping objects.\n\nHigher numbers have higher priority.")]
		public int priority;

		[Tooltip("Whether this object can be grabbed from far away.")]
		public bool remoteGrabbable = false;

		public bool colorChange = true;

		[Tooltip("The color to tint the meshes assigned below.")]
		public Color highlightColor = Color.white;

		public Action Grabbed;
		public Action Released;

		[Tooltip("The meshes will be tinted the highlight color and will be enabled when hovering.")]
		[FormerlySerializedAs("meshes")]
		public Renderer[] highlightMeshes = new Renderer[0];

		[Tooltip("The objects will be enabled or disabled when hovering.")]
		public Transform[] highlightObjs = new Transform[0];

		private List<Color> origColors = new List<Color>();
		private List<bool> origVisibility = new List<bool>();

		/// <summary>
		/// Network feature. Is this object owned by this player?
		/// </summary>
		public bool locallyOwned = true;
		/// <summary>
		/// Network feature. Is this object being held by another player?
		/// </summary>
		public bool networkGrabbed = false;

		protected void Awake()
		{
			#region Set up highlighting 🖊
			if (highlightMeshes.Length == 0 && highlightObjs.Length == 0 && GetComponent<Renderer>())
			{
				highlightMeshes = new Renderer[] { GetComponent<Renderer>() };
			}

			if (highlightMeshes.Length > 0)
			{
				foreach (var mesh in highlightMeshes)
				{
					if (colorChange)
					{
						foreach (Material m in mesh.materials)
						{
							if (m.HasProperty("_Color"))
							{
								origColors.Add(m.color);
							}
							else if (m.HasProperty("_MainColor"))
							{
								origColors.Add(m.color);
							}
							else
							{
								Debug.LogError("Material doesn't have a color", mesh.gameObject);
								colorChange = false;
							}
						}
					}
					origVisibility.Add(mesh.enabled);
				}
			}
			#endregion
		}

		public virtual void HandleGrab(VRGrabbableHand h)
		{
			if (!canBeGrabbedByMultipleHands && listOfGrabbedByHands.Count > 0)
			{
				if (listOfGrabbedByHands.Count > 1)
				{
					Debug.LogError("wat");
				}
				// ask the old hand to release it
				GrabbedBy.Release();
			}
			listOfGrabbedByHands.Add(h);

			Grabbed?.Invoke();
		}

		public virtual void HandleRelease(VRGrabbableHand h = null)
		{
			if (h != null)
			{
				listOfGrabbedByHands.Remove(h);
			}
			else
			{
				if (listOfGrabbedByHands.Count > 1)
				{
					Debug.LogError("Called release without specifying hand, but there are multiple hands grabbing.", this);
				}
				else if (listOfGrabbedByHands.Count > 0)
				{
					listOfGrabbedByHands.RemoveAt(0);
				}
				else
				{
					Debug.LogError("Called release but no hands are grabbing", this);
				}
			}

			Released?.Invoke();
		}

		public void HandleSelection()
		{

			if (highlightMeshes.Length > 0)
			{
				for (int i = 0; i < highlightMeshes.Length; i++)
				{
					if (colorChange)
					{
						foreach (Material m in highlightMeshes[i].materials)
						{
							m.color = highlightColor;
						}
					}
					highlightMeshes[i].enabled = true;
				}
			}
			if (highlightObjs.Length > 0)
			{
				for (int i = 0; i < highlightObjs.Length; i++)
				{
					highlightObjs[i].gameObject.SetActive(true);
				}
			}
		}

		public void HandleDeselection()
		{

			if (highlightMeshes.Length > 0)
			{
				int k = 0;
				for (int i = 0; i < highlightMeshes.Length; i++)
				{
					if (colorChange)
					{
						foreach (Material m in highlightMeshes[i].materials)
						{
							m.color = origColors[k++];
						}
					}
					highlightMeshes[i].enabled = origVisibility[i];
				}
			}
			if (highlightObjs.Length > 0)
			{
				for (int i = 0; i < highlightObjs.Length; i++)
				{
					highlightObjs[i].gameObject.SetActive(false);
				}
			}
		}

		public abstract byte[] PackData();

		public abstract void UnpackData(byte[] data);
	}
}