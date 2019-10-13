#define OCULUS_UTILITIES_AVAILABLE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace unityutilities
{
	/// <summary>
	/// Adds several movement techniques while maintaining compatibility with many rig setups.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[AddComponentMenu("unityutilities/Movement")]
	public class Movement : MonoBehaviour
	{
		public Rig rig;

		[Header("Features")] public bool grabWalls;
		public bool grabAir = true;
		public bool stickBoostBrake = true;
		public bool handBoosters = true;
		public bool yaw = true;
		public bool pitch;
		public bool roll;
		public bool slidingMovement;
		public bool teleportingMovement;

		[Header("Rotation")] 
		[Tooltip("Not used if pitch is enabled.")]
		public Side turnInput = Side.Either;
		public bool continuousRotation;
		public float continuousRotationSpeed = 100f;
		public float snapRotationAmount = 30f;
		public float turnNullZone = .3f;
		private bool snapTurnedThisFrame;
		
		
		[Header("Tuning")]
		public float mainBoosterMaxSpeed = 5f;
		public float mainBrakeDrag = 2f;
		public float maxHandBoosterSpeed = 4f;
		public float mainBoosterForce = 1f;
		public float handBoosterAccel = 1f;
		public float mainBoosterCost = 1f;
		private float mainBoosterBudget = 1f;
		private float normalDrag;
		public float slidingAccel = 1f;
		public float slidingSpeed = 3f;
		public float minVel = .1f;
		
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

		private readonly Vector3[] lastVels = new Vector3[5];
		private int lastVelsIndex;

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
		public Action TeleportStart;

		/// <summary>
		/// Called when the teleport happens.
		/// Contains the translation offset vector. 
		/// </summary>
		public Action<Vector3> TeleportEnd;
		
		/// <summary>
		/// Contains the direction of the snap turn.
		/// </summary>
		public Action<string> SnapTurn;

		/// <summary>
		/// The current chosen spot to teleport to
		/// </summary>
		[Serializable]
		public class Teleporter
		{
			// inspector values
			public bool rotateOnTeleport;
			public float maxTeleportableSlope = 20f;
			public float lineRendererWidth = .01f;
			public float teleportCurve = .01f;
			public float teleportArcInitialVel = .5f;
			public float smoothTeleportTime = .1f;
			public GameObject teleportMarkerOverride;
			public Material lineRendererMaterialOverride;
			public LayerMask validLayers = ~0;

			[Header("Blink")]
			public bool blink;
			public float blinkDuration;

			public int renderQueue = 5000;
			private float alpha;
			[HideInInspector] public Material blinkMaterial;
			public Shader blinkShader;
			[HideInInspector] public MeshRenderer blinkRenderer;
			[HideInInspector] public MeshFilter blinkMesh;



			[HideInInspector]
			public GameObject teleportMarkerInstance;
			
			
			public Teleporter()
			{
				Active = false;
			}
			
			public Teleporter(Vector3 pos, Vector3 dir)
			{
				Pos = pos;
				Dir = dir;
				Active = true;
			}

			private bool active;
			
			private Vector3 pos;
			private Vector3 dir;

			public bool Active
			{
				get => active;
				set
				{
					if (active != value)
					{
						if (teleportMarkerInstance != null)
						{
							teleportMarkerInstance.SetActive(value);
						}
						else
						{
							if (teleportMarkerOverride != null)
							{
								teleportMarkerInstance = Instantiate(teleportMarkerOverride);
							}
							else
							{
								teleportMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
								Destroy(teleportMarkerInstance.GetComponent<Collider>());
								teleportMarkerInstance.transform.localScale = new Vector3(.1f,.01f,.1f);
								teleportMarkerInstance.GetComponent<MeshRenderer>().material.color = Color.black;
							}
						}
					}

					active = value;
				}
			}

			public Vector3 Pos
			{
				get => pos;
				set
				{
					pos = value;
					if (teleportMarkerInstance != null)
					{
						teleportMarkerInstance.transform.position = pos;
					}
				}
			}

			public Vector3 Dir
			{
				get => dir;
				set
				{
					dir = value;
					if (teleportMarkerInstance != null)
					{
						teleportMarkerInstance.transform.rotation = Quaternion.LookRotation(dir);
					}
				}
			}
		}

		public Teleporter teleporter = new Teleporter();

		private void Awake() {
			if (!teleportingMovement || !teleporter.blink) return;
			teleporter.blinkMaterial = new Material(teleporter.blinkShader);
			teleporter.blinkMesh = rig.head.gameObject.AddComponent<MeshFilter>();
			teleporter.blinkRenderer = rig.head.gameObject.AddComponent<MeshRenderer>();

			Mesh mesh = new Mesh();
			teleporter.blinkMesh.mesh = mesh;

			float x = 1f;
			float y = 1f;
			float distance = .5f;

			Vector3[] vertices = {
				new Vector3(-x, -y, distance),
				new Vector3(x, -y, distance),
				new Vector3(-x, y, distance),
				new Vector3(x, y, distance)
			};

			int[] tris = { 0, 2, 1, 2, 3, 1 };

			Vector3[] normals = {
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward
			};

			Vector2[] uv = {
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

			SceneManager.activeSceneChanged += SceneChangeEvent;
		}

		private void Start()
		{
			cpt = null;
			if (cpt == null)
			{
				cpt = gameObject.AddComponent<CopyTransform>();
			}

			cpt.followPosition = true;
			cpt.positionFollowType = CopyTransform.FollowType.Velocity;
			normalDrag = rig.rb.drag;

			wasKinematic = rig.rb.isKinematic;
		}

		private void Update()
		{
			// turn
			Turn();
			
			// grab walls and air
			if (grabWalls && !grabAir)
			{
				if (leftHandGrabbedObj != null || grabbingSide == Side.Left)
				{
					GrabMove(ref rig.leftHand, ref leftHandGrabPos, Side.Left, leftHandGrabbedObj);
				}

				if (rightHandGrabbedObj != null || grabbingSide == Side.Right)
				{
					GrabMove(ref rig.rightHand, ref rightHandGrabPos, Side.Right, rightHandGrabbedObj);
				}
			}
			else if (grabAir && !snapTurnedThisFrame)
			{
				GrabMove(ref rig.leftHand, ref leftHandGrabPos, Side.Left);
				GrabMove(ref rig.rightHand, ref rightHandGrabPos, Side.Right);
			}

			Boosters();

			if (slidingMovement)
			{
				SlidingMovement();
			}

			// update lastVels
			lastVels[lastVelsIndex] = rig.rb.velocity;
			lastVelsIndex = ++lastVelsIndex % 5;
			
			// update last frame's grabbed objs
			lastLeftHandGrabbedObj = leftHandGrabbedObj;
			lastRightHandGrabbedObj = rightHandGrabbedObj;

			if (teleportingMovement)
			{
				Teleporting();
			}

			snapTurnedThisFrame = false;
		}

		#region Teleporting

		private void Teleporting()
		{
			// check for start of teleports
			if (InputMan.Up(Side.Left))
			{
				currentTeleportingSide = Side.Left;
			}

			if (InputMan.Up(Side.Right))
			{
				currentTeleportingSide = Side.Right;
			}

			// if the teleport laser is visible
			if (currentTeleportingSide != Side.None)
			{

				// check for end of teleport
				if (currentTeleportingSide == Side.Left && InputMan.ThumbstickIdle(Side.Left) ||
				    currentTeleportingSide == Side.Right && InputMan.ThumbstickIdle(Side.Right))
				{
					// complete the teleport
					TeleportTo(teleporter);
					TeleportEnd?.Invoke(teleporter.Pos + rig.head.transform.position - transform.position);
					currentTeleportingSide = Side.None;

					// delete the line renderer
					Destroy(lineRenderer);
					teleporter.Active = false;
				}
				else
				{

					// add a new linerenderer if needed
					if (lineRenderer == null)
					{
						lineRenderer = gameObject.AddComponent<LineRenderer>();
						lineRenderer.widthMultiplier = teleporter.lineRendererWidth;
						if (teleporter.lineRendererMaterialOverride != null)
						{
							lineRenderer.material = teleporter.lineRendererMaterialOverride;
						}
						else
						{
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

					if (currentTeleportingSide == Side.Left)
					{
						lastPos = rig.leftHand.position;
						lastDir = rig.leftHand.forward;
					}
					else
					{
						lastPos = rig.rightHand.position;
						lastDir = rig.rightHand.forward;
					}

					Vector3 xVelocity = Vector3.ProjectOnPlane(lastDir, Vector3.up) * velocity;
					float yVelocity = Vector3.Dot(lastDir, Vector3.up) * velocity;

					float segmentLength = .25f;
					const float numSegments = 200f;

					// the teleport line will stop at a max distance
					for (int i = 0; i < numSegments; i++)
					{
						if (Physics.Raycast(lastPos, lastDir, out teleportHit, segmentLength, teleporter.validLayers))
						{
							points.Add(teleportHit.point);

							// if the hit point is valid
							if (Vector3.Angle(teleportHit.normal, Vector3.up) < teleporter.maxTeleportableSlope)
							{
								// define the point as a good teleportable point
								teleporter.Pos = teleportHit.point;
								Vector3 dir = rig.head.forward;
								dir = Vector3.ProjectOnPlane(dir, Vector3.up);
								if (teleporter.rotateOnTeleport)
								{
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
							else
							{
								// if the hit point is close enough to the last valid point
								teleporter.Active = !(Vector3.Distance(teleporter.Pos, teleportHit.point) > .1f);
							}


							break;
						}
						else
						{
							// add the point to the line renderer
							points.Add(lastPos);

							// calculate the next ray
							Vector3 newPos = lastPos + xVelocity + Vector3.up * yVelocity;
							lastDir = newPos - lastPos;
							segmentLength = (newPos - lastPos).magnitude;
							lastPos = newPos;
							yVelocity -= .01f;
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
		private void TeleportTo(Teleporter goal)
		{
			if (goal.Active)
			{
				TeleportTo(goal.Pos, goal.Dir);
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public void TeleportTo(Vector3 position, Vector3 direction)
		{
			TeleportTo(position, Quaternion.LookRotation(direction));
		}
		
		public void TeleportTo(Vector3 position, Quaternion rotation)
		{
			float headRotOffset = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(rig.head.transform.forward,Vector3.up), Vector3.up);
			rotation = Quaternion.Euler(0,-headRotOffset,0) * rotation;
			Quaternion origRot = transform.rotation;
			transform.rotation = rotation;
		
			Vector3 headPosOffset = transform.position - rig.head.transform.position;
			headPosOffset.y = 0;
			//transform.position = position + headPosOffset;
			
			transform.rotation = origRot;
			
			StartCoroutine(DoSmoothTeleport(position + headPosOffset, rotation, teleporter.smoothTeleportTime));
		}

		private IEnumerator DoSmoothTeleport(Vector3 position, Quaternion rotation, float time)
		{
			float distance = Vector3.Distance(transform.position, position);
			
			transform.rotation = rotation;
			
			if (teleporter.blink) {
				FadeOut(teleporter.blinkDuration/2);
				yield return new WaitForSeconds(4 * teleporter.blinkDuration / 5);
			}

			for (float i = 0; i < time; i+=Time.deltaTime)
			{
				transform.position = Vector3.MoveTowards(transform.position, position, (Time.deltaTime / time)*distance);
				//transform.RotateAround(rig.head.position, axis,angle*(Time.deltaTime/time));
				yield return null;
			}

			transform.rotation = rotation;
			transform.position = position;

			if (teleporter.blink) {
				FadeIn(teleporter.blinkDuration / 2);
			}

		}

		/// <summary>
		/// Fades the screen to black
		/// </summary>
		/// <param name="duration"></param>
		public void FadeOut(float duration) {
			StartCoroutine(Fade(0, 1, duration));
		}

		/// <summary>
		/// Fades the screen from black back to normal
		/// </summary>
		/// <param name="duration"></param>
		public void FadeIn(float duration) {
			StartCoroutine(Fade(1, 0, duration));
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

		public void SetBlinkOpacity(float value) {
			Color color = Color.black;
			color.a = value;
			teleporter.blinkMaterial.color = color;
			teleporter.blinkRenderer.material = teleporter.blinkMaterial;

			teleporter.blinkRenderer.enabled = value > 0.001f;
		}

		private void SceneChangeEvent(Scene oldScene, Scene newScene) {
			SetBlinkOpacity(0);
			FadeIn(1);
		}

		#endregion

		private void RoundVelToZero()
		{
			if (rig.rb.velocity.magnitude < minVel)
			{
				rig.rb.velocity = Vector3.zero;
			}
		}

		private void SlidingMovement()
		{
			bool useForce = false;
			Vector3 forward = -rig.head.forward;
			forward.y = 0;
			forward.Normalize();

			Vector3 right = new Vector3(-forward.z, 0, forward.x);


			if (useForce)
			{
				Vector3 forwardForce = Time.deltaTime * InputMan.ThumbstickY(Side.Left) * forward * 1000f;
				if (Mathf.Abs(Vector3.Dot(rig.rb.velocity, rig.head.forward)) < slidingSpeed)
				{
					rig.rb.AddForce(forwardForce);
				}

				Vector3 rightForce = Time.deltaTime * InputMan.ThumbstickX(Side.Left) * right * 1000f;
				if (Mathf.Abs(Vector3.Dot(rig.rb.velocity, rig.head.right)) < slidingSpeed)
				{
					rig.rb.AddForce(rightForce);
				}
			}
			else
			{
				Vector3 currentSpeed = rig.rb.velocity;
				Vector3 forwardSpeed = InputMan.ThumbstickY(Side.Left) * forward;
				Vector3 rightSpeed = InputMan.ThumbstickX(Side.Left) * right;
				Vector3 speed = forwardSpeed + rightSpeed;
				rig.rb.velocity = slidingSpeed * speed + (currentSpeed.y * rig.rb.transform.up);
			}
		}

		private void Boosters()
		{
			// update timers
			mainBoosterBudget += Time.deltaTime;
			mainBoosterBudget = Mathf.Clamp01(mainBoosterBudget);

			// use main booster
			if (stickBoostBrake && InputMan.ThumbstickPressDown(Side.Left) && InputMan.ThumbstickIdle(Side.Left))
			{
				// add timeout
				if (mainBoosterBudget - mainBoosterCost > 0)
				{
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rig.rb.velocity, rig.head.forward) < mainBoosterMaxSpeed)
					{
						// add the force
						rig.rb.AddForce(mainBoosterForce * 100f * rig.head.forward);
					}

					mainBoosterBudget -= mainBoosterCost;
				}
			}

			// use main brake
			if (stickBoostBrake && InputMan.PadClick(Side.Right))
			{
				// add a bunch of drag
				rig.rb.drag = mainBrakeDrag;
			}
			else if (InputMan.PadClickUp(Side.Right))
			{
				rig.rb.drag = normalDrag;
				RoundVelToZero();
			}

			if (handBoosters)
			{
				if (InputMan.Button2(Side.Left))
				{
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rig.rb.velocity, rig.leftHand.forward) < maxHandBoosterSpeed)
					{
						// add the force
						rig.rb.AddForce(handBoosterAccel * Time.deltaTime * 100f * rig.leftHand.forward);
					}
				}

				if (InputMan.Button2(Side.Right))
				{
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rig.rb.velocity, rig.rightHand.forward) < maxHandBoosterSpeed)
					{
						// add the force
						rig.rb.AddForce(handBoosterAccel * Time.deltaTime * 100f * rig.rightHand.forward);
					}
				}
			}
		}

		private void Turn()
		{
			// don't turn if currently teleporting
			if (currentTeleportingSide != Side.None)
			{
				return;
			}
			
			Vector3 pivot = rig.head.position;
			if (grabbingSide == Side.Left)
			{
				pivot = rig.leftHand.position;
			}
			else if (grabbingSide == Side.Right)
			{
				pivot = rig.rightHand.position;
			}

			if (continuousRotation)
			{
				Side turnInputLocal = turnInput;
				if (roll)
				{
					turnInputLocal = Side.Right;
				}
				if (yaw && Mathf.Abs(InputMan.ThumbstickX(turnInputLocal)) > turnNullZone)
				{
					rig.rb.transform.RotateAround(pivot, rig.rb.transform.up,
						InputMan.ThumbstickX(turnInputLocal) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (pitch && Mathf.Abs(InputMan.ThumbstickY(turnInputLocal)) > turnNullZone)
				{
					rig.rb.transform.RotateAround(pivot, rig.head.right,
						InputMan.ThumbstickY(turnInputLocal) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (roll && Mathf.Abs(InputMan.ThumbstickX(Side.Left)) > turnNullZone)
				{
					rig.rb.transform.RotateAround(pivot, rig.head.forward,
						InputMan.ThumbstickX(Side.Left) * Time.deltaTime * continuousRotationSpeed * 2);
				}
			}
			else
			{
				Side turnInputLocal = turnInput;
				string snapTurnDirection = "";

				if (roll)
				{
					turnInputLocal = Side.Right;
				}
				if (yaw && InputMan.Left(turnInputLocal))
				{
					snapTurnDirection = "left";
					rig.rb.transform.RotateAround(pivot, rig.rb.transform.up, -snapRotationAmount);
				}
				else if (yaw && InputMan.Right(turnInputLocal))
				{
					snapTurnDirection = "right";
					rig.rb.transform.RotateAround(pivot, rig.rb.transform.up, snapRotationAmount);
				}
				else if (pitch && InputMan.Up(turnInputLocal))
				{
					snapTurnDirection = "up";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.forward, -snapRotationAmount);
				}
				else if (pitch && InputMan.Down(turnInputLocal))
				{
					snapTurnDirection = "down";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.forward, snapRotationAmount);
				}
				else if (roll && InputMan.Left(Side.Left))
				{
					snapTurnDirection = "roll-left";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.right, -snapRotationAmount);
				}
				else if (roll && InputMan.Right(Side.Left))
				{
					snapTurnDirection = "roll-right";
					rig.rb.transform.RotateAround(pivot, rig.head.transform.right, snapRotationAmount);
				}

				if (snapTurnDirection != "") {
					SnapTurn?.Invoke(snapTurnDirection);

					cpt.enabled = false;
					snapTurnedThisFrame = true;

					if (grabbingSide != Side.None) {
						cpt.positionOffset = rig.rb.position - 
						  (grabbingSide == Side.Left ? rig.leftHand.position : rig.rightHand.position);
					}
				}
			}
		}

		public void ResetOrientation()
		{
			rig.rb.transform.localRotation = Quaternion.identity;
		}

		private void GrabMove(ref Transform hand, ref GameObject grabPos, Side side, Transform parent = null)
		{
			if (InputMan.GripDown(side) || (InputMan.Grip(side) && 
                ((side == Side.Left && leftHandGrabbedObj != null && lastLeftHandGrabbedObj == null) || 
                (side == Side.Right && rightHandGrabbedObj != null && lastRightHandGrabbedObj == null))))
			{
				grabbingSide = side;

				if (grabPos != null)
				{
					Destroy(grabPos.gameObject);
				}

				grabPos = new GameObject(side + " Hand Grab Pos");
				grabPos.transform.position = hand.position;
				grabPos.transform.SetParent(parent);
				cpt.target = grabPos.transform;
				cpt.positionOffset = rig.rb.position - hand.position;
				cpt.snapIfDistanceGreaterThan = 1f;
				rig.rb.isKinematic = false;
				
				InputMan.Vibrate(side, 1);

				// if event has subscribers, execute
				OnGrab?.Invoke(parent, side);
			}
			else if (side == grabbingSide)
			{
				if (InputMan.Grip(side))
				{
					cpt.positionOffset = rig.rb.position - hand.position;
					if (!snapTurnedThisFrame) {
						cpt.enabled = true;
					}
				}
				else
				{
					if (side == grabbingSide)
					{
						grabbingSide = Side.None;
					}

					if (grabPos != null)
					{
						OnRelease?.Invoke(grabPos.transform, side);
						Destroy(grabPos.gameObject);
						cpt.target = null;
						//rig.rb.velocity = MedianAvg(lastVels);
						rig.rb.velocity = -rig.transform.TransformVector(InputMan.ControllerVelocity(side));
						RoundVelToZero();
						rig.rb.isKinematic = wasKinematic;
					}
				}
			}
		}

		public void SetGrabbedObj(Transform obj, Side side)
		{
			if (side == Side.Left)
			{
				if (obj == null)
				{
					leftHandGrabbedObj = null;
				}
				else
				{
					leftHandGrabbedObj = obj;
				}
			}
			else if (side == Side.Right)
			{
				if (obj == null)
				{
					rightHandGrabbedObj = null;
				}
				else
				{
					rightHandGrabbedObj = obj;
				}
			}
		}

		private Vector3 MedianAvg(Vector3[] inputArray)
		{
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
