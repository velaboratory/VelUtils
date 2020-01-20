using System.IO;
using UnityEngine;

namespace unityutilities.VRInteraction
{
	public class TableController : VRGrabbable
	{
		float topHeight;
		[SerializeField] float bottomHeight;
		public float goalHeight;
		private const float movementSpeed = 100;
		private float controllerOffset;
		[HideInInspector] public float lastHapticHeight;
		[SerializeField] float lowestHeight;
		[SerializeField] float highestHeight;

		// Use this for initialization
		void Start()
		{
			topHeight = transform.position.y;
			GoalToLastSaved();
			MoveTo(goalHeight - bottomHeight);
		}

		private void Update()
		{

			if (GrabbedBy != null)
			{
				MoveToWithControllerOffset(GrabbedBy.transform.position.y);
			}

			float increment = .1f;
			float dist = transform.position.y - goalHeight;
			if (dist > increment)
			{
				transform.Translate(0, -Mathf.Clamp(movementSpeed * Time.deltaTime, 0, dist), 0, Space.World);
			}
			else if (dist < -increment)
			{
				transform.Translate(0, -Mathf.Clamp(movementSpeed * Time.deltaTime, 0, dist), 0, Space.World);
			}

			goalHeight = Mathf.Clamp(goalHeight, lowestHeight, highestHeight);

		}

		private void MoveTo(float goalHeight)
		{
			transform.Translate(0, goalHeight - transform.position.y + bottomHeight, 0, Space.World);// = new Vector3(transform.position.x, goalHeight, transform.position.x);
			this.goalHeight = goalHeight + bottomHeight;
		}

		public void MoveToWithControllerOffset(float goalHeight)
		{
			MoveTo(controllerOffset + goalHeight - bottomHeight);
		}

		public void UpdateControllerOffset(float controllerHeight)
		{
			controllerOffset = transform.position.y - controllerHeight;
		}

		public void MoveBy(float distance)
		{
			goalHeight += distance;
			PlayerPrefs.SetFloat("tableHeight", goalHeight);
		}


		public void Top()
		{
			goalHeight = topHeight;
		}

		public void GoalToLastSaved()
		{
			goalHeight = PlayerPrefs.GetFloat("tableHeight");
		}

		public void Bottom()
		{
			goalHeight = bottomHeight;
		}

		public override void HandleGrab(VRGrabbableHand h)
		{
			base.HandleGrab(h);
			UpdateControllerOffset(h.transform.position.y);

		}

		public override byte[] PackData()
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(outputStream);

				writer.Write(goalHeight);

				return outputStream.ToArray();
			}
		}

		public override void UnpackData(byte[] data)
		{
			using (MemoryStream inputStream = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(inputStream);

				goalHeight = reader.ReadSingle();
			}
		}
	}
}