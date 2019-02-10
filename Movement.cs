using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Adds several movement techniques while maintaining compatibility with many rig setups.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class Movement : MonoBehaviour
	{
		[Header("Object References")]
		public Rigidbody rigRB;
		public Transform head;
		public Transform leftHand;
		public Transform rightHand;

		[Header("Features")] public bool grabWalls;
		public bool grabAir = true;
		public bool stickBoostBrake = true;
		public bool handBoosters = true;
		public bool yaw = true;
		public bool pitch;
		public bool roll;
		public bool slidingMovement;
		public bool teleportingMovement;

		[Header("Tuning")] public bool continuousRotation;
		public float continuousRotationSpeed = 100f;
		public float snapRotationAmount = 30f;
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
		public float maxTeleportableSlope = 10f;
		public Material lineRendererMaterial;
		public float lineRendererWidth = .02f;
		public float teleportCurve;

		public delegate void GrabEvent(Transform obj, Side side);

		public event GrabEvent OnGrab;
		public event GrabEvent OnRelease;

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
		
		
		// teleporting vars
		private LineRenderer lineRenderer;
		private Side currentTeleportingSide = Side.None;
		RaycastHit teleportHit;


		void Start()
		{
			cpt = null;
			if (cpt == null)
			{
				cpt = gameObject.AddComponent<CopyTransform>();
			}

			cpt.followPosition = true;
			cpt.positionFollowType = CopyTransform.FollowType.Velocity;
			normalDrag = rigRB.drag;

			
		}

		void Update()
		{
			// turn
			Turn();
			
			// grab walls and air
			if (grabWalls)
			{
				if (leftHandGrabbedObj != null || grabbingSide == Side.Left)
				{
					GrabMove(ref leftHand, ref leftHandGrabPos, Side.Left, leftHandGrabbedObj);
				}

				if (rightHandGrabbedObj != null || grabbingSide == Side.Right)
				{
					GrabMove(ref rightHand, ref rightHandGrabPos, Side.Right, rightHandGrabbedObj);
				}
			}
			else if (grabAir)
			{
				GrabMove(ref leftHand, ref leftHandGrabPos, Side.Left);
				GrabMove(ref rightHand, ref rightHandGrabPos, Side.Right);
			}

			Boosters();

			if (slidingMovement)
			{
				SlidingMovement();
			}

			// update lastVels
			lastVels[lastVelsIndex] = rigRB.velocity;
			lastVelsIndex = ++lastVelsIndex % 5;
			
			// update last frame's grabbed objs
			lastLeftHandGrabbedObj = leftHandGrabbedObj;
			lastRightHandGrabbedObj = rightHandGrabbedObj;

			Teleporting();

		}

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
				if (currentTeleportingSide == Side.Left && InputMan.ThumbstickIdleY(Side.Left) ||
				    currentTeleportingSide == Side.Right && InputMan.ThumbstickIdleY(Side.Right))
				{
					// complete the teleport
					TeleportTo(teleportHit.point, transform.rotation);
					currentTeleportingSide = Side.None;
					
					// delete the line renderer
					Destroy(lineRenderer);
				}


				// add a new linerenderer if needed
				if (lineRenderer == null)
				{
					lineRenderer = gameObject.AddComponent<LineRenderer>();
					lineRenderer.widthMultiplier = lineRendererWidth;
					lineRenderer.material = lineRendererMaterial;
				}

				// simulate the curved ray
				Vector3 lastPos;
				Vector3 lastDir;
				List<Vector3> points = new List<Vector3>();
				
				if (currentTeleportingSide == Side.Left)
				{
					lastPos = leftHand.position;
					lastDir = leftHand.forward;
				}
				else
				{
					lastPos = rightHand.position;
					lastDir = rightHand.forward;
				}

				const float segmentLength = .2f;
				const float numSegments = 100f;
				
				// the teleport line will stop at a max distance
				for (int i = 0; i < numSegments; i++)
				{
					if (Physics.Raycast(lastPos, lastDir, out teleportHit, segmentLength))
					{
						points.Add(teleportHit.point);

						if (Vector3.Angle(teleportHit.normal, Vector3.up) < maxTeleportableSlope)
						{
							// TODO define the point as a good teleportable point
						}
						
						
						break;
					}
					else
					{
						// add the point to the line renderer
						points.Add(lastPos);
						
						// calculate the next ray
						lastPos += lastDir * segmentLength;
						lastDir = Vector3.RotateTowards(lastDir, Vector3.down, teleportCurve, 0);
					}
				}
				
				
				lineRenderer.positionCount = points.Count;
				lineRenderer.SetPositions(points.ToArray());
			}
			
			
		}

		public void TeleportTo(Vector3 position, Vector3 direction)
		{
			TeleportTo(position, Quaternion.LookRotation(direction));
		}
		
		public void TeleportTo(Vector3 position, Quaternion rotation)
		{
			float headRotOffset = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(head.transform.forward,Vector3.up), Vector3.up);
			transform.rotation = rotation;
			transform.Rotate(Vector3.up, -headRotOffset);
		
			Vector3 headPosOffset = transform.position - head.transform.position;
			headPosOffset.y = 0;
			transform.position = position + headPosOffset;
		}

		private void RoundVelToZero()
		{
			if (rigRB.velocity.magnitude < minVel)
			{
				rigRB.velocity = Vector3.zero;
			}
		}

		private void SlidingMovement()
		{
			bool useForce = false;
			Vector3 forward = -head.forward;
			forward.y = 0;
			forward.Normalize();

			Vector3 right = new Vector3(-forward.z, 0, forward.x);


			if (useForce)
			{
				Vector3 forwardForce = Time.deltaTime * InputMan.ThumbstickY(Side.Left) * forward * 1000f;
				if (Mathf.Abs(Vector3.Dot(rigRB.velocity, head.forward)) < slidingSpeed)
				{
					rigRB.AddForce(forwardForce);
				}

				Vector3 rightForce = Time.deltaTime * InputMan.ThumbstickX(Side.Left) * right * 1000f;
				if (Mathf.Abs(Vector3.Dot(rigRB.velocity, head.right)) < slidingSpeed)
				{
					rigRB.AddForce(rightForce);
				}
			}
			else
			{
				Vector3 currentSpeed = rigRB.velocity;
				Vector3 forwardSpeed = InputMan.ThumbstickY(Side.Left) * forward;
				Vector3 rightSpeed = InputMan.ThumbstickX(Side.Left) * right;
				Vector3 speed = forwardSpeed + rightSpeed;
				rigRB.velocity = slidingSpeed * speed + (currentSpeed.y * rigRB.transform.up);
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
					if (Vector3.Dot(rigRB.velocity, head.forward) < mainBoosterMaxSpeed)
					{
						// add the force
						rigRB.AddForce(head.forward * mainBoosterForce * 100f);
					}

					mainBoosterBudget -= mainBoosterCost;
				}
			}

			// use main brake
			if (stickBoostBrake && InputMan.PadClick(Side.Right))
			{
				// add a bunch of drag
				rigRB.drag = mainBrakeDrag;
			}
			else if (InputMan.PadClickUp(Side.Right))
			{
				rigRB.drag = normalDrag;
				RoundVelToZero();
			}

			if (handBoosters)
			{
				if (InputMan.SecondaryMenu(Side.Left))
				{
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rigRB.velocity, leftHand.forward) < maxHandBoosterSpeed)
					{
						// add the force
						rigRB.AddForce(leftHand.forward * handBoosterAccel * Time.deltaTime * 100f);
					}
				}

				if (InputMan.SecondaryMenu(Side.Right))
				{
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rigRB.velocity, rightHand.forward) < maxHandBoosterSpeed)
					{
						// add the force
						rigRB.AddForce(rightHand.forward * handBoosterAccel * Time.deltaTime * 100f);
					}
				}
			}
		}

		private void Turn()
		{
			Vector3 pivot = head.position;
			if (grabbingSide == Side.Left)
			{
				pivot = leftHand.position;
			}
			else if (grabbingSide == Side.Right)
			{
				pivot = rightHand.position;
			}

			if (continuousRotation)
			{
				if (yaw && Mathf.Abs(InputMan.ThumbstickX(Side.Right)) > .5f)
				{
					rigRB.transform.RotateAround(pivot, rigRB.transform.up,
						InputMan.ThumbstickX(Side.Right) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (pitch && Mathf.Abs(InputMan.ThumbstickY(Side.Right)) > .5f)
				{
					rigRB.transform.RotateAround(pivot, head.right,
						InputMan.ThumbstickY(Side.Right) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (roll && Mathf.Abs(InputMan.ThumbstickX(Side.Left)) > .5f)
				{
					rigRB.transform.RotateAround(pivot, head.forward,
						InputMan.ThumbstickX(Side.Left) * Time.deltaTime * continuousRotationSpeed * 2);
				}
			}
			else
			{
				if (yaw && InputMan.Left(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, rigRB.transform.up, -snapRotationAmount);
				}
				else if (yaw && InputMan.Right(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, rigRB.transform.up, snapRotationAmount);
				}
				else if (pitch && InputMan.Up(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, head.transform.forward, -snapRotationAmount);
				}
				else if (pitch && InputMan.Down(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, head.transform.forward, snapRotationAmount);
				}
				else if (roll && InputMan.Left(Side.Left))
				{
					rigRB.transform.RotateAround(pivot, head.transform.right, -snapRotationAmount);
				}
				else if (roll && InputMan.Right(Side.Left))
				{
					rigRB.transform.RotateAround(pivot, head.transform.right, snapRotationAmount);
				}
			}
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
				cpt.positionOffset = rigRB.position - hand.position;
				
				InputMan.Vibrate(side, 1);
				
				// if event has subscribers, execute
				OnGrab?.Invoke(parent, side);
			}
			else if (side == grabbingSide)
			{
				if (InputMan.Grip(side))
				{
					cpt.positionOffset = rigRB.position - hand.position;
				}
				else
				{
					if (side == grabbingSide)
					{
						grabbingSide = Side.None;
					}

					if (grabPos != null)
					{
						Destroy(grabPos.gameObject);
						rigRB.velocity = MedianAvg(lastVels);
						RoundVelToZero();
					}
				}
			}
		}

		Vector3 MedianAvg(Vector3[] inputArray)
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