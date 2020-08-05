using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace unityutilities
{

#if UNITY_EDITOR
	[CustomEditor(typeof(WorldMouseInputModule))]
	[CanEditMultipleObjects]
	public class WorldMouseInputModuleEditor : Editor
	{
		WorldMouseInputModule uuWorldMouseInputModule;

		private void OnEnable()
		{
			uuWorldMouseInputModule = target as WorldMouseInputModule;
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
		}
	}
#endif

	public class WorldMouseInputModule : BaseInputModule
	{
		[ReadOnly]
		public WorldMouse[] worldMice;
		private Camera worldMouseCam;
		private PointerEventData[] eventData;
		private PointerEventData[] mouseEventData;
		private Vector3[] lastRaycastHitPoint;
		private Vector3[] mouseLastRaycastHitPoint;
		private float[] pressedDistance;
		private float[] mousePressedDistance;
		public bool handleMouseCursor = true;
		public Camera[] extraCanvasCameras;
		public Vector3 offsetMouse;
		public float dragThreshold = .1f;
		protected override void Start()
		{
			// required...baseinputmodule.Start() does something clearly :-)
			base.Start();

			worldMice = FindObjectsOfType<WorldMouse>();

			// We create a camera that will be our raycasting camera and add it to all canvases
			// this is necessary because unity requires a screen space position to do raycasts against
			// GUI objects, and uses an "event camera" to do the actual raycasts
			worldMouseCam = new GameObject("Controller UI Camera").AddComponent<Camera>();
			worldMouseCam.stereoTargetEye = StereoTargetEyeMask.None;
			worldMouseCam.nearClipPlane = .01f;
			worldMouseCam.clearFlags = CameraClearFlags.Nothing; //note the camera renders nothing
			worldMouseCam.cullingMask = 0; //and no objects even try to draw to the camera

			Canvas[] canvases = FindObjectsOfType<Canvas>();
			foreach (Canvas canvas in canvases)
			{
				if (canvas.worldCamera == null && canvas.renderMode == RenderMode.WorldSpace)
				{
					canvas.worldCamera = worldMouseCam;
				}
			}

			eventData = new PointerEventData[worldMice.Length];
			for (int i = 0; i < worldMice.Length; i++)
			{
				eventData[i] = new PointerEventData(eventSystem);
			}
			mouseEventData = new PointerEventData[extraCanvasCameras.Length];
			for (int i = 0; i < extraCanvasCameras.Length; i++)
			{
				mouseEventData[i] = new PointerEventData(eventSystem);
			}
			lastRaycastHitPoint = new Vector3[worldMice.Length];
			mouseLastRaycastHitPoint = new Vector3[extraCanvasCameras.Length];
			pressedDistance = new float[worldMice.Length];
			mousePressedDistance = new float[extraCanvasCameras.Length];
		}


		// Process is called by UI system to process events.  
		public override void Process()
		{
			if (eventData == null)
			{
				return;
			}
			//we process each world mouse separately, which encapsulates the concept of a mouse within a camera space that will raycast into the world
			for (int i = 0; i < eventData.Length; i++)
			{
				WorldMouse wm = worldMice[i];

				wm.rayDistance = Mathf.Infinity; //assume nothing was hit to start
			}

			//the currently selected object may want to update something each frame
			BaseEventData data = GetBaseEventData();
			ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);



			//we process each world mouse separately, which encapsulates the concept of a mouse within a camera space that will raycast into the world
			for (int i = 0; i < eventData.Length; i++)
			{
				WorldMouse wm = worldMice[i];

				//wm.rayDistance = Mathf.Infinity;//assume nothing was hit to start

				//this makes the event system camera align with the world mouse
				worldMouseCam.transform.position = wm.transform.position;
				worldMouseCam.transform.forward = wm.transform.forward;
				//we reset the event data for the current frame.  Since the cursor doesn't actuall move, these are constant
				//this may cause some problems down the road...in which case we would probably create a camera for each canvas
				//and then move the cursor to the raycast intersection with the quad encapsulating the GUI, updating these deltas
				PointerEventData currentEventData = eventData[i];

				currentEventData.Reset();
				currentEventData.position = new Vector2(worldMouseCam.pixelWidth / 2.0f, worldMouseCam.pixelHeight / 2.0f);
				currentEventData.scrollDelta = Vector2.zero;
				currentEventData.button = PointerEventData.InputButton.Left;


				//this is where all the magic actually happens
				//the event system takes care of raycasting from all cameras at all cursor locations into the world
				eventSystem.RaycastAll(currentEventData, m_RaycastResultCache);
				currentEventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

				Ray ray = new Ray(worldMouseCam.transform.position, worldMouseCam.transform.forward);
				Vector3 hitPoint = ray.GetPoint(currentEventData.pointerCurrentRaycast.distance);
				if (currentEventData.pointerCurrentRaycast.distance > 0)
				{
					wm.rayDistance = currentEventData.pointerCurrentRaycast.distance;
				}


				if (wm.worldRayDistance < currentEventData.pointerCurrentRaycast.distance || wm.rayDistance < currentEventData.pointerCurrentRaycast.distance)
				{
					continue; //don't process anything if a world object was hit or a closer canvast object was hit
				}


				currentEventData.delta = hitPoint - lastRaycastHitPoint[i];
				lastRaycastHitPoint[i] = hitPoint;

				m_RaycastResultCache.Clear();

				var currentOverGo = currentEventData.pointerCurrentRaycast.gameObject;
				//process presses
				if (wm.PressDown())
				{


					currentEventData.eligibleForClick = true;
					currentEventData.delta = Vector2.zero;
					currentEventData.useDragThreshold = true;
					currentEventData.pressPosition = currentEventData.position;
					currentEventData.pointerPressRaycast = currentEventData.pointerCurrentRaycast;


					var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
					// if we have clicked something new, deselect the old thing
					// leave 'selection handling' up to the press event though.
					if (selectHandlerGO != eventSystem.currentSelectedGameObject)
					{
						eventSystem.SetSelectedGameObject(null, currentEventData);
					}


					var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, currentEventData, ExecuteEvents.pointerDownHandler);

					// didnt find a press handler... search for a click handler
					if (newPressed == null)
					{
						newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
					}

					currentEventData.pointerPress = newPressed;    // TODO:remove?
					pressedDistance[i] = 0;
					currentEventData.rawPointerPress = currentOverGo;

					currentEventData.clickTime = Time.unscaledTime;

					// Save the drag handler as well
					currentEventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

					if (currentEventData.pointerDrag != null)
					{
						ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.initializePotentialDrag);
					}

				}
				// PointerUp notification
				if (wm.PressUp())
				{

					ExecuteEvents.Execute(currentEventData.pointerPress, currentEventData, ExecuteEvents.pointerUpHandler);

					// see if we button up on the same element that we clicked on...
					var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

					// PointerClick and Drop events
					if (currentEventData.pointerPress == pointerUpHandler && currentEventData.eligibleForClick)
					{
						ExecuteEvents.Execute(currentEventData.pointerPress, currentEventData, ExecuteEvents.pointerClickHandler);
					}
					else if (currentEventData.pointerDrag != null && currentEventData.dragging)
					{
						ExecuteEvents.ExecuteHierarchy(currentOverGo, currentEventData, ExecuteEvents.dropHandler);
					}

					currentEventData.eligibleForClick = false;
					currentEventData.pointerPress = null;
					pressedDistance[i] = 0;              // just in case
					currentEventData.rawPointerPress = null;

					if (currentEventData.pointerDrag != null && currentEventData.dragging)
					{
						ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.endDragHandler);
					}

					currentEventData.dragging = false;
					currentEventData.pointerDrag = null;

					// redo pointer enter / exit to refresh state
					// so that if we hovered over something that ignored it before
					// due to having pressed on something else
					// it now gets it.
					if (currentOverGo != currentEventData.pointerEnter)
					{
						HandlePointerExitAndEnter(currentEventData, null);
						HandlePointerExitAndEnter(currentEventData, currentOverGo);
					}
				}

				//process move
				var targetGO = currentEventData.pointerCurrentRaycast.gameObject;
				HandlePointerExitAndEnter(currentEventData, targetGO);

				//processDrag


				// If pointer is not moving or if a button is not pressed (or pressed control did not return drag handler), do nothing
				if (!currentEventData.IsPointerMoving() || currentEventData.pointerDrag == null)
				{
				}
				else
				{
					// We are eligible for drag. If drag did not start yet, add drag distance
					if (!currentEventData.dragging)
					{
						pressedDistance[i] += currentEventData.delta.sqrMagnitude;
						bool m_isButtonPressedChanged = false;
						bool shouldStartDragging = !m_isButtonPressedChanged && (pressedDistance[i] > dragThreshold);
						if (shouldStartDragging)
						{
							ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.beginDragHandler);
							currentEventData.dragging = true;
						}
					}

					// Drag notification
					if (currentEventData.dragging)
					{
						// Before doing drag we should cancel any pointer down state
						// And clear selection!
						if (currentEventData.pointerPress != currentEventData.pointerDrag)
						{
							ExecuteEvents.Execute(currentEventData.pointerPress, currentEventData, ExecuteEvents.pointerUpHandler);

							currentEventData.eligibleForClick = false;
							currentEventData.pointerPress = null;
							currentEventData.rawPointerPress = null;
						}
						ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.dragHandler);
					}
				}

			}

			//now do it for the mice
			for (int i = 0; i < extraCanvasCameras.Length; i++)
			{  //this is for the real mouse (must do this once per camera we want to handle mouse events for)

				Camera c = extraCanvasCameras[i];

				Vector3 relativeDisplay = Display.RelativeMouseAt(Input.mousePosition);
				Vector3 raycastPos = relativeDisplay;
				if ((c.targetDisplay != relativeDisplay.z) || relativeDisplay == Vector3.zero)
				{
					//ignore, not the primary monitor...no way to get valid coordinates for this.
					if (!Application.isEditor)
					{
						continue; //above stuff is valid
					}
					if (Application.isEditor && Screen.width != 1280)
					{

						continue; //such a huge hack...
					}
					else
					{
						raycastPos = Input.mousePosition;
					}
				}

				Ray r = c.ScreenPointToRay(raycastPos);
				worldMouseCam.transform.position = r.origin;
				worldMouseCam.transform.forward = r.direction;
				//we reset the event data for the current frame.  Since the cursor doesn't actuall move, these are constant
				//this may cause some problems down the road...in which case we would probably create a camera for each canvas
				//and then move the cursor to the raycast intersection with the quad encapsulating the GUI, updating these deltas
				PointerEventData currentEventData = mouseEventData[i];

				currentEventData.Reset();
				currentEventData.position = new Vector2(worldMouseCam.pixelWidth / 2.0f, worldMouseCam.pixelHeight / 2.0f);
				currentEventData.scrollDelta = Vector2.zero;
				currentEventData.button = PointerEventData.InputButton.Left;


				//this is where all the magic actually happens
				//the event system takes care of raycasting from all cameras at all cursor locations into the world
				eventSystem.RaycastAll(currentEventData, m_RaycastResultCache);
				currentEventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

				Ray ray = new Ray(worldMouseCam.transform.position, worldMouseCam.transform.forward);
				Vector3 hitPoint = ray.GetPoint(currentEventData.pointerCurrentRaycast.distance);
				//wm.rayDistance = currentEventData.pointerCurrentRaycast.distance;

				/*
				if (wm.worldRayDistance < currentEventData.pointerCurrentRaycast.distance || wm.rayDistance < currentEventData.pointerCurrentRaycast.distance) {
					continue; //don't process anything if a world object was hit or a closer canvast object was hit
				}
				*/

				currentEventData.delta = hitPoint - mouseLastRaycastHitPoint[i];
				mouseLastRaycastHitPoint[i] = hitPoint;

				m_RaycastResultCache.Clear();

				var currentOverGo = currentEventData.pointerCurrentRaycast.gameObject;
				//process presses
				if (Input.GetMouseButtonDown(0))
				{


					currentEventData.eligibleForClick = true;
					currentEventData.delta = Vector2.zero;
					currentEventData.useDragThreshold = true;
					currentEventData.pressPosition = currentEventData.position;
					currentEventData.pointerPressRaycast = currentEventData.pointerCurrentRaycast;


					var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
					// if we have clicked something new, deselect the old thing
					// leave 'selection handling' up to the press event though.
					if (selectHandlerGO != eventSystem.currentSelectedGameObject)
					{
						eventSystem.SetSelectedGameObject(null, currentEventData);
					}


					var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, currentEventData, ExecuteEvents.pointerDownHandler);

					// didnt find a press handler... search for a click handler
					if (newPressed == null)
					{
						newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
					}

					currentEventData.pointerPress = newPressed;    // TODO:remove?
					mousePressedDistance[i] = 0;
					currentEventData.rawPointerPress = currentOverGo;

					currentEventData.clickTime = Time.unscaledTime;

					// Save the drag handler as well
					currentEventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

					if (currentEventData.pointerDrag != null)
					{
						ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.initializePotentialDrag);
					}

				}
				// PointerUp notification
				if (Input.GetMouseButtonUp(0))
				{

					ExecuteEvents.Execute(currentEventData.pointerPress, currentEventData, ExecuteEvents.pointerUpHandler);

					// see if we button up on the same element that we clicked on...
					var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

					// PointerClick and Drop events
					if (currentEventData.pointerPress == pointerUpHandler && currentEventData.eligibleForClick)
					{
						ExecuteEvents.Execute(currentEventData.pointerPress, currentEventData, ExecuteEvents.pointerClickHandler);
					}
					else if (currentEventData.pointerDrag != null && currentEventData.dragging)
					{
						ExecuteEvents.ExecuteHierarchy(currentOverGo, currentEventData, ExecuteEvents.dropHandler);
					}

					currentEventData.eligibleForClick = false;
					currentEventData.pointerPress = null;
					mousePressedDistance[i] = 0;              // just in case
					currentEventData.rawPointerPress = null;

					if (currentEventData.pointerDrag != null && currentEventData.dragging)
					{
						ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.endDragHandler);
					}

					currentEventData.dragging = false;
					currentEventData.pointerDrag = null;

					// redo pointer enter / exit to refresh state
					// so that if we hovered over something that ignored it before
					// due to having pressed on something else
					// it now gets it.
					if (currentOverGo != currentEventData.pointerEnter)
					{
						HandlePointerExitAndEnter(currentEventData, null);
						HandlePointerExitAndEnter(currentEventData, currentOverGo);
					}
				}

				//process move
				var targetGO = currentEventData.pointerCurrentRaycast.gameObject;
				HandlePointerExitAndEnter(currentEventData, targetGO);

				//processDrag


				// If pointer is not moving or if a button is not pressed (or pressed control did not return drag handler), do nothing
				if (!currentEventData.IsPointerMoving() || currentEventData.pointerDrag == null)
				{
				}
				else
				{
					// We are eligible for drag. If drag did not start yet, add drag distance
					if (!currentEventData.dragging)
					{
						mousePressedDistance[i] += currentEventData.delta.sqrMagnitude;
						bool m_isButtonPressedChanged = false;
						bool shouldStartDragging = !m_isButtonPressedChanged && (mousePressedDistance[i] > dragThreshold);
						if (shouldStartDragging)
						{
							ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.beginDragHandler);
							currentEventData.dragging = true;
						}
					}

					// Drag notification
					if (currentEventData.dragging)
					{
						// Before doing drag we should cancel any pointer down state
						// And clear selection!
						if (currentEventData.pointerPress != currentEventData.pointerDrag)
						{
							ExecuteEvents.Execute(currentEventData.pointerPress, currentEventData, ExecuteEvents.pointerUpHandler);

							currentEventData.eligibleForClick = false;
							currentEventData.pointerPress = null;
							currentEventData.rawPointerPress = null;
						}
						ExecuteEvents.Execute(currentEventData.pointerDrag, currentEventData, ExecuteEvents.dragHandler);
					}
				}
			}
		}
	}
}