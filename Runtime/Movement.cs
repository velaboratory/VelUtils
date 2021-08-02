using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace unityutilities {
	/// <summary>
	/// Adds several movement techniques while maintaining compatibility with many rig setups.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[AddComponentMenu("unityutilities/Movement")]
	public class Movement : MonoBehaviour {
		public Rig rig;

		public bool setPhysicsTimestepToRefreshRate;


		[Header("Hand-based Movement")]

		[Tooltip("Whether to move with hands when colliding with an object. " +
			"Only really makes sense when \"Grab Air\" is false.")]
		public bool grabWalls;

		[Tooltip("Allows for hand-based movement by holding grip.")]
		public bool grabAir;
		public VRInput grabInput = VRInput.Grip;

		[Tooltip("Press left and right stick to boost and brake in head direction.")]
		public bool stickBoostBrake;

		[Tooltip("Press Y and B to boost in the hand pointing directions.")]
		public bool handBoosters;

		public float mainBoosterMaxSpeed = 5f;
		public float mainBrakeDrag = 2f;
		public float maxHandBoosterSpeed = 4f;
		public float mainBoosterForce = 1f;
		public float handBoosterAccel = 1f;
		public float mainBoosterCost = 1f;
		private float mainBoosterBudget = 1f;
		public float minVel = .1f;





		[Header("Stick-based Movement")]

		[Tooltip("Which controller to use for snap turn. Always `Right` if roll is enabled.")]
		public Side turnInput = Side.Either;
		public bool continuousRotation;
		public float continuousRotationSpeed = 100f;
		public float snapRotationAmount = 30f;
		public float turnNullZone = .3f;
		private bool snapTurnedThisFrame;

		[Tooltip("Enable thumbstick turning for left/right.")]
		public bool yaw = true;

		[Tooltip("Enable thumbstick turning for up/down.")]
		public bool pitch;

		[Tooltip("Enable thumbstick turning for roll.")]
		public bool roll;

		[Tooltip("Enable thumbstick movement like and FPS controller.")]
		public bool slidingMovement;

		public float slidingAccel = 1f;
		public float slidingSpeed = 3f;


		[Header("Teleporting")]

		[Tooltip("Enable thumbstick teleporting.")]
		public bool teleportingMovement;

		public Teleporter teleporter = new Teleporter();
		private const string colorProperty = "_Color";

		[Tooltip("Scoot backwards with thumbstick down")]
		public bool scootBackMovement;
		public Side scootBackMovementController = Side.None;


		[Header("Weird Movement")]

		[Tooltip("Values > 1 increase the effect of head translation on movement in space.")]
		public float translationalGain = 1;

		[Tooltip("The \"Tracking Space\" object for Oculus rigs works well.")]
		public Transform translationalGainOffsetObj;


		
		// Private junk

		private float normalDrag;

		public Action<Transform, Side> OnGrab;
		public Action<Transform, Side> OnRelease;

		[HideInInspector]
		public Transform leftHandGrabbedObj;
		[HideInInspector]
		public Transform rightHandGrabbedObj;
		private Transform lastLeftHandGrabbedObj;
		private Transform lastRightHandGrabbedObj;
		private GameObject leftHandGrabPos;
		private GameObject rightHandGrabPos;
		private Vector3 lastLeftHandPos;
		private Vector3 lastRightHandPos;

		private Queue<Vector3> lastVels = new Queue<Vector3>();
		private int lastVelsLength = 5;

		private CopyTransform cpt;

		private Side grabbingSide = Side.None;
		private bool wasKinematic;


		// teleporting vars
		private LineRenderer lineRenderer;
		[HideInInspector]
		public Side currentTeleportingSide = Side.None;
		private RaycastHit teleportHit;

		/// <summary>
		/// Called when the teleporter is activated.
		/// </summary>
		public Action<Side> TeleportStart;

		/// <summary>
		/// Called when the teleport happens.
		/// Contains the translation offset vector. 
		/// </summary>
		public Action<Side, Vector3> TeleportEnd;

		/// <summary>
		/// Contains the direction of the snap turn.
		/// </summary>
		public Action<Side, string> SnapTurn;

		/// <summary>
		/// The current chosen spot to teleport to
		/// </summary>
		[Serializable]
		public class Teleporter {
			// inspector values
			public Side inputSide = Side.Either;
			public bool rotateOnTeleport;
			public float maxTeleportableSlope = 45f;
			public float lineRendererWidth = .01f;
			public float teleportCurve = .01f;
			public float teleportArcInitialVel = .5f;
			public float smoothTeleportTime;
			public GameObject teleportMarkerOverride;
			public Material lineRendererMaterialOverride;
			public LayerMask validLayers = ~0;

			[Header("Blink")]
			public bool blink = true;
			public float blinkDuration = .1f;

			public int renderQueue = 5000;
			[HideInInspector] public Material blinkMaterial;
			public Shader blinkShader;
			[HideInInspector] public MeshRenderer blinkRenderer;
			[HideInInspector] public MeshFilter blinkMesh;
			[HideInInspector] public Coroutine blinkCoroutine;



			[HideInInspector]
			public GameObject teleportMarkerInstance;


			public Teleporter() {
				Active = false;
			}

			public Teleporter(Vector3 pos, Vector3 dir) {
				Pos = pos;
				Dir = dir;
				Active = true;
			}
			public Teleporter(Teleporter t) {
				Pos = t.Pos;
				Dir = t.Dir;
				blinkShader = t.blinkShader;
				validLayers = t.validLayers;
				blinkShader = t.blinkShader;
			}



			private bool active;

			private Vector3 pos;
			private Vector3 dir;

			public bool Active {
				get => active;
				set {
					if (active != value) {
						if (teleportMarkerInstance != null) {
							teleportMarkerInstance.SetActive(value);
						}
						else {
							if (teleportMarkerOverride != null) {
								teleportMarkerInstance = Instantiate(teleportMarkerOverride);
							}
							else {
								teleportMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
								Destroy(teleportMarkerInstance.GetComponent<Collider>());
								teleportMarkerInstance.transform.localScale = new Vector3(.1f, .01f, .1f);
								teleportMarkerInstance.GetComponent<MeshRenderer>().material.color = Color.black;
							}
						}
					}

					active = value;
				}
			}

			public Vector3 Pos {
				get => pos;
				set {
					pos = value;
					if (teleportMarkerInstance != null) {
						teleportMarkerInstance.transform.position = pos;
					}
				}
			}

			public Vector3 Dir {
				get => dir;
				set {
					dir = value;
					if (teleportMarkerInstance != null) {
						teleportMarkerInstance.transform.rotation = Quaternion.LookRotation(dir);
					}
				}
			}
		}


		private void Awake()
		{
			SetupTeleporter();

			if (setPhysicsTimestepToRefreshRate && XRDevice.refreshRate != 0)
			{
				Time.fixedDeltaTime = 1 / XRDevice.refreshRate;
			}
		}

		private void SetupTeleporter()
		{
			if (teleporter.blinkShader == null) return;
			teleporter.blinkMaterial = new Material(teleporter.blinkShader);
			GameObject blinkMeshObj = new GameObject("Blink Mesh");
			blinkMeshObj.transform.SetParent(rig.head);
			blinkMeshObj.transform.localPosition = Vector3.zero;
			blinkMeshObj.transform.localRotation = Quaternion.identity;
			teleporter.blinkMesh = blinkMeshObj.AddComponent<MeshFilter>();
			teleporter.blinkRenderer = blinkMeshObj.AddComponent<MeshRenderer>();

			Mesh mesh = new Mesh();
			teleporter.blinkMesh.mesh = mesh;

			const float x = 1f;
			const float y = 1f;
			const float distance = .5f;

			Vector3[] vertices =
			{
				new Vector3(-x, -y, distance),
				new Vector3(x, -y, distance),
				new Vector3(-x, y, distance),
				new Vector3(x, y, distance)
			};

			int[] tris = {0, 2, 1, 2, 3, 1};

			Vector3[] normals =
			{
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward
			};

			Vector2[] uv =
			{
				new Vector2(0, 0),
				new Vector2(1, 0),
				new Vector2(0, 1),
				new Vector2(1, 1)
			};

			mesh.vertices = vertices;
			mesh.triangles = tris;
			mesh.normals = normals;
			mesh.uv = uv;

			teleporter.blinkMaterial.renderQueue = teleporter.renderQueue;
			teleporter.blinkRenderer.material = teleporter.blinkMaterial;

			SetBlinkOpacity(0);
		}

		private void Start() {
			cpt = null;
			if (cpt == null) {
				cpt = gameObject.AddComponent<CopyTransform>();
			}

			cpt.followPosition = true;
			cpt.positionFollowType = CopyTransform.FollowType.Velocity;
			normalDrag = rig.rb.drag;

			wasKinematic = rig.rb.isKinematic;
		}

		private void Update() {

			// turn
			Turn();

			// grab walls and air
			if (grabWalls && !grabAir) {
				if (leftHandGrabbedObj != null || grabbingSide == Side.Left) {
					GrabMove(ref rig.leftHand, ref leftHandGrabPos, Side.Left, leftHandGrabbedObj);
				}

				if (rightHandGrabbedObj != null || grabbingSide == Side.Right) {
					GrabMove(ref rig.rightHand, ref rightHandGrabPos, Side.Right, rightHandGrabbedObj);
				}
			}
			else if (grabAir && !snapTurnedThisFrame) {
				GrabMove(ref rig.leftHand, ref leftHandGrabPos, Side.Left);
				GrabMove(ref rig.rightHand, ref rightHandGrabPos, Side.Right);
			}

			Boosters();

			
			SlidingMovement();

			// update lastVels
			lastVels.Enqueue(rig.rb.velocity);
			if (lastVels.Count > lastVelsLength) lastVels.Dequeue();

			lastLeftHandPos = rig.leftHand.position;
			lastRightHandPos = rig.rightHand.position;

			// update last frame's grabbed objs
			lastLeftHandGrabbedObj = leftHandGrabbedObj;
			lastRightHandGrabbedObj = rightHandGrabbedObj;

			if (teleportingMovement) {
				Teleporting();
			}

			if (scootBackMovement)
			{
				if (InputMan.Down(scootBackMovementController))
				{
					Vector3 f = rig.head.forward;
					Vector3 p = rig.head.position;
					f.y = 0;
					p.y = rig.transform.position.y;
					TeleportTo(p - f * .5f, Quaternion.LookRotation(f, Vector3.up));
				}
			}

			snapTurnedThisFrame = false;

			TranslationalGain();

		}


		#region Teleporting

		private void Teleporting() {

			// check for start of teleports
			if (InputMan.Up(Side.Left) && teleporter.inputSide.Contains(Side.Left)) {
				if (currentTeleportingSide == Side.None) {
					TeleportStart?.Invoke(Side.Left);
				}
				currentTeleportingSide = Side.Left;
			}

			if (InputMan.Up(Side.Right) && teleporter.inputSide.Contains(Side.Right)) {
				if (currentTeleportingSide == Side.None) {
					TeleportStart?.Invoke(Side.Right);
				}
				currentTeleportingSide = Side.Right;
			}

			// if the teleport laser is visible
			if (currentTeleportingSide != Side.None) {

				// check for end of teleport
				if (currentTeleportingSide == Side.Left && InputMan.ThumbstickIdle(Side.Left) ||
					currentTeleportingSide == Side.Right && InputMan.ThumbstickIdle(Side.Right)) {
					// complete the teleport
					TeleportTo(teleporter);
					TeleportEnd?.Invoke(currentTeleportingSide, teleporter.Pos + rig.head.transform.position - transform.position);
					currentTeleportingSide = Side.None;

					// delete the line renderer
					Destroy(lineRenderer);
					teleporter.Active = false;
				}
				else {

					// add a new linerenderer if needed
					if (lineRenderer == null) {
						lineRenderer = gameObject.AddComponent<LineRenderer>();
						lineRenderer.widthMultiplier = teleporter.lineRendererWidth;
						if (teleporter.lineRendererMaterialOverride != null) {
							lineRenderer.material = teleporter.lineRendererMaterialOverride;
						}
						else {
							Material material;
							(material = lineRenderer.material).shader = Shader.Find("Unlit/Color");
							material.color = Color.black;
						}
					}

					// simulate the curved ray
					Vector3 lastPos;
					Vector3 lastDir;
					float velocity = teleporter.teleportArcInitialVel;
					List<Vector3> points = new List<Vector3>();

					if (currentTeleportingSide == Side.Left) {
						lastPos = rig.leftHand.position;
						lastDir = rig.leftHand.forward;
					}
					else {
						lastPos = rig.rightHand.position;
						lastDir = rig.rightHand.forward;
					}

					Vector3 xVelocity = Vector3.ProjectOnPlane(lastDir, Vector3.up) * velocity;
					float yVelocity = Vector3.Dot(lastDir, Vector3.up) * velocity;

					lastDir *= velocity;
					const float maxSegments = 200;


					// add the point to the line renderer
					points.Add(lastPos);

					// the teleport line will stop at a max distance
					for (int i = 0; i < maxSegments; i++) {
						if (Physics.Raycast(lastPos, lastDir.normalized, out teleportHit, lastDir.magnitude, teleporter.validLayers)) {
							points.Add(teleportHit.point);

							// if the hit point is valid
							if (Vector3.Angle(teleportHit.normal, Vector3.up) < teleporter.maxTeleportableSlope) {
								// define the point as a good teleportable point
								teleporter.Pos = teleportHit.point;
								Vector3 dir = rig.head.forward;
								dir = Vector3.ProjectOnPlane(dir, Vector3.up);
								if (teleporter.rotateOnTeleport) {
									Vector3 thumbstickDir = new Vector3(
										InputMan.ThumbstickX(currentTeleportingSide),
										0,
										InputMan.ThumbstickY(currentTeleportingSide));
									thumbstickDir.Normalize();
									float angle = -Vector3.SignedAngle(-Vector3.forward, thumbstickDir, Vector3.up);
									dir = Quaternion.Euler(0, angle, 0) * dir;
								}

								teleporter.Dir = dir;


								teleporter.Active = true;

							}
							else {
								// if the hit point is close enough to the last valid point
								teleporter.Active = !(Vector3.Distance(teleporter.Pos, teleportHit.point) > .1f);
							}


							break;
						}
						else {
							// calculate the next ray
							lastPos = lastPos + lastDir;
							Vector3 newPos = lastPos + xVelocity + Vector3.up * yVelocity;
							lastDir = newPos - lastPos;
							yVelocity -= .01f;

							// add the endpoint to the line renderer
							points.Add(lastPos);
						}

						// if we reached the end of the arc without hitting something
						if (i + 1 == maxSegments) {;
							teleporter.Active = false;
						}
					}


					lineRenderer.positionCount = points.Count;
					lineRenderer.SetPositions(points.ToArray());
				}
			}
		}

		/// <summary>
		/// May not actually teleport if goal is not active
		/// </summary>
		/// <param name="goal">The target pos</param>
		private void TeleportTo(Teleporter goal) {
			if (goal.Active) {
				TeleportTo(goal.Pos, goal.Dir);
			}
		}

		public void TeleportTo(Vector3 position, Vector3 direction) {
			TeleportTo(position, Quaternion.LookRotation(direction));
		}

		/// <summary>
		/// Moves the player object to the requested location.
		/// Can be used externally to set player position
		/// </summary>
		/// <param name="position">Global target position</param>
		/// <param name="rotation">Global target rotation</param>
		/// <param name="noBlinkOverride">Set to true to avoid fading to black even if that is set normally</param>
		public void TeleportTo(Vector3 position, Quaternion rotation, bool noBlinkOverride = false) {
			float headRotOffset = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(rig.head.transform.forward, Vector3.up), Vector3.up);
			rotation = Quaternion.Euler(0, -headRotOffset, 0) * rotation;
			Quaternion origRot = transform.rotation;
			transform.rotation = rotation;

			Vector3 headPosOffset = transform.position - rig.head.transform.position;
			headPosOffset.y = 0;
			//transform.position = position + headPosOffset;

			transform.rotation = origRot;

			StartCoroutine(DoSmoothTeleport(position + headPosOffset, rotation, teleporter.smoothTeleportTime, noBlinkOverride));
		}

		private IEnumerator DoSmoothTeleport(Vector3 position, Quaternion rotation, float time, bool noBlinkOverride = false) {
			float distance = Vector3.Distance(transform.position, position);

			transform.rotation = rotation;

			if (teleporter.blink && !noBlinkOverride) {
				FadeOut(teleporter.blinkDuration / 2);
				yield return new WaitForSeconds(4 * teleporter.blinkDuration / 5);
			}

			for (float i = 0; i < time; i += Time.deltaTime) {
				transform.position = Vector3.MoveTowards(transform.position, position, (Time.deltaTime / time) * distance);
				//transform.RotateAround(rig.head.position, axis,angle*(Time.deltaTime/time));
				yield return null;
			}

			transform.rotation = rotation;
			transform.position = position;

			if (teleporter.blink && !noBlinkOverride) {
				FadeIn(teleporter.blinkDuration / 2);
			}

		}

		/// <summary>
		/// Fades the screen to black
		/// </summary>
		/// <param name="duration"></param>
		public void FadeOut(float duration) {
			if (teleporter.blinkCoroutine != null)
			{
				StopCoroutine(teleporter.blinkCoroutine);
			}
			StartCoroutine(Fade(0, 1, duration));
		}

		/// <summary>
		/// Fades the screen from black back to normal
		/// </summary>
		/// <param name="duration"></param>
		public void FadeIn(float duration) {
			if (teleporter.blinkCoroutine != null)
			{
				StopCoroutine(teleporter.blinkCoroutine);
			}
			teleporter.blinkCoroutine = StartCoroutine(Fade(1, 0, duration));
		}

		private IEnumerator Fade(float startVal, float endVal, float duration) {
			float time = 0;
			while (time < duration) {
				SetBlinkOpacity(Mathf.Lerp(startVal, endVal, time / duration));
				time += Time.deltaTime;
				yield return null;
			}
			SetBlinkOpacity(endVal);
		}

		public void SetBlinkOpacity(float value, bool stopOtherFades = false) {
			if (stopOtherFades && teleporter.blinkCoroutine != null)
			{
				StopCoroutine(teleporter.blinkCoroutine);
			}
			Color color = Color.black;
			color.a = value;
			if (teleporter.blinkMaterial != null)
			{
				teleporter.blinkMaterial.SetColor(colorProperty, color);
				teleporter.blinkRenderer.material = teleporter.blinkMaterial;
				teleporter.blinkRenderer.enabled = value > 0.001f;
			}
		}

		#endregion

		private void TranslationalGain() {
			if (translationalGain == 1) {
				return;
			}

			Vector3 trackingPosOffset = rig.head.localPosition;
			trackingPosOffset.y = 0;

			translationalGainOffsetObj.localPosition = trackingPosOffset * (translationalGain - 1);
		}

		private void RoundVelToZero() {
			if (rig.rb.velocity.magnitude < minVel) {
				rig.rb.velocity = Vector3.zero;
			}
		}

		private void SlidingMovement() {
			if (!slidingMovement) return;

			float horizontal = InputMan.ThumbstickX(Side.Left);
			float vertical = InputMan.ThumbstickY(Side.Left);

			bool useForce = false;
			Vector3 forward = -rig.head.forward;
			forward.y = 0;
			forward.Normalize();

			Vector3 right = new Vector3(-forward.z, 0, forward.x);


			if (useForce) {
				Vector3 forwardForce = Time.deltaTime * vertical * forward * 1000f;
				if (Mathf.Abs(Vector3.Dot(rig.rb.velocity, rig.head.forward)) < slidingSpeed) {
					rig.rb.AddForce(forwardForce);
				}

				Vector3 rightForce = Time.deltaTime * horizontal * right * 1000f;
				if (Mathf.Abs(Vector3.Dot(rig.rb.velocity, rig.head.right)) < slidingSpeed) {
					rig.rb.AddForce(rightForce);
				}
			}
			else {
				Vector3 currentSpeed = rig.rb.velocity;
				Vector3 forwardSpeed = vertical * forward;
				Vector3 rightSpeed = horizontal * right;
				Vector3 speed = forwardSpeed + rightSpeed;
				rig.rb.velocity = slidingSpeed * speed + (currentSpeed.y * rig.rb.transform.up);
			}
		}

		private void Boosters() {
			// update timers
			mainBoosterBudget += Time.deltaTime;
			mainBoosterBudget = Mathf.Clamp01(mainBoosterBudget);

			// use main booster
			if (stickBoostBrake && InputMan.ThumbstickPressDown(Side.Left) && InputMan.ThumbstickIdle(Side.Left)) {
				// add timeout
				if (mainBoosterBudget - mainBoosterCost > 0) {
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rig.rb.velocity, rig.head.forward) < mainBoosterMaxSpeed) {
						// add the force
						rig.rb.AddForce(mainBoosterForce * 100f * rig.head.forward);
					}

					mainBoosterBudget -= mainBoosterCost;
				}
			}

			// use main brake
			if (stickBoostBrake && InputMan.PadClick(Side.Right)) {
				// add a bunch of drag
				rig.rb.drag = mainBrakeDrag;
			}
			else if (InputMan.PadClickUp(Side.Right)) {
				rig.rb.drag = normalDrag;
				RoundVelToZero();
			}

			if (handBoosters) {
				if (InputMan.Button2(Side.Left)) {
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rig.rb.velocity, rig.leftHand.forward) < maxHandBoosterSpeed) {
						// add the force
						rig.rb.AddForce(handBoosterAccel * Time.deltaTime * 100f * rig.leftHand.forward);
					}
				}

				if (InputMan.Button2(Side.Right)) {
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rig.rb.velocity, rig.rightHand.forward) < maxHandBoosterSpeed) {
						// add the force
						rig.rb.AddForce(handBoosterAccel * Time.deltaTime * 100f * rig.rightHand.forward);
					}
				}
			}
		}

		private void Turn() {

			// don't turn if currently teleporting
			if (currentTeleportingSide != Side.None) {
				return;
			}

			Vector3 pivot = rig.head.position;
			if (grabbingSide == Side.Left) {
				pivot = rig.leftHand.position;
			}
			else if (grabbingSide == Side.Right) {
				pivot = rig.rightHand.position;
			}

			if (continuousRotation) {
				Side turnInputLocal = turnInput;
				if (roll) {
					turnInputLocal = Side.Right;
				}
				if (yaw && Mathf.Abs(InputMan.ThumbstickX(turnInputLocal)) > turnNullZone) {
					rig.rb.transform.RotateAround(pivot, rig.rb.transform.up,
						InputMan.ThumbstickX(turnInputLocal) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (pitch && Mathf.Abs(InputMan.ThumbstickY(turnInputLocal)) > turnNullZone) {
					rig.rb.transform.RotateAround(pivot, rig.head.right,
						InputMan.ThumbstickY(turnInputLocal) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (roll && Mathf.Abs(InputMan.ThumbstickX(Side.Left)) > turnNullZone) {
					rig.rb.transform.RotateAround(pivot, rig.head.forward,
						InputMan.ThumbstickX(Side.Left) * Time.deltaTime * continuousRotationSpeed * 2);
				}
			}
			else {
				Side turnInputLocal = turnInput;
				string snapTurnDirection = "";

				if (roll) {
					turnInputLocal = Side.Right;
				}
				if (yaw && InputMan.Left(turnInputLocal)) {
					snapTurnDirection = "left";
					rig.rb.transform.RotateAround(pivot, rig.rb.transform.up, -snapRotationAmount);
				}
				else if (yaw && InputMan.Right(turnInputLocal)) {
					snapTurnDirection = "right";
					rig.rb.transform.RotateAround(pivot, rig.rb.transform.up, snapRotationAmount);
				}
				else if (pitch && InputMan.Up(turnInputLocal)) {
					snapTurnDirection = "up";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.forward, -snapRotationAmount);
				}
				else if (pitch && InputMan.Down(turnInputLocal)) {
					snapTurnDirection = "down";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.forward, snapRotationAmount);
				}
				else if (roll && InputMan.Left(Side.Left)) {
					snapTurnDirection = "roll-left";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.right, -snapRotationAmount);
				}
				else if (roll && InputMan.Right(Side.Left)) {
					snapTurnDirection = "roll-right";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.right, snapRotationAmount);
				}

				if (snapTurnDirection != "") {
					Side whichSideWasUsed;
					if (turnInputLocal == Side.Either) {
						if (InputMan.ThumbstickX(Side.Left) > InputMan.ThumbstickX(Side.Right))
							whichSideWasUsed = Side.Left;
						else
							whichSideWasUsed = Side.Right;
					}
					else {
						whichSideWasUsed = turnInputLocal;
					}
					SnapTurn?.Invoke(whichSideWasUsed, snapTurnDirection);

					cpt.enabled = false;
					snapTurnedThisFrame = true;

					if (grabbingSide != Side.None) {
						cpt.positionOffset = rig.rb.position -
						  (grabbingSide == Side.Left ? rig.leftHand.position : rig.rightHand.position);
					}
				}
			}
		}

		public void ResetOrientation() {
			rig.rb.transform.localRotation = Quaternion.identity;
		}

		private void GrabMove(ref Transform hand, ref GameObject grabPos, Side side, Transform parent = null) {
			bool grabDown = false;
			bool grab = false;
			switch (grabInput) {
				case VRInput.Trigger:
					grabDown = InputMan.TriggerDown(side);
					grab = InputMan.Trigger(side);
					break;
				case VRInput.Grip:
					grabDown = InputMan.GripDown(side);
					grab = InputMan.Grip(side);
					break;
			}

			if (grabDown || (grab &&
				((side == Side.Left && leftHandGrabbedObj != null && lastLeftHandGrabbedObj == null) ||
				(side == Side.Right && rightHandGrabbedObj != null && lastRightHandGrabbedObj == null)))) {
				grabbingSide = side;

				if (grabPos != null) {
					Destroy(grabPos.gameObject);
				}

				grabPos = new GameObject(side + " Hand Grab Pos");
				grabPos.transform.position = hand.position;
				grabPos.transform.SetParent(parent);
				cpt.SetTarget(grabPos.transform, false);
				cpt.positionOffset = rig.rb.position - hand.position;
				cpt.snapIfDistanceGreaterThan = 1f;
				rig.rb.isKinematic = false;

				InputMan.Vibrate(side, 1);

				// if event has subscribers, execute
				OnGrab?.Invoke(parent, side);
			}
			else if (side == grabbingSide) {
				if (grab) {
					cpt.positionOffset = rig.rb.position - hand.position;
					if (!snapTurnedThisFrame) {
						cpt.enabled = true;
					}
				}
				else {
					if (side == grabbingSide) {
						grabbingSide = Side.None;
					}

					if (grabPos != null) {
						OnRelease?.Invoke(grabPos.transform, side);
						Destroy(grabPos.gameObject);
						cpt.SetTarget(null);
						//rig.rb.velocity = MedianAvg(lastVels);
						rig.rb.velocity = -rig.transform.TransformVector(InputMan.ControllerVelocity(side));
						RoundVelToZero();
						rig.rb.isKinematic = wasKinematic;
					}
				}
			}
		}

		/// <summary>
		/// Sets a grabbed object for that side. Set to null for that side on release.
		/// </summary>
		/// <param name="obj">The object just grabbed.</param>
		/// <param name="side">Which side's object to modify.</param>
		public void SetGrabbedObj(Transform obj, Side side) {
			if (side == Side.Left) {
				if (obj == null) {
					leftHandGrabbedObj = null;
				}
				else {
					leftHandGrabbedObj = obj;
				}
			}
			else if (side == Side.Right) {
				if (obj == null) {
					rightHandGrabbedObj = null;
				}
				else {
					rightHandGrabbedObj = obj;
				}
			}
		}

		private Vector3 MedianAvg(Vector3[] inputArray) {
			List<Vector3> list = new List<Vector3>(inputArray);
			list = list.OrderBy(x => x.magnitude).ToList();
			list.RemoveAt(0);
			list.RemoveAt(list.Count - 1);
			Vector3 result = new Vector3(
				list.Average(x => x.x),
				list.Average(x => x.y),
				list.Average(x => x.z));
			return result;
		}
	}
}
