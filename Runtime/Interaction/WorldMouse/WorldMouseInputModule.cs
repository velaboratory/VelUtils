using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VelUtils.Interaction.WorldMouse
{
#if UNITY_EDITOR
	[CustomEditor(typeof(WorldMouseInputModule))]
	[CanEditMultipleObjects]
	public class WorldMouseInputModuleEditor : Editor
	{
		private WorldMouseInputModule worldMouseInputModule;

		private void OnEnable()
		{
			worldMouseInputModule = target as WorldMouseInputModule;
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Manager for WorldMice.");
			EditorGUILayout.Space();

			//if (uuWorldMouseInputModule.worldMice.Length == 0)
			//{
			//	EditorGUILayout.HelpBox("No WorldMice set.", MessageType.Warning);
			//	if (GUILayout.Button("Find WorldMice in scene."))
			//	{
			//		uuWorldMouseInputModule.worldMice = FindObjectsOfType<WorldMouse>();
			//	}
			//}

			base.OnInspectorGUI();

			if (worldMouseInputModule.worldMice.Count > 0)
			{
				EditorGUILayout.LabelField("Current WorldMice:");
			}

			GUI.enabled = false;
			foreach (WorldMouse mouse in worldMouseInputModule.worldMice)
			{
				EditorGUILayout.ObjectField(mouse, typeof(WorldMouse), true);
			}

			GUI.enabled = true;

			WorldMouseInputModule.autoFindCanvases = EditorGUILayout.Toggle("Auto Find Canvases", WorldMouseInputModule.autoFindCanvases);

			// last clicked object
			EditorGUILayout.LabelField("Last clicked object:");
			GUI.enabled = false;
			EditorGUILayout.ObjectField(WorldMouseInputModule.globalLastPressed, typeof(GameObject), true);
			GUI.enabled = true;
		}
	}
#endif
	public class WorldMouseInputModule : BaseInputModule
	{
		internal readonly List<WorldMouse> worldMice = new List<WorldMouse>();
		private PointerEventData[] eventData;

		private static WorldMouseInputModule instance;

		/// <summary>
		/// The last gameobject clicked by any mouse. Just for debugging
		/// </summary>
		internal static GameObject globalLastPressed;

		public static bool autoFindCanvases = true;
		private Camera cam;

		public WorldMouse lastProcessedMouse;
		internal static WorldMouseInputModule Instance
		{
			get
			{
				if (instance == null)
				{
					// TODO this causes problems:
					// Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?)

					// Debug.Log("Creating new WorldMouseInputModule");
					// EventSystem eventSystem = FindObjectOfType<EventSystem>();
					// if (eventSystem != null)
					// {
					// 	instance = eventSystem.gameObject.AddComponent<WorldMouseInputModule>();
					// }
					// else
					// {
					// 	instance = new GameObject("WorldMouseInputModule").AddComponent<WorldMouseInputModule>();
					// }
				}

				return instance;
			}
		}

		protected override void Awake()
		{
			instance = this;
			base.Awake();
		}

		protected override void Start()
		{
			base.Start();

			if (cam == null)
			{
				cam = new GameObject("World Mouse UI Camera").AddComponent<Camera>();
				cam.clearFlags = CameraClearFlags.Nothing;
				cam.stereoTargetEye = StereoTargetEyeMask.None;
				cam.orthographic = true;
				cam.orthographicSize = 0.001f;
				cam.cullingMask = 0;
				cam.nearClipPlane = 0.01f;
				cam.depth = 0f;
				cam.allowHDR = false;
				cam.enabled = false;
				cam.fieldOfView = 0.00001f;


				// if this object is dontdestroyonload, apply that to the camera as well - parenting doesn't work because of positions
				if (gameObject.scene.buildIndex == -1) DontDestroyOnLoad(cam);
			}

			FindCanvases();

			FindObjectsByType<WorldMouse>(FindObjectsSortMode.None).ToList().ForEach(AddWorldMouse);
		}

		public static void FindCanvases()
		{
			if (!autoFindCanvases) return;
			if (!instance) return;

			Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
			foreach (Canvas canvas in canvases)
			{
				canvas.worldCamera = instance.cam;
			}
		}

		public static void AddWorldMouse(WorldMouse wm)
		{
			Instance.AddWorldMouseLocal(wm);
		}

		private void AddWorldMouseLocal(WorldMouse wm)
		{
			if (!worldMice.Contains(wm))
			{
				worldMice.Add(wm);
				// Preserve existing entries (append keeps their positions); the new
				// mouse gets a fresh entry, never an aliased old one.
				RebuildEventData(wm);
			}

			wm.pointerIndex = worldMice.IndexOf(wm);
		}

		public static void RemoveWorldMouse(WorldMouse wm)
		{
			Instance.RemoveWorldMouseLocal(wm);
		}

		private void RemoveWorldMouseLocal(WorldMouse wm)
		{
			if (worldMice.Contains(wm))
			{
				worldMice.Remove(wm);
			}

			// Rebuild FIRST, while every survivor's pointerIndex still names its OLD
			// slot — that's what lets us carry its in-flight state over. Then re-index.
			RebuildEventData(null);

			foreach (WorldMouse point in worldMice)
			{
				point.SetIndex(worldMice.IndexOf(point));
			}
		}

		/// <summary>
		/// Resize eventData to match worldMice, PRESERVING surviving mice's entries.
		/// Replacing the whole array (the old behaviour) silently wiped in-flight
		/// hover/press state for every other mouse whenever any mouse was added or
		/// removed (controller sleep, scene churn) — a press whose state vanished
		/// mid-click could leave that hand's UI dead. Carry-over is by each mouse's
		/// CURRENT pointerIndex (so call this BEFORE re-indexing), with a claim
		/// guard so two mice can never end up sharing one PointerEventData.
		/// </summary>
		private void RebuildEventData(WorldMouse justAdded)
		{
			PointerEventData[] old = eventData;
			PointerEventData[] fresh = new PointerEventData[worldMice.Count];
			bool[] claimed = old != null ? new bool[old.Length] : null;

			for (int i = 0; i < worldMice.Count; i++)
			{
				WorldMouse m = worldMice[i];
				if (old != null && m != null && m != justAdded)
				{
					int oldIndex = m.pointerIndex;
					if (oldIndex >= 0 && oldIndex < old.Length && !claimed[oldIndex] && old[oldIndex] != null)
					{
						fresh[i] = old[oldIndex];
						claimed[oldIndex] = true;
					}
				}

				if (fresh[i] == null)
				{
					fresh[i] = new PointerEventData(eventSystem)
					{
						delta = Vector2.zero,
						position = new Vector2(Screen.width / 2f, Screen.height / 2f)
					};
				}
			}

			eventData = fresh;
		}

		/// <summary>
		/// Resolve a mouse to its CURRENT index, self-healing if it is missing or
		/// stale (a cached integer pointerIndex can go stale if add/remove events are
		/// missed or arrive out of order — that left a hand unable to click anything
		/// until restart). Returns -1 only if the mouse is null.
		/// </summary>
		private int ResolveIndex(WorldMouse wm)
		{
			if (wm == null) return -1;
			int index = worldMice.IndexOf(wm);
			if (index < 0)
			{
				// Not registered (missed OnEnable / desynced state) — re-register now.
				AddWorldMouseLocal(wm);
				index = worldMice.IndexOf(wm);
			}

			if (index >= 0 && wm.pointerIndex != index) wm.pointerIndex = index;
			return index;
		}

		public override void Process()
		{
#pragma warning disable
			if (eventSystem.currentSelectedGameObject != null) {
				var data = GetBaseEventData();
				ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

			}
				

			for (int index = 0; index < worldMice.Count; index++)
			{
				try
				{
					if (worldMice[index] != null && worldMice[index].enabled)
					{
						lastProcessedMouse = worldMice[index];
						cam.transform.position = worldMice[index].GetPosition();
						cam.transform.forward = worldMice[index].GetForward();
						float dist = worldMice[index].GetMaxDistance();
						
						// Hooks in to Unity's event system to handle hovering
						eventSystem.RaycastAll(eventData[index], m_RaycastResultCache);
						RaycastResult ray = FindFirstRaycast(m_RaycastResultCache);
						// GraphicRaycaster measures distance from the ray origin, which ScreenPointToRay
						// puts ON the camera near plane - not at the hand. Uncompensated, every consumer
						// (laser length, hover distance, synced remote lasers) reads ~nearClipPlane short,
						// which is exactly the visible air gap between the laser tip and the canvas.
						if (ray.gameObject != null) ray.distance += cam.nearClipPlane;
						eventData[index].pointerCurrentRaycast = ray;
						if (dist > ray.distance)
						{

							HandlePointerExitAndEnter(eventData[index], eventData[index].pointerCurrentRaycast.gameObject);

							ExecuteEvents.Execute(eventData[index].pointerDrag, eventData[index], ExecuteEvents.dragHandler);
						}
					}
				}
				catch
				{
					// ignored
				}
			}
#pragma warning restore
		}

		/// <summary>
		/// Identity-based press: resolves the mouse's index at call time (self-healing
		/// if its cached index went stale), so a press can never act on another
		/// mouse's — or a nonexistent — slot.
		/// </summary>
		public static GameObject ProcessPress(WorldMouse wm)
		{
			if (Instance == null) return null;
			int index = Instance.ResolveIndex(wm);
			return index >= 0 ? Instance.ProcessPressLocal(index) : null;
		}

		public static GameObject ProcessPress(int index)
		{
			if (Instance == null || index < 0 || index >= Instance.worldMice.Count) return null;
			return Instance.ProcessPressLocal(index);
		}

		private GameObject ProcessPressLocal(int index)
		{
			lastProcessedMouse = worldMice[index];
			cam.transform.position = worldMice[index].GetPosition();
			cam.transform.forward = worldMice[index].GetForward();
			float dist = worldMice[index].GetMaxDistance();

			if (dist > eventData[index].pointerCurrentRaycast.distance)
			{
				// Hooks in to Unity's event system to process a press
				eventData[index].pointerPressRaycast = eventData[index].pointerCurrentRaycast;

				eventData[index].pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventData[index].pointerPressRaycast.gameObject);
				eventData[index].pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(eventData[index].pointerPressRaycast.gameObject);

				// Ray pose at press time, for the forgiving-release drift test in ProcessReleaseLocal.
				worldMice[index].pressOrigin = worldMice[index].GetPosition();
				worldMice[index].pressForward = worldMice[index].GetForward();

				ExecuteEvents.Execute(eventData[index].pointerPress, eventData[index], ExecuteEvents.pointerDownHandler);
				ExecuteEvents.Execute(eventData[index].pointerDrag, eventData[index], ExecuteEvents.beginDragHandler);

				return eventData[index].pointerPressRaycast.gameObject;
			}

			return null;
		}

		/// <summary>Identity-based release; see <see cref="ProcessPress(WorldMouse)"/>.</summary>
		public static void ProcessRelease(WorldMouse wm)
		{
			if (Instance == null) return;
			int index = Instance.ResolveIndex(wm);
			if (index >= 0) Instance.ProcessReleaseLocal(index);
		}

		public static void ProcessRelease(int index)
		{
			if (Instance == null || index < 0 || index >= Instance.worldMice.Count) return;
			Instance.ProcessReleaseLocal(index);
		}

		private void ProcessReleaseLocal(int index)
		{
			lastProcessedMouse = worldMice[index];
			cam.transform.position = worldMice[index].GetPosition();
			cam.transform.forward = worldMice[index].GetForward();

			// Hooks in to Unity's event system to process a release
			GameObject pointerRelease = ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventData[index].pointerCurrentRaycast.gameObject);

			// Forgiving release: squeezing the trigger jerks the hand, so the ray often slips off
			// the button between press and release - which used to eat the click. A press captures
			// its target: releasing still clicks it as long as the ray has swept less than the
			// mouse's tolerance (measured at the pressed surface's distance). Big sweeps -
			// intentional drags / aborts - still cancel the click, exactly like before.
			bool clickPressed = eventData[index].pointerPress == pointerRelease;
			WorldMouse wm = worldMice[index];
			if (!clickPressed && eventData[index].pointerPress != null && wm != null)
			{
				float surfaceDist = eventData[index].pointerPressRaycast.distance;
				if (surfaceDist <= 0) surfaceDist = 1f;
				Vector3 pressPoint = wm.pressOrigin + wm.pressForward * surfaceDist;
				Vector3 nowPoint = wm.GetPosition() + wm.GetForward() * surfaceDist;
				clickPressed = Vector3.Distance(pressPoint, nowPoint) <= wm.releaseClickTolerance;
			}

			if (clickPressed)
			{
				ExecuteEvents.Execute(eventData[index].pointerPress, eventData[index], ExecuteEvents.pointerClickHandler);
			}

			ExecuteEvents.Execute(eventData[index].pointerPress, eventData[index], ExecuteEvents.pointerUpHandler);
			ExecuteEvents.Execute(eventData[index].pointerDrag, eventData[index], ExecuteEvents.endDragHandler);

			eventData[index].pointerPress = null;
			eventData[index].pointerDrag = null;

			eventData[index].pointerCurrentRaycast.Clear();
		}

		/// <summary>
		/// Returns a raycast even if past the max distance.
		/// Make sure to limit distance for things like linerenderers.
		/// Identity-based: resolves the mouse's slot at call time (self-healing).
		/// Null when no module exists yet.
		/// </summary>
		public static PointerEventData GetData(WorldMouse wm)
		{
			if (Instance == null) return null;
			int index = Instance.ResolveIndex(wm);
			return index >= 0 && index < Instance.eventData.Length ? Instance.eventData[index] : null;
		}

		public static PointerEventData GetData(int index)
		{
			if (Instance == null || Instance.eventData == null ||
			    index < 0 || index >= Instance.eventData.Length)
			{
				return null;
			}

			return Instance.eventData[index];
		}
	}
}