using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

namespace unityutilities
{
#if UNITY_EDITOR
	[CustomEditor(typeof(WorldMouseInputModule))]
	[CanEditMultipleObjects]
	public class WorldMouseInputModuleEditor : Editor
	{
		WorldMouseInputModule worldMouseInputModule;

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
		[ReadOnly] [System.NonSerialized] public readonly List<WorldMouse> worldMice = new List<WorldMouse>();
		private Camera worldMouseCam;
		/// <summary>
		/// The last pointer event data for each mouse
		/// </summary>
		private readonly List<PointerEventData> eventData = new List<PointerEventData>();
		/// <summary>
		/// The last pressed object for each mouse
		/// </summary>
		private readonly List<GameObject> lastPressed = new List<GameObject>();
		private static WorldMouseInputModule instance;
		/// <summary>
		/// The last gameobject clicked by any mouse
		/// </summary>
		public static GameObject globalLastPressed;

		public static WorldMouseInputModule Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new GameObject("WorldMouseInputModule").AddComponent<WorldMouseInputModule>();
				}

				return instance;
			}
		}

		protected override void Awake()
		{
			instance = this;
		}

		protected override void Start()
		{
			base.Start(); //required...baseinputmodule does something clearly :-)

			//we create a camera that will be our raycasting camera and add it to all canvases
			//this is necessary because unity requires a screen space position to do raycasts against
			//gui objects, and uses an "event camera" to do the actual raycasts
			worldMouseCam = new GameObject("Controller UI Camera").AddComponent<Camera>();
			worldMouseCam.nearClipPlane = .01f;
			worldMouseCam.fieldOfView = 1;
			worldMouseCam.depth = -100;
			worldMouseCam.clearFlags = CameraClearFlags.Nothing; // set the camera to render nothing
			worldMouseCam.cullingMask = 0; // and no objects even try to draw to the camera
			worldMouseCam.stereoTargetEye = StereoTargetEyeMask.None;

			// if this object is dontdestroyonload, apply that to the camera as well - parenting doesn't work because of positions
			if (gameObject.scene.buildIndex == -1) DontDestroyOnLoad(worldMouseCam);

			FindCanvases();

			FindObjectsOfType<WorldMouse>().ToList().ForEach(m => AddWorldMouse(m));

			//SceneManager.sceneLoaded += SceneLoaded;
		}

		//private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
		//{
		//	FindCanvases();
		//	foreach (WorldMouse wm in worldMice)
		//	{
		//		AddWorldMouse(wm);
		//	}
		//}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (instance != null && instance.worldMouseCam != null)
			{
				instance.worldMouseCam.gameObject.SetActive(true);
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (instance != null && instance.worldMouseCam != null)
			{
				instance.worldMouseCam.gameObject.SetActive(false);
			}
		}

		public static void FindCanvases()
		{
			Canvas[] canvases = FindObjectsOfType<Canvas>();
			foreach (Canvas canvas in canvases)
			{
				canvas.worldCamera = instance.worldMouseCam;
			}
		}

		public void AddWorldMouse(WorldMouse m)
		{
			if (!worldMice.Contains(m))
			{
				worldMice.Add(m);
			}

			eventData.Add(new PointerEventData(eventSystem));
			lastPressed.Add(null);

			FindCanvases();
		}

		public void RemoveWorldMouse(WorldMouse m)
		{
			int index = worldMice.IndexOf(m);
			worldMice.RemoveAt(index);
			eventData.RemoveAt(index);
			lastPressed.RemoveAt(index);
		}

		// Process is called by UI system to process events.  
		public override void Process()
		{
			//the currently selected object may want to update something each frame
			BaseEventData data = GetBaseEventData();
			ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

			//we process each world mouse separately, which encapsulates the concept of a mouse within a camera space that will raycast into the world
			for (int i = 0; i < eventData.Count && i < worldMice.Count; i++)
			{
				WorldMouse wm = worldMice[i];

				wm.rayDistance = 0.0f; //assume nothing was hit to start

				//this makes the event system camera align with the world mouse
				worldMouseCam.transform.position = wm.transform.position;
				worldMouseCam.transform.forward = wm.transform.forward;
				//we reset the event data for the current frame.  Since the cursor doesn't actually move, these are constant
				//this may cause some problems down the road...in which case we would probably create a camera for each canvas
				//and then move the cursor to the raycast intersection with the quad encapsulating the GUI, updating these deltas
				PointerEventData currentEventData = eventData[i];
				currentEventData.Reset();
				currentEventData.delta = Vector2.zero;
				currentEventData.position = new Vector2(worldMouseCam.pixelWidth / 2.0f, worldMouseCam.pixelHeight / 2.0f);
				currentEventData.scrollDelta = Vector2.zero;

				//this is where all the magic actually happens
				//the event system takes care of raycasting from all cameras at all cursor locations into the world
				eventSystem.RaycastAll(currentEventData, m_RaycastResultCache);
				currentEventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
				if (wm.worldRayDistance < currentEventData.pointerCurrentRaycast.distance)
				{
					continue; //don't process anything if a world object was hit
				}

				//if we hit something this will not be null
				if (currentEventData.pointerCurrentRaycast.gameObject != null)
				{
					//this is useful to know where the object was hit (to draw a point or limit the lenght of a laser)
					wm.rayDistance = currentEventData.pointerCurrentRaycast.distance;
					//we can think of the object we hit as what we are hovering above (the simplest type)
					GameObject hoverObject = currentEventData.pointerCurrentRaycast.gameObject;
					if (wm.lastHoverObject != hoverObject)
					{
						wm.HoverEntered?.Invoke(hoverObject);
					}

					wm.lastHoverObject = hoverObject;

					// handle enter and exit events (highlight)
					HandlePointerExitAndEnter(currentEventData, hoverObject);

					//if the user clicks, other events may need to be handled
					if (wm.PressDown())
					{
						//if we click, we want to clear the current selection 
						//and fire associated events
						if (eventSystem.currentSelectedGameObject)
						{
							eventSystem.SetSelectedGameObject(null);
						}

						//these are important for those handling a click event
						currentEventData.pressPosition = currentEventData.position;
						currentEventData.pointerPressRaycast = currentEventData.pointerCurrentRaycast;

						//execute both the pointer down handler 
						GameObject handledPointerDown = ExecuteEvents.ExecuteHierarchy(hoverObject, currentEventData,
							ExecuteEvents.pointerDownHandler);
						//execute the click handler, either on the hoverObject if nothing handled  pointerdown or on whatever handled it
						GameObject handledClick = ExecuteEvents.ExecuteHierarchy(
							handledPointerDown == null ? hoverObject : handledPointerDown, currentEventData,
							ExecuteEvents.pointerClickHandler);
						//something handled the click or pressed, so save a reference to it, needed later
						GameObject newPressed = handledClick != null ? handledClick : handledPointerDown;

						//we need to deal with a new selection if the press/click was handled
						if (newPressed != null)
						{
							currentEventData.pointerPress = newPressed;
							if (ExecuteEvents.GetEventHandler<ISelectHandler>(newPressed))
							{
								eventSystem.SetSelectedGameObject(newPressed);
							}
						}

						//execute a drag start event on the currently pressed object and save it
						ExecuteEvents.Execute(newPressed, currentEventData, ExecuteEvents.beginDragHandler);
						currentEventData.pointerDrag = newPressed;

						//we save what was currently pressed for when we release
						lastPressed[i] = newPressed == null ? hoverObject : newPressed;

						wm.ClickDown?.Invoke(lastPressed[i]);
					}

					//handle releasing the "click"
					if (wm.PressUp())
					{
						if (lastPressed[i] != null)
						{
							ExecuteEvents.Execute(lastPressed[i], currentEventData, ExecuteEvents.endDragHandler);
							ExecuteEvents.ExecuteHierarchy(lastPressed[i], currentEventData, ExecuteEvents.dropHandler);
							ExecuteEvents.Execute(lastPressed[i], currentEventData, ExecuteEvents.pointerUpHandler);
							currentEventData.pointerDrag = null;
							currentEventData.rawPointerPress = null;
							currentEventData.pointerPress = null;
							wm.Clicked?.Invoke(lastPressed[i]);
							globalLastPressed = lastPressed[i];
							lastPressed[i] = null;
						}
					}

					// drag handling
					if (lastPressed[i] != null)
					{
						ExecuteEvents.Execute(lastPressed[i], currentEventData, ExecuteEvents.dragHandler);
					}
				}


				m_RaycastResultCache.Clear();
			}
		}
	}
}