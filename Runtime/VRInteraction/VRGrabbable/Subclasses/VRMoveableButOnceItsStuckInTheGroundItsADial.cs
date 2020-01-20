using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class VRMoveableButOnceItsStuckInTheGroundItsADial : VRGrabbable
	{
		public Vector3 dialAxis = Vector3.forward;
		private Rigidbody rb;
		public Quaternion lastGrabbedRotation;
		public Vector3 lastGrabbedPosition;
		private Vector3 posOffset;
		private Quaternion rotationOffset;
		public delegate void MovedHandler(bool localInput);
		public delegate void ReleaseHandler();
		public event MovedHandler Moved = delegate { };
		public event ReleaseHandler Released = delegate { };
		public float multiplier = 1;
		[Tooltip("How much to use position of hand rather than rotation")]
		[Range(0, 1)]
		public float positionMix = 0;
		[Tooltip("Whether to vary the position mix with distance to the center of the object")]
		public bool dynamicPositionMix = false;
		public float dynamicPositionMixDistanceMultiplier = 10f;

		private float initialHandToGroundDistance;
		private bool stuckInTheGround;

		//[HideInInspector]
		public float currentAngle;


		public AudioSource lift;
		public AudioSource place;


		// Use this for initialization
		private void Start()
		{
			rb = GetComponent<Rigidbody>();
		}

		private void Update()
		{

			if (grabbedBy != null)
			{
				GrabInput();
			}

		}

		private void GrabInput()
		{
			#region Stick and Unstick into the ground
			if (!stuckInTheGround)
			{
				// Check if stuck into the ground
				if (Physics.Raycast(transform.position + transform.up, -transform.up, out RaycastHit hit))
				{

					if (hit.distance < 1f)
					{
						stuckInTheGround = true;
						initialHandToGroundDistance = Vector3.Distance(transform.position, grabbedBy.position);
						place.Play();
					}
				}
			}
			else
			{
				// Check if lifted out of the ground
				if (Vector3.Distance(transform.position, grabbedBy.position) > initialHandToGroundDistance + .1f)
				{
					stuckInTheGround = false;
					lift.Play();
				}
			}
			#endregion

			#region Move
			if (stuckInTheGround)
			{

				transform.LookAt(grabbedBy.position, grabbedBy.forward);
				transform.Rotate(90, 0, 0, Space.Self);

				lastGrabbedPosition = grabbedBy.position;
			}

			else
			{

				transform.rotation = grabbedBy.rotation * rotationOffset;
				transform.position = grabbedBy.TransformPoint(posOffset);

				lastGrabbedRotation = grabbedBy.rotation;
				lastGrabbedPosition = grabbedBy.position;

			}

			Moved(true);
			#endregion

		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			if (grabbedBy != null)
			{
				HandleRelease();
			}
			base.HandleGrab(h);

			lastGrabbedRotation = grabbedBy.rotation;
			lastGrabbedPosition = grabbedBy.position;
			posOffset = grabbedBy.InverseTransformPoint(transform.position);
			rotationOffset = Quaternion.Inverse(grabbedBy.rotation) * transform.rotation;

			if (stuckInTheGround)
			{
				initialHandToGroundDistance = Vector3.Distance(transform.position, grabbedBy.position);
			}
		}

		public override int HandleRelease(VRGrabbableHand h = null)
		{

			//if has inertia, keep turning, otherwise, stop
			base.HandleRelease(h);
			HandleDeselection();

			return 0;
		}

		public override byte[] PackData()
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(outputStream);

				writer.Write(transform.localPosition);
				writer.Write(transform.localRotation);

				return outputStream.ToArray();
			}
		}

		public override void UnpackData(byte[] data)
		{
			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				transform.localPosition = reader.ReadVector3();
				transform.localRotation = reader.ReadQuaternion();

				Moved?.Invoke(false);
			}
		}
	}
}