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
		[Header("Object References")] public InputMan inputMan;
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


		void Start()
		{
			cpt = GetComponent<CopyTransform>();
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
				Vector3 forwardForce = Time.deltaTime * inputMan.ThumbstickY(Side.Left) * forward * 1000f;
				if (Mathf.Abs(Vector3.Dot(rigRB.velocity, head.forward)) < slidingSpeed)
				{
					rigRB.AddForce(forwardForce);
				}

				Vector3 rightForce = Time.deltaTime * inputMan.ThumbstickX(Side.Left) * right * 1000f;
				if (Mathf.Abs(Vector3.Dot(rigRB.velocity, head.right)) < slidingSpeed)
				{
					rigRB.AddForce(rightForce);
				}
			}
			else
			{
				Vector3 currentSpeed = rigRB.velocity;
				Vector3 forwardSpeed = inputMan.ThumbstickY(Side.Left) * forward;
				Vector3 rightSpeed = inputMan.ThumbstickX(Side.Left) * right;
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
			if (stickBoostBrake && inputMan.ThumbstickPressDown(Side.Left) && inputMan.ThumbstickIdle(Side.Left))
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
			if (stickBoostBrake && inputMan.PadClick(Side.Right))
			{
				// add a bunch of drag
				rigRB.drag = mainBrakeDrag;
			}
			else if (inputMan.PadClickUp(Side.Right))
			{
				rigRB.drag = normalDrag;
				RoundVelToZero();
			}

			if (handBoosters)
			{
				if (inputMan.SecondaryMenu(Side.Left))
				{
					// TODO speed can be faster by spherical Pythagorus
					// limit max speed
					if (Vector3.Dot(rigRB.velocity, leftHand.forward) < maxHandBoosterSpeed)
					{
						// add the force
						rigRB.AddForce(leftHand.forward * handBoosterAccel * Time.deltaTime * 100f);
					}
				}

				if (inputMan.SecondaryMenu(Side.Right))
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
				if (yaw && Mathf.Abs(inputMan.ThumbstickX(Side.Right)) > .5f)
				{
					rigRB.transform.RotateAround(pivot, rigRB.transform.up,
						inputMan.ThumbstickX(Side.Right) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (pitch && Mathf.Abs(inputMan.ThumbstickY(Side.Right)) > .5f)
				{
					rigRB.transform.RotateAround(pivot, head.right,
						inputMan.ThumbstickY(Side.Right) * Time.deltaTime * continuousRotationSpeed * 2);
				}
				else if (roll && Mathf.Abs(inputMan.ThumbstickX(Side.Left)) > .5f)
				{
					rigRB.transform.RotateAround(pivot, head.forward,
						inputMan.ThumbstickX(Side.Left) * Time.deltaTime * continuousRotationSpeed * 2);
				}
			}
			else
			{
				if (yaw && inputMan.Left(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, rigRB.transform.up, -snapRotationAmount);
				}
				else if (yaw && inputMan.Right(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, rigRB.transform.up, snapRotationAmount);
				}
				else if (pitch && inputMan.Up(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, head.transform.forward, -snapRotationAmount);
				}
				else if (pitch && inputMan.Down(Side.Right))
				{
					rigRB.transform.RotateAround(pivot, head.transform.forward, snapRotationAmount);
				}
				else if (roll && inputMan.Left(Side.Left))
				{
					rigRB.transform.RotateAround(pivot, head.transform.right, -snapRotationAmount);
				}
				else if (roll && inputMan.Right(Side.Left))
				{
					rigRB.transform.RotateAround(pivot, head.transform.right, snapRotationAmount);
				}
			}
		}

		private void GrabMove(ref Transform hand, ref GameObject grabPos, Side side, Transform parent = null)
		{
			if (inputMan.GripDown(side) || (inputMan.Grip(side) && 
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
				
				// if event has subscribers, execute
				OnGrab?.Invoke(parent, side);
			}
			else if (side == grabbingSide)
			{
				if (inputMan.Grip(side))
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