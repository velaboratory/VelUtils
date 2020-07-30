using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;
using static unityutilities.InputMan;

namespace unityutilities
{
	public class InputModuleUnity : InputModule
	{

		private static readonly Dictionary<InputStrings, string[]> inputManagerStrings = new Dictionary<InputStrings, string[]>() {
			{InputStrings.VR_Trigger, new[] {"VR_Trigger_Left", "VR_Trigger_Right"}},
			{InputStrings.VR_Grip, new[] {"VR_Grip_Left", "VR_Grip_Right"}},
			{InputStrings.VR_Thumbstick_X, new[] {"VR_Thumbstick_X_Left", "VR_Thumbstick_X_Right"}},
			{InputStrings.VR_Thumbstick_Y, new[] {"VR_Thumbstick_Y_Left", "VR_Thumbstick_Y_Right"}},
			{InputStrings.VR_Thumbstick_X_Left, new[] {"VR_Thumbstick_X_Left_Left", "VR_Thumbstick_X_Left_Right"}},
			{InputStrings.VR_Thumbstick_X_Right, new[] {"VR_Thumbstick_X_Right_Left", "VR_Thumbstick_X_Right_Right"}},
			{InputStrings.VR_Thumbstick_Y_Up, new[] {"VR_Thumbstick_Y_Up_Left", "VR_Thumbstick_Y_Up_Right"}},
			{InputStrings.VR_Thumbstick_Y_Down, new[] {"VR_Thumbstick_Y_Down_Left", "VR_Thumbstick_Y_Down_Right"}},
			{InputStrings.VR_Thumbstick_Press, new[] {"VR_Thumbstick_Press_Left", "VR_Thumbstick_Press_Right"}},
			{InputStrings.VR_Button1, new[] {"VR_Button1_Left", "VR_Button1_Right"}},
			{InputStrings.VR_Button2, new[] {"VR_Button2_Left", "VR_Button2_Right"}}
		};

		private void Awake()
		{
			firstPressed = new Dictionary<InputStrings, bool[,]>();
			firstPressed.Add(InputStrings.VR_Trigger, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Grip, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_X_Left, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_X_Right, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_Y_Up, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_Y_Down, new bool[2, 3]);

			directionalTimeoutValue = new Dictionary<InputStrings, float[]>();
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_X_Left, new float[] { 0, 0 });
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_X_Right, new float[] { 0, 0 });
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_Y_Up, new float[] { 0, 0 });
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_Y_Down, new float[] { 0, 0 });
		}

		#region Navigation Vars

		/// <summary>
		/// Contains a pair of bools for each axis input that can act as a button.
		/// The first is true only for the first frame the axis is active
		/// The second is true only for the first frame the axis is inactive
		/// The third remains true when the button is held.
		/// 	it represents whether the button was already down last frame
		/// </summary>
		protected static Dictionary<InputStrings, bool[,]> firstPressed = new Dictionary<InputStrings, bool[,]>();

		// the distance necessary to count as a "press"
		public static float triggerThreshold = .5f;
		public static float gripThreshold = .5f;
		public static float touchpadThreshold = .5f;
		public static float thumbstickThreshold = .5f;
		public static float thumbstickIdleThreshold = .1f;
		public static float directionalTimeout = 1f;

		private static Dictionary<InputStrings, float[]> directionalTimeoutValue = new Dictionary<InputStrings, float[]>();

		#endregion

		private static InputDevice GetXRNode(Side side)
		{
			return InputDevices.GetDeviceAtXRNode(side == Side.Left ? XRNode.LeftHand : XRNode.RightHand);
		}

		private static XRNodeState GetXRNodeState(Side side)
		{
			List<XRNodeState> nodes = new List<XRNodeState>();
			InputTracking.GetNodeStates(nodes);

			foreach (XRNodeState ns in nodes)
			{
				if (side == Side.Left && ns.nodeType == XRNode.LeftHand)
				{
					return ns;
				}

				if (side == Side.Right && ns.nodeType == XRNode.RightHand)
				{
					return ns;
				}
			}
			if (nodes != null && nodes.Count > 0)
			{
				return nodes[0];
			}
			else
			{
				return new XRNodeState();
			}
		}

		public override void Vibrate(Side side, float intensity, float duration)
		{
			if (side == Side.Both)
			{
				GetXRNode(Side.Left).SendHapticImpulse(0, intensity, duration);
				GetXRNode(Side.Right).SendHapticImpulse(0, intensity, duration);
			}
			else
			{
				GetXRNode(side).SendHapticImpulse(0, intensity, duration);
			}
		}


		public override float GetRawValue(InputStrings key, Side side)
		{
			float left, right;
			switch (side)
			{
				case Side.Both:
					left = Input.GetAxis(inputManagerStrings[key][(int)Side.Left]);
					right = Input.GetAxis(inputManagerStrings[key][(int)Side.Right]);
					return Mathf.Abs(left) < Mathf.Abs(right) ? left : right;
				case Side.Either:
					left = Input.GetAxis(inputManagerStrings[key][(int)Side.Left]);
					right = Input.GetAxis(inputManagerStrings[key][(int)Side.Right]);
					return Mathf.Abs(left) > Mathf.Abs(right) ? left : right;
				case Side.None:
					return 0;
				default:
					return Input.GetAxis(inputManagerStrings[key][(int)side]);
			}
		}

		/// <summary>
		/// Returns true if the axis is past the threshold
		/// </summary>
		/// <param name="key"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public override bool GetRaw(InputStrings key, Side side)
		{
			if (key.IsAxis())
			{
				var threshold = key.IsThumbstickAxis() ? thumbstickThreshold : triggerThreshold;
				var doubleSided = key.IsDoubleSidedAxis();
				bool left, right;

				var leftValue = Input.GetAxis(inputManagerStrings[key][(int)Side.Left]);
				var rightValue = Input.GetAxis(inputManagerStrings[key][(int)Side.Right]);
				switch (side)
				{
					case Side.Both:
						if (doubleSided)
						{
							left = Mathf.Abs(leftValue) > threshold;
							right = Mathf.Abs(rightValue) > threshold;
						}
						else
						{
							left = leftValue > threshold;
							right = rightValue > threshold;
						}
						return left && right;
					case Side.Either:
						if (doubleSided)
						{
							left = Mathf.Abs(leftValue) > threshold;
							right = Mathf.Abs(rightValue) > threshold;
						}
						else
						{
							left = leftValue > threshold;
							right = rightValue > threshold;
						}
						return left || right;
					case Side.None:
						return false;
					default:
						if (doubleSided)
						{
							return Mathf.Abs(Input.GetAxis(inputManagerStrings[key][(int)side])) > threshold;
						}
						else
						{
							return Input.GetAxis(inputManagerStrings[key][(int)side]) > threshold;
						}
				}
			}
			else
			{
				switch (side)
				{
					case Side.Both:
						return Input.GetButton(inputManagerStrings[key][(int)Side.Left]) &&
							   Input.GetButton(inputManagerStrings[key][(int)Side.Right]);
					case Side.Either:
						return Input.GetButton(inputManagerStrings[key][(int)Side.Left]) ||
							   Input.GetButton(inputManagerStrings[key][(int)Side.Right]);
					case Side.None:
						return false;
					default:
						return Input.GetButton(inputManagerStrings[key][(int)side]);
				}
			}
		}

		public override bool GetRawDown(InputStrings key, Side side)
		{
			if (key.IsAxis())
			{
				switch (side)
				{
					case Side.Both:
						return firstPressed[key][0, 0] &&
							   firstPressed[key][1, 0];
					case Side.Either:
						return firstPressed[key][0, 0] ||
							   firstPressed[key][1, 0];
					case Side.None:
						return false;
					default:
						return firstPressed[key][(int)side, 0];
				}
			}
			else
			{
				switch (side)
				{
					case Side.Both:
						return Input.GetButtonDown(inputManagerStrings[key][(int)Side.Left]) &&
							   Input.GetButtonDown(inputManagerStrings[key][(int)Side.Right]);
					case Side.Either:
						return Input.GetButtonDown(inputManagerStrings[key][(int)Side.Left]) ||
							   Input.GetButtonDown(inputManagerStrings[key][(int)Side.Right]);
					case Side.None:
						return false;
					default:
						return Input.GetButtonDown(inputManagerStrings[key][(int)side]);
				}
			}
		}

		public override bool GetRawUp(InputStrings key, Side side)
		{
			if (key.IsAxis())
			{
				switch (side)
				{
					case Side.Both:
						return firstPressed[key][0, 1] &&
							   firstPressed[key][1, 1];
					case Side.Either:
						return firstPressed[key][0, 1] ||
							   firstPressed[key][1, 1];
					case Side.None:
						return false;
					default:
						return firstPressed[key][(int)side, 1];
				}
			}
			else
			{
				switch (side)
				{
					case Side.Both:
						return Input.GetButtonUp(inputManagerStrings[key][(int)Side.Left]) &&
							   Input.GetButtonUp(inputManagerStrings[key][(int)Side.Right]);
					case Side.Either:
						return Input.GetButtonUp(inputManagerStrings[key][(int)Side.Left]) ||
							   Input.GetButtonUp(inputManagerStrings[key][(int)Side.Right]);
					case Side.None:
						return false;
					default:
						return Input.GetButtonUp(inputManagerStrings[key][(int)side]);
				}
			}
		}

		public override Vector3 ControllerVelocity(Side side, Space space)
		{
			Vector3 vel;
			if (space == Space.World)
			{
				// TODO convert to global space from tracking volume
				GetXRNodeState(side).TryGetVelocity(out vel);
			}
			else
			{
				GetXRNodeState(side).TryGetVelocity(out vel);
			}

			return vel;
		}

		public override Vector3 ControllerAngularVelocity(Side side, Space space)
		{
			Vector3 vel;
			if (space == Space.World)
			{
				GetXRNodeState(side).TryGetAngularVelocity(out vel);
			}
			else
			{
				// TODO convert to local space
				GetXRNodeState(side).TryGetAngularVelocity(out vel);
			}
			return vel;
		}

		private void UpdateDictionary(bool currentVal, int side, InputStrings key)
		{
			// if down right now
			if (currentVal)
			{
				// if it wasn't down last frame
				if (!firstPressed[key][side, 2] && !firstPressed[key][side, 0])
				{
					// activate the down event 
					firstPressed[key][side, 0] = true;
				}
				else
				{
					// deactive the down event
					firstPressed[key][side, 0] = false;
				}
				// save that the input was down for next frame
				firstPressed[key][side, 2] = true;
			}
			// if up right now
			else
			{
				// if it wasn't up last frame
				if (firstPressed[key][side, 2] && !firstPressed[key][side, 1])
				{
					// activate the up event
					firstPressed[key][side, 1] = true;
				}
				else
				{
					// deactivate the up event
					firstPressed[key][side, 1] = false;
				}
				// save that the input was up for next frame
				firstPressed[key][side, 2] = false;

				firstPressed[key][side, 0] = false;
			}
		}

		private void UpdateDictionaryDirection(bool currentVal, int side, InputStrings key)
		{
			if (currentVal)
			{
				if (directionalTimeoutValue[key][side] > directionalTimeout)
				{
					firstPressed[key][side, 1] = false;
					directionalTimeoutValue[key][side] = 0;
				}

				//directionalTimeoutValue[key][side] += Time.deltaTime;
			}
			else
			{
				directionalTimeoutValue[key][side] = 0;
			}

			UpdateDictionary(currentVal, side, key);
		}

		private void Update()
		{

			for (int i = 0; i < 2; i++)
			{
				UpdateDictionary(Trigger((Side)i), i, InputStrings.VR_Trigger);
				UpdateDictionary(Grip((Side)i), i, InputStrings.VR_Grip);


				UpdateDictionaryDirection(ThumbstickX((Side)i) < -thumbstickThreshold, i, InputStrings.VR_Thumbstick_X_Left);
				UpdateDictionaryDirection(ThumbstickX((Side)i) > thumbstickThreshold, i, InputStrings.VR_Thumbstick_X_Right);
				UpdateDictionaryDirection(ThumbstickY((Side)i) < -thumbstickThreshold, i, InputStrings.VR_Thumbstick_Y_Up);
				UpdateDictionaryDirection(ThumbstickY((Side)i) > thumbstickThreshold, i, InputStrings.VR_Thumbstick_Y_Down);
			}
		}
	}
}