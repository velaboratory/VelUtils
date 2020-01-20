using System;
using System.Collections.Generic;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public abstract class VRGrabbable : MonoBehaviour, INetworkPack
	{

		public bool colorChange = true;

		[HideInInspector]
		public List<VRGrabbableHand> listOfGrabbedByHands = new List<VRGrabbableHand>();

		[HideInInspector]
		public Transform grabbedBy;

		[Tooltip("The importance of this object compared to others when grabbing overlapping objects. Higher numbers have higher priority.")]
		public int priority;

		[Tooltip("The color to tint the meshes assigned below.")]
		public Color highlightColor = Color.white;

		public Action Grabbed;
		public Action Released;

		[Tooltip("The meshes will be tinted the highlight color and will be enabled when hovering.")]
		public Renderer[] meshes = new Renderer[0];

		[Tooltip("The objects will be enabled or disabled when hovering.")]
		public Transform[] highlightObjs = new Transform[0];

		private List<Color> origColors = new List<Color>();
		private List<bool> origVisibility = new List<bool>();

		public bool locallyOwned = true;

		private void Awake()
		{
			if (meshes.Length == 0 && highlightObjs.Length == 0 && GetComponent<Renderer>())
			{
				meshes = new Renderer[] { GetComponent<Renderer>() };
			}

			if (meshes.Length > 0)
			{
				for (int i = 0; i < meshes.Length; i++)
				{
					foreach (Material m in meshes[i].materials)
					{
						if (m.HasProperty("_Color"))
						{
							origColors.Add(m.color);
						}
						else
						{
							Debug.LogError("Material doesn't have a color", meshes[i].gameObject);
						}
					}
					origVisibility.Add(meshes[i].enabled);
				}
			}

		}

		public virtual void HandleGrab(VRGrabbableHand h)
		{

			grabbedBy = h.transform;
			listOfGrabbedByHands.Add(h);

		}

		public virtual int HandleRelease(VRGrabbableHand h = null)
		{

			grabbedBy = null;
			int index = 0;
			if (h != null)
			{
				index = listOfGrabbedByHands.IndexOf(h);
				listOfGrabbedByHands.Remove(h);

			}
			else
			{
				listOfGrabbedByHands.RemoveAt(listOfGrabbedByHands.Count - 1);
				index = listOfGrabbedByHands.Count - 1;
			}
			return index;
		}

		public void HandleSelection()
		{

			if (meshes.Length > 0)
			{
				for (int i = 0; i < meshes.Length; i++)
				{
					if (colorChange)
					{
						foreach (Material m in meshes[i].materials)
						{
							m.color = highlightColor;
						}
					}
					meshes[i].enabled = true;
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

			if (meshes.Length > 0)
			{
				int k = 0;
				for (int i = 0; i < meshes.Length; i++)
				{
					if (colorChange)
					{
						foreach (Material m in meshes[i].materials)
						{
							m.color = origColors[k++];
						}
					}
					meshes[i].enabled = origVisibility[i];
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