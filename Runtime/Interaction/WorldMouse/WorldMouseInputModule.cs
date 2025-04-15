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
				eventData = new PointerEventData[worldMice.Count];

				for (int i = 0; i < eventData.Length; i++)
				{
					eventData[i] = new PointerEventData(eventSystem)
					{
						delta = Vector2.zero,
						position = new Vector2(Screen.width / 2f, Screen.height / 2f)
					};
				}
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

			foreach (WorldMouse point in worldMice)
			{
				point.SetIndex(worldMice.IndexOf(point));
			}

			eventData = new PointerEventData[worldMice.Count];
			for (int i = 0; i < eventData.Length; i++)
			{
				eventData[i] = new PointerEventData(eventSystem)
				{
					delta = Vector2.zero,
					position = new Vector2(Screen.width / 2f, Screen.height / 2f)
				};
			}
		}

		public override void Process()
		{
#pragma warning disable
			for (int index = 0; index < worldMice.Count; index++)
			{
				try
				{
					if (worldMice[index] != null && worldMice[index].enabled)
					{
						cam.transform.position = worldMice[index].GetPosition();
						cam.transform.forward = worldMice[index].GetForward();
						float dist = worldMice[index].GetMaxDistance();
						
						// Hooks in to Unity's event system to handle hovering
						eventSystem.RaycastAll(eventData[index], m_RaycastResultCache);
						RaycastResult ray = FindFirstRaycast(m_RaycastResultCache);
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

		public static GameObject ProcessPress(int index)
		{
			return Instance.ProcessPressLocal(index);
		}

		private GameObject ProcessPressLocal(int index)
		{
			cam.transform.position = worldMice[index].GetPosition();
			cam.transform.forward = worldMice[index].GetForward();
			float dist = worldMice[index].GetMaxDistance();

			if (dist > eventData[index].pointerCurrentRaycast.distance)
			{
				// Hooks in to Unity's event system to process a press
				eventData[index].pointerPressRaycast = eventData[index].pointerCurrentRaycast;

				eventData[index].pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventData[index].pointerPressRaycast.gameObject);
				eventData[index].pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(eventData[index].pointerPressRaycast.gameObject);

				ExecuteEvents.Execute(eventData[index].pointerPress, eventData[index], ExecuteEvents.pointerDownHandler);
				ExecuteEvents.Execute(eventData[index].pointerDrag, eventData[index], ExecuteEvents.beginDragHandler);

				return eventData[index].pointerPressRaycast.gameObject;
			}

			return null;
		}

		public static void ProcessRelease(int index)
		{
			Instance.ProcessReleaseLocal(index);
		}

		private void ProcessReleaseLocal(int index)
		{
			cam.transform.position = worldMice[index].GetPosition();
			cam.transform.forward = worldMice[index].GetForward();

			// Hooks in to Unity's event system to process a release
			GameObject pointerRelease = ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventData[index].pointerCurrentRaycast.gameObject);

			if (eventData[index].pointerPress == pointerRelease)
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
		/// Make sure to limit distance for things like linerenderers
		/// </summary>
		public static PointerEventData GetData(int index)
		{
			if (index >= Instance.eventData.Length)
			{
				Debug.Log("HERE");
			}
			return Instance.eventData[index];
		}
	}
}