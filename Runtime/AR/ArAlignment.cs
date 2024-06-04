using System;
using UnityEngine;
using VelUtils.VRInteraction;

namespace VelUtils.Ar
{
	/// <summary>
	/// This script moves the camera rig to align the virtual space with known real world points. In conVRged, this is on the MenuTablet 
	/// </summary>
	public class ArAlignment : MonoBehaviour
	{
		/// <summary>
		/// This transform should be a static object in the scene. We will move our camera rig so that this point aligns with point 1 in the real world. The forward vector will point at point 2.
		/// If there is no origin in the scene, alignment cannot happen. 
		/// </summary>
		private ArAlignmentOrigin alignmentOrigin;

		[Tooltip("This is the object that we will move to align the spaces")]
		public Transform cameraRig;

		[Tooltip("Used for getting hand positions for spawning points. This may be the same as cameraRig")]
		public Rig rig;

		/// <summary>
		/// Technically we would only need one point to perform alignment, but saving both gives the user the ability to adjust after the fact easily.
		/// </summary>
		public Action<Vector3, Vector3> OnAlignmentComplete;

		/// <summary>
		/// Point 1 and 2 are in world space
		/// </summary>
		public Action<Vector3, Vector3> OnManualAlignmentComplete;

		public Action<int, GameObject> PointSpawned;

		/// <summary>
		/// Called every frame that the point is moved
		/// </summary>
		public Action<int, GameObject> PointMoved;

		/// <summary>
		/// Called right before destroying a point
		/// </summary>
		public Action<int, GameObject> PointDestroyed;

		public bool debugLogs = true;

		public static ArAlignment instance;

		public GameObject pointPrefab;

		[Tooltip(
			"If these are not set, the prefab is spawned instead. Once a point has been spawned, it will be reused for future calls to SpawnPoint()")]
		public GameObject[] pointObjects = new GameObject[2];

		public Color[] pointObjectColors = new Color[]
		{
			new Color(1, 0, 0, .1f),
			new Color(0, 1, 0, .1f),
		};

		public bool inputsEnabled = true;


		private void OnEnable()
		{
			instance = this;
		}

		private void OnDisable()
		{
			if (instance == this) instance = null;
		}

		public void TryAlign(bool manualAlignment = true)
		{
			if (pointObjects[0] != null && pointObjects[1] != null)
			{
				Align(pointObjects[0].transform.position, pointObjects[1].transform.position, manualAlignment);
			}
		}

		public void Align(Vector3 point1, Vector3 point2, bool manualAlignment = true)
		{
			Log("Aligning two points");
			if (alignmentOrigin == null)
			{
				alignmentOrigin = FindObjectOfType<ArAlignmentOrigin>();
			}

			if (alignmentOrigin == null)
			{
				Debug.LogError("Couldn't find an alignment anchor in this scene.");
				return;
			}

			point2.y = point1.y;
			float angle = Vector3.SignedAngle(point2 - point1, alignmentOrigin.transform.forward, Vector3.up);
			cameraRig.RotateAround(point1, Vector3.up, angle);
			Vector3 diff = alignmentOrigin.transform.position - point1;
			cameraRig.position += diff;
			OnAlignmentComplete?.Invoke(point1, point2);
			if (manualAlignment)
			{
				OnManualAlignmentComplete?.Invoke(point1, point2);
			}

			Log($"Alignment complete (manual: {manualAlignment})");
		}

		public void Log(object message)
		{
			if (debugLogs) Debug.Log("[VelUtils.Ar] " + message);
		}


		public void SpawnPoint(int index, Vector3? position)
		{
			// TODO this is bad for perf. Should check on loading scene if we are in a scene with an origin
			if (alignmentOrigin == null)
			{
				alignmentOrigin = FindObjectOfType<ArAlignmentOrigin>();
			}

			if (alignmentOrigin == null)
			{
				Debug.LogError("Couldn't find an alignment anchor in this scene.");
				return;
			}

			Vector3 pos = position ?? transform.position;
			if (pointObjects[index] == null)
			{
				Log("Spawning new point " + index);
				pointObjects[index] = Instantiate(pointPrefab, pos, Quaternion.identity, parent: cameraRig);
				PointSpawned?.Invoke(index, pointObjects[index]);
			}
			else
			{
				pointObjects[index].transform.SetPositionAndRotation(pos, Quaternion.identity);
				Log("Moving point " + index);
			}

			VRGrabbable grabbable = pointObjects[index].GetComponent<VRGrabbable>();
			Renderer rend = pointObjects[index].GetComponent<Renderer>();
			rend.material.color = pointObjectColors[index];
			PointMoved?.Invoke(index, pointObjects[index]);
			grabbable.Released += () => TryAlign();
		}

		private void Update()
		{
			if (inputsEnabled)
			{
				if (InputMan.ThumbstickPress(Side.Left))
				{
					SpawnPoint(0, rig.leftHand.position + rig.leftHand.forward * .2f);
					TryAlign(false);
				}

				if (InputMan.ThumbstickPress(Side.Right))
				{
					SpawnPoint(1, rig.rightHand.position + rig.rightHand.forward * .2f);
					TryAlign(false);
				}

				if (InputMan.ThumbstickPressUp(Side.Left))
				{
					TryAlign();
				}

				if (InputMan.ThumbstickPressUp(Side.Right))
				{
					TryAlign();
				}

				if (Input.GetKeyDown(KeyCode.F1) || (InputMan.Button1(Side.Left) && InputMan.Button1Down(Side.Right)))
				{
					TryAlign();
				}
			}
		}
	}
}