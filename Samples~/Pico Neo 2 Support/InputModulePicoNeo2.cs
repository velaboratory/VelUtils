using UnityEngine;
using Pvr_UnitySDKAPI;
using System.Collections.Generic;
using static VelUtils.InputMan;

namespace VelUtils
{
	public class InputModulePicoNeo2 : InputModule
	{
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

		public static List<InputStrings> keys = new List<InputStrings> {
			InputStrings.VR_Trigger,
			InputStrings.VR_Grip,
			InputStrings.VR_Thumbstick_Press,
			InputStrings.VR_Button1,
			InputStrings.VR_Button2
		};

		private void Awake()
		{
			firstPressed.Add(InputStrings.VR_Thumbstick_X_Left, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_X_Right, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_Y_Up, new bool[2, 3]);
			firstPressed.Add(InputStrings.VR_Thumbstick_Y_Down, new bool[2, 3]);

			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_X_Left, new float[] { 0, 0 });
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_X_Right, new float[] { 0, 0 });
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_Y_Up, new float[] { 0, 0 });
			directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_Y_Down, new float[] { 0, 0 });
		}

		public override void Vibrate(Side side, float intensity, float duration)
		{
			if (side == Side.Both)
			{
				Controller.UPvr_VibrateNeo2Controller(intensity, (int)(1000 * duration), 0);
				Controller.UPvr_VibrateNeo2Controller(intensity, (int)(1000 * duration), 1);
			}
			else if (side == Side.Left || side == Side.Right)
			{
				Controller.UPvr_VibrateNeo2Controller(intensity, (int)(1000 * duration), (int)side);
			}
			else
			{
				Debug.LogError("Can't vibrate on that side", this);
			}
		}

		public override float GetRawValue(InputStrings key, Side side)
		{
			if (side == Side.None)
			{
				return 0;
			}
			else if (side == Side.Right || side == Side.Left)
			{
				if (key == InputStrings.VR_Trigger)
				{
					return Controller.UPvr_GetControllerTriggerValue((int)side) / 255f;
				}
				else if (key == InputStrings.VR_Thumbstick_X)
				{
					return Controller.UPvr_GetAxis2D((int)side).x;
				}
				else if (key == InputStrings.VR_Thumbstick_Y)
				{
					return Controller.UPvr_GetAxis2D((int)side).y;
				}
				else
				{
					Debug.LogError("Not yet implemented");  // TODO
					return 0;
				}
			}
			else if (side == Side.Both)
			{
				return Mathf.Min(GetRawValue(key, Side.Left), GetRawValue(key, Side.Right));
			}
			else if (side == Side.Either)
			{
				return Mathf.Max(GetRawValue(key, Side.Left), GetRawValue(key, Side.Right));
			}
			else
			{
				Debug.LogError("Wrong side");
				return 0;
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
			if (side == Side.None)
			{
				return false;
			}
			else if (side == Side.Left || side == Side.Right)
			{
				if (key == InputStrings.VR_Thumbstick_X)
				{
					return Mathf.Abs(Controller.UPvr_GetAxis2D((int)side).x) > thumbstickThreshold;
				}
				else if (key == InputStrings.VR_Thumbstick_X_Left)
				{
					return Controller.UPvr_GetAxis2D((int)side).x < -thumbstickThreshold;
				}
				else if (key == InputStrings.VR_Thumbstick_X_Right)
				{
					return Controller.UPvr_GetAxis2D((int)side).x > thumbstickThreshold;
				}
				else if (key == InputStrings.VR_Thumbstick_Y)
				{
					return Mathf.Abs(Controller.UPvr_GetAxis2D((int)side).y) > thumbstickThreshold;
				}
				else if (key == InputStrings.VR_Thumbstick_Y_Up)
				{
					return Controller.UPvr_GetAxis2D((int)side).y > thumbstickThreshold;
				}
				else if (key == InputStrings.VR_Thumbstick_Y_Down)
				{
					return Controller.UPvr_GetAxis2D((int)side).y < -thumbstickThreshold;
				}
				else if (keys.Contains(key))
				{
					return Controller.UPvr_GetKey((int)side, InputStringsToPvrKey(key, side));
				}
				else
				{
					Debug.LogError("Wrong input");
					return false;
				}

			}
			else if (side == Side.Both)
			{
				return GetRaw(key, Side.Left) && GetRaw(key, Side.Right);
			}
			else if (side == Side.Either)
			{
				return GetRaw(key, Side.Left) || GetRaw(key, Side.Right);
			}
			else
			{
				Debug.LogError("Wrong side");
				return false;
			}
		}

		public override bool GetRawDown(InputStrings key, Side side)
		{
			if (side == Side.None)
			{
				return false;
			}
			else if (side == Side.Left || side == Side.Right)
			{
				// if this key is a button
				if (keys.Contains(key))
				{
					return Controller.UPvr_GetKeyDown((int)side, InputStringsToPvrKey(key, side));
				}
				else if (key.IsAxis())
				{
					return firstPressed[key][(int)side, 0];
				}
				else
				{
					Debug.LogError("Wrong input");
					return false;
				}
			}
			else if (side == Side.Both)
			{
				return (GetRaw(key, Side.Left) && GetRawDown(key, Side.Right)) || (GetRawDown(key, Side.Left) && GetRaw(key, Side.Right));
			}
			else if (side == Side.Either)
			{
				return GetRawDown(key, Side.Left) || GetRawDown(key, Side.Right);
			}
			else
			{
				Debug.LogError("Wrong side");
				return false;
			}
		}

		public override bool GetRawUp(InputStrings key, Side side)
		{
			if (side == Side.None)
			{
				return false;
			}
			else if (side == Side.Left || side == Side.Right)
			{
				// if this key is a button
				if (keys.Contains(key))
				{
					return Controller.UPvr_GetKeyUp((int)side, InputStringsToPvrKey(key, side));
				}
				else if (key.IsAxis())
				{
					return firstPressed[key][(int)side, 1];
				}
				else
				{
					Debug.LogError("Wrong input");
					return false;
				}

			}
			else if (side == Side.Both)
			{
				return GetRaw(key, Side.Left) && GetRaw(key, Side.Right);
			}
			else if (side == Side.Either)
			{
				return GetRaw(key, Side.Left) || GetRaw(key, Side.Right);
			}
			else
			{
				Debug.LogError("Wrong side");
				return false;
			}
		}

		public override Vector3 ControllerVelocity(Side side, Space space)
		{
			Vector3 vel;
			if (space == Space.World)
			{
				// TODO convert to global space from tracking volume
				vel = Controller.UPvr_GetVelocity((int)side) / 1000f;
			}
			else
			{
				vel = Controller.UPvr_GetVelocity((int)side) / 1000f;
			}

			return vel;
		}

		public override Vector3 ControllerAngularVelocity(Side side, Space space)
		{
			Vector3 vel;
			if (space == Space.World)
			{
				// TODO convert to world space
				vel = Controller.UPvr_GetAngularVelocity((int)side);
			}
			else
			{
				vel = Controller.UPvr_GetAngularVelocity((int)side);
			}
			return vel;
		}

		private static Pvr_KeyCode InputStringsToPvrKey(InputStrings key, Side side)
		{
			if (side == Side.Left || side == Side.Right)
			{
				switch (key)
				{
					case InputStrings.VR_Trigger:
						return Pvr_KeyCode.TRIGGER;
					case InputStrings.VR_Grip:
						return side == Side.Left ? Pvr_KeyCode.Left : Pvr_KeyCode.Right;
					case InputStrings.VR_Button1:
						return side == Side.Left ? Pvr_KeyCode.X : Pvr_KeyCode.A;
					case InputStrings.VR_Button2:
						return side == Side.Left ? Pvr_KeyCode.Y : Pvr_KeyCode.B;
					case InputStrings.VR_Thumbstick_Press:
						return Pvr_KeyCode.TOUCHPAD;
				}
			}

			Debug.LogError("Must specify left or right");
			return Pvr_KeyCode.APP;

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
				UpdateDictionaryDirection(ThumbstickX((Side)i) < -thumbstickThreshold, i, InputStrings.VR_Thumbstick_X_Left);
				UpdateDictionaryDirection(ThumbstickX((Side)i) > thumbstickThreshold, i, InputStrings.VR_Thumbstick_X_Right);
				UpdateDictionaryDirection(ThumbstickY((Side)i) > thumbstickThreshold, i, InputStrings.VR_Thumbstick_Y_Up);
				UpdateDictionaryDirection(ThumbstickY((Side)i) < -thumbstickThreshold, i, InputStrings.VR_Thumbstick_Y_Down);
			}
		}

	}
}
