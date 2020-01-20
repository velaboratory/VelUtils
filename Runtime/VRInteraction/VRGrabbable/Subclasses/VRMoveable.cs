using System;
using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class VRMoveable : VRGrabbable
	{
		private Rigidbody rb;

		/// <summary>
		/// bool: localInput
		/// </summary>
		public Action<bool> Moved;
		public Transform centerOfMass;

		public AudioSource collisionSound;
		public bool useVelocity;
		private bool wasKinematic;

		// Use this for initialization
		private void Start()
		{
			rb = GetComponent<Rigidbody>();
			wasKinematic = rb.isKinematic;
		}

		// Update is called once per frame
		private void FixedUpdate()
		{
		}

		private void Update()
		{
			if (grabbedBy != null)
			{
				Moved?.Invoke(true);
			}

			if (!locallyOwned)
			{
				rb.isKinematic = true;
			}
			else
			{
				rb.isKinematic = wasKinematic;
			}
		}

		public override void HandleGrab(VRGrabbableHand h)
		{

			if (grabbedBy != null)
			{
				HandleRelease();
			}
			base.HandleGrab(h);

			Grabbed?.Invoke();

		}

		public override int HandleRelease(VRGrabbableHand h = null)
		{

			if (h == null)
			{
				Debug.LogError("HAND NULL ON RELEASE. FIX");
			}

			//if has inertia, keep moving, otherwise, stop
			if (!rb.isKinematic)
			{
				//rb.velocity = 1.5f * AvgOfVector3s(h.lastVels);


				if (!useVelocity)
				{
					//rb.angularVelocity = h.transform.rotation * InputMan.ControllerAngularVelocity(h.side);
				}
			}

			base.HandleRelease(h);
			HandleDeselection();

			Released?.Invoke();

			return 0;
		}

		Vector3 AvgOfVector3s(Vector3[] list)
		{
			Vector3 total = new Vector3();
			foreach (var item in list)
			{
				total += item;
			}
			return total / list.Length;
		}

		private void OnCollisionEnter(Collision other)
		{
			if (collisionSound && Time.timeSinceLevelLoad > 1)
			{
				float volume = other.relativeVelocity.magnitude / 8f;
				volume = Mathf.Clamp01(volume);
				//collisionSound.volume = volume;
				//Debug.Log("Collision at: " + other.relativeVelocity.magnitude);
				collisionSound.Play();

			}
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