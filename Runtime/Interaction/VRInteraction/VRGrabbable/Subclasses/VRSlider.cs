using System;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	/// <summary>
	/// ↕ Sliders, like legs of tripod, height-adjustable table, etc.
	/// </summary>
	[AddComponentMenu("unityutilities/Interaction/VRSlider")]
	public class VRSlider : VRGrabbable
	{
		private Rigidbody rb;
		[Space]
		public float slideMax;
		public float slideMin;
		public Vector3 localSlideAxis; //localAxis;
		private Vector3 lastGrabPosition;
		public float currentSlidePosition = 0;
		public bool useVelocity;
		public float movementSpeedMultiplier = 1;

		private float vibrationDelta = .02f;
		private float vibrationDeltaSum = 0;

		/// <summary>
		/// float: currentSlideAmount, float: deltaSlide, bool: localInput
		/// </summary>
		public Action<float, float, bool> OnSlide;

		private new void Awake()
		{
			base.Awake();
			OnSlide += SlideAction;
			if (slideMax < slideMin)
			{
				Debug.LogError("Slide min greater than slide max. Do something.", this);
			}
			if (localSlideAxis == Vector3.zero)
			{
				Debug.LogError("Slide axis not set up.", this);
			}
		}


		// Use this for initialization
		void Start()
		{
			localSlideAxis = localSlideAxis.normalized;
			rb = GetComponent<Rigidbody>();
			if (useVelocity && !rb)
			{
				Debug.LogError("Set to use velocity but no rigidbody.", this);
			}
		}

		void Update()
		{
			if (!useVelocity && GrabbedBy != null)
			{
				GrabInput(Time.deltaTime);
			}
		}

		private void FixedUpdate()
		{
			if (useVelocity && GrabbedBy != null)
			{
				GrabInput(Time.fixedDeltaTime);
			}
		}

		void GrabInput(float timeDelta)
		{
			Vector3 currentPosition = GrabbedBy.transform.position;
			Vector3 between = currentPosition - lastGrabPosition;
			Vector3 worldSlideAxis = transform.TransformDirection(localSlideAxis);
			float deltaSlide = Vector3.Dot(between, worldSlideAxis) * movementSpeedMultiplier;

			float nextSlide = currentSlidePosition + deltaSlide;
			if (nextSlide > slideMax)
			{
				nextSlide = slideMax;
				deltaSlide = slideMax - currentSlidePosition;
			}
			else if (nextSlide < slideMin)
			{
				nextSlide = slideMin;
				deltaSlide = slideMin - currentSlidePosition;
			}

			if (deltaSlide != 0)
			{
				SetData(nextSlide, true);
				lastGrabPosition = GrabbedBy.transform.position;
			}
		}

		public void SetData(float updatedSlide, bool localInput = true)
		{

			float slideDifference = updatedSlide - currentSlidePosition;
			currentSlidePosition = updatedSlide;
			transform.Translate(localSlideAxis * slideDifference, Space.Self);


			// vibrate
			// vibrate only when rotated by a certain amount
			if (vibrationDeltaSum > vibrationDelta)
			{
				if (GrabbedBy)
				{
					InputMan.Vibrate(GrabbedBy.side, 1f, .2f);
				}
				vibrationDeltaSum = 0;
			}

			vibrationDeltaSum += Mathf.Abs(slideDifference);


			OnSlide?.Invoke(updatedSlide, slideDifference, localInput);
		}

		public void SlideAction(float currentSlideAmount, float deltaLength, bool localInput)
		{
		}

		public void Beginning()
		{
			SetData(slideMin, true);
		}

		public void End()
		{
			SetData(slideMax, true);
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);
			lastGrabPosition = h.transform.position;
		}

		public override byte[] PackData()
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(outputStream);

				writer.Write(currentSlidePosition);

				return outputStream.ToArray();
			}
		}

		public override void UnpackData(byte[] data)
		{
			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				SetData(reader.ReadSingle(), false);
			}
		}

	}
}