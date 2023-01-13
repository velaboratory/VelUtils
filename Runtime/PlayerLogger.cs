using System.Collections.Generic;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Logs position data from a VR rig using Logger.cs every frame
	/// </summary>
	public class PlayerLogger : MonoBehaviour
	{
		public Rig rig;
		public Movement m;
		[Space] private const string positionsFileName = "positions";
		private const string inputsFileName = "inputs";
		private const string movementFileName = "movement";
		[Space] public float updateRateHz = 4;

		private float nextUpdateTime;

		public bool logInputs;

		// Start is called before the first frame update
		void Start()
		{
			m.TeleportStart += TeleportStart;
			m.TeleportEnd += TeleportEnd;
			m.SnapTurn += SnapTurn;
		}

		void Update()
		{
			// if we are due for an update
			if (Time.time < nextUpdateTime) return;

			// set the next update time
			nextUpdateTime += 1f / updateRateHz;

			// if we are still behind, we missed an update - just reset
			if (Time.time > nextUpdateTime)
			{
				Debug.Log("Missed a log cycle", this);
				nextUpdateTime = Time.time + 1f / updateRateHz;
			}


			List<string> positions = new List<string>();

			// tracking space pos
			Vector3 spacePos = rig.transform.position;
			positions.Add(spacePos.x.ToString());
			positions.Add(spacePos.y.ToString());
			positions.Add(spacePos.z.ToString());
			positions.Add(rig.transform.eulerAngles.y.ToString());

			// local space of head and hands
			Vector3 headPos = rig.head.position;
			Vector3 headForward = rig.head.forward;
			Vector3 headUp = rig.head.up;
			Vector3 headMoment = rig.head.rotation.ToMomentVector();
			positions.Add(headPos.x.ToString());
			positions.Add(headPos.y.ToString());
			positions.Add(headPos.z.ToString());
			positions.Add(headForward.x.ToString());
			positions.Add(headForward.y.ToString());
			positions.Add(headForward.z.ToString());
			positions.Add(headUp.x.ToString());
			positions.Add(headUp.y.ToString());
			positions.Add(headUp.z.ToString());
			positions.Add(headMoment.x.ToString());
			positions.Add(headMoment.y.ToString());
			positions.Add(headMoment.z.ToString());

			Vector3 leftHandPos = rig.leftHand.position;
			Vector3 leftHandForward = rig.leftHand.forward;
			Vector3 leftHandUp = rig.leftHand.up;
			Vector3 leftHandMoment = rig.leftHand.rotation.ToMomentVector();
			positions.Add(leftHandPos.x.ToString());
			positions.Add(leftHandPos.y.ToString());
			positions.Add(leftHandPos.z.ToString());
			positions.Add(leftHandForward.x.ToString());
			positions.Add(leftHandForward.y.ToString());
			positions.Add(leftHandForward.z.ToString());
			positions.Add(leftHandUp.x.ToString());
			positions.Add(leftHandUp.y.ToString());
			positions.Add(leftHandUp.z.ToString());
			positions.Add(leftHandMoment.x.ToString());
			positions.Add(leftHandMoment.y.ToString());
			positions.Add(leftHandMoment.z.ToString());

			Vector3 rightHandPos = rig.rightHand.position;
			Vector3 rightHandForward = rig.rightHand.forward;
			Vector3 rightHandUp = rig.rightHand.up;
			Vector3 rightHandMoment = rig.rightHand.rotation.ToMomentVector();
			positions.Add(rightHandPos.x.ToString());
			positions.Add(rightHandPos.y.ToString());
			positions.Add(rightHandPos.z.ToString());
			positions.Add(rightHandForward.x.ToString());
			positions.Add(rightHandForward.y.ToString());
			positions.Add(rightHandForward.z.ToString());
			positions.Add(rightHandUp.x.ToString());
			positions.Add(rightHandUp.y.ToString());
			positions.Add(rightHandUp.z.ToString());
			positions.Add(rightHandMoment.x.ToString());
			positions.Add(rightHandMoment.y.ToString());
			positions.Add(rightHandMoment.z.ToString());

			Logger.LogRow(positionsFileName, positions);


			if (logInputs)
			{
				List<string> inputs = new List<string>
				{
					// trigger
					InputMan.Trigger(Side.Left).ToString(),
					InputMan.Trigger(Side.Right).ToString(),
					InputMan.TriggerValue(Side.Left).ToString(),
					InputMan.TriggerValue(Side.Right).ToString(),
					// grip
					InputMan.Grip(Side.Left).ToString(),
					InputMan.Grip(Side.Right).ToString(),
					InputMan.GripValue(Side.Left).ToString(),
					InputMan.GripValue(Side.Right).ToString(),
					// buttons
					InputMan.Button1(Side.Left).ToString(),
					InputMan.Button1(Side.Right).ToString(),
					InputMan.Button2(Side.Left).ToString(),
					InputMan.Button2(Side.Right).ToString(),
					// thumbstick
					InputMan.Up(Side.Left).ToString(),
					InputMan.Up(Side.Right).ToString(),
					InputMan.Down(Side.Left).ToString(),
					InputMan.Down(Side.Right).ToString(),
					InputMan.Left(Side.Left).ToString(),
					InputMan.Left(Side.Right).ToString(),
					InputMan.Right(Side.Left).ToString(),
					InputMan.Right(Side.Right).ToString(),
					InputMan.ThumbstickX(Side.Left).ToString(),
					InputMan.ThumbstickX(Side.Right).ToString(),
					InputMan.ThumbstickY(Side.Left).ToString(),
					InputMan.ThumbstickY(Side.Right).ToString(),
					InputMan.ThumbstickPress(Side.Left).ToString(),
					InputMan.ThumbstickPress(Side.Right).ToString()
				};

				Logger.LogRow(inputsFileName, inputs);
			}
		}


		void TeleportStart(Side side)
		{
			List<string> movement = new List<string>
			{
				"teleport-start",
				side.ToString()
			};

			Logger.LogRow(movementFileName, movement);
		}

		void TeleportEnd(Side side, float time, Vector3 translation)
		{
			List<string> movement = new List<string>
			{
				"teleport-end",
				side.ToString(),
				time.ToString(),
				translation.x.ToString(),
				translation.y.ToString(),
				translation.z.ToString()
			};

			Logger.LogRow(movementFileName, movement);
		}

		void SnapTurn(Side side, string direction)
		{
			List<string> movement = new List<string>
			{
				"snap-turn",
				side.ToString(),
				direction
			};

			Logger.LogRow(movementFileName, movement);
		}
	}

	public static class PlayerLoggerExtensionMethods
	{
		public static Vector3 ToMomentVector(this Quaternion value)
		{
			value.ToAngleAxis(out float angle, out Vector3 axis);
			return axis * angle;
		}
	}
}