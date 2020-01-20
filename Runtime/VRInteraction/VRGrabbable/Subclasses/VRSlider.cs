using System;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	[RequireComponent(typeof(Rigidbody))]
	public class VRSlider : VRGrabbable
	{

		public Rigidbody rb;
		public float slideMax;
		public float slideMin;
		public Vector3 localSlideAxis; //localAxis;
		public Vector3 lastGrabPosition;
		public float currentSlideAmount = 0;
		public float slideScale = 1;

		/// <summary>
		/// float: currentSlideAmount, float: deltaSlide, bool: localInput
		/// </summary>
		public Action<float, float, bool> OnSlide;


		// Use this for initialization
		void Start()
		{
			localSlideAxis = localSlideAxis.normalized;
			rb = this.GetComponent<Rigidbody>();
		}

		void Update()
		{
			grabInput();
		}

		void grabInput()
		{
			if (grabbedBy != null)
			{
				Vector3 currentPosition = grabbedBy.position;
				Vector3 between = currentPosition - lastGrabPosition;
				Vector3 worldSlideAxis = transform.localToWorldMatrix.MultiplyVector(localSlideAxis);
				float deltaSlide = Vector3.Dot(between, worldSlideAxis) * slideScale;

				float nextSlide = currentSlideAmount + deltaSlide;
				if (nextSlide > slideMax)
				{
					nextSlide = slideMax;
					deltaSlide = slideMax - currentSlideAmount;
				}
				else if (nextSlide < slideMin)
				{
					nextSlide = slideMin;
					deltaSlide = slideMin - currentSlideAmount;
				}

				if (deltaSlide != 0)
				{
					SetData(nextSlide, true);
					lastGrabPosition = grabbedBy.position;
				}

			}
		}

		public void SetData(float updatedSlide, bool localInput)
		{

			float slideDifference = updatedSlide - currentSlideAmount;
			currentSlideAmount = updatedSlide;
			OnSlide(updatedSlide, slideDifference, localInput);
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);
			lastGrabPosition = h.transform.position;
		}

		public override int HandleRelease(VRGrabbableHand h = null)
		{
			base.HandleRelease(h);

			return 0;
		}

		public override byte[] PackData()
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(outputStream);

				writer.Write(currentSlideAmount);

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