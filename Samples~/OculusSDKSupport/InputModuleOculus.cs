using UnityEngine;
using static unityutilities.InputMan;

namespace unityutilities
{
	public class InputModuleOculus : InputModule
	{
		float[] vibrationTimeLeft = { -2, -2 };

		public override void Vibrate(Side side, float intensity, float duration)
		{
			if (side == Side.Both)
			{
				OVRInput.SetControllerVibration(1, intensity, OVRInput.Controller.LTouch);
				vibrationTimeLeft[0] = duration;
				OVRInput.SetControllerVibration(1, intensity, OVRInput.Controller.RTouch);
				vibrationTimeLeft[1] = duration;
			}
			else if (side == Side.Left || side == Side.Right)
			{
				OVRInput.SetControllerVibration(1, intensity, Side2OVRController(side));
				vibrationTimeLeft[side == Side.Left ? 0 : 1] = duration;
			}
			else
			{
				Debug.LogError("Can't vibrate on that side", this);
			}
		}

		private void FixedUpdate()
		{
			for (int i = 0; i < 2; i++)
			{
				if (vibrationTimeLeft[i] > 0)
				{
					vibrationTimeLeft[i] -= Time.fixedDeltaTime;
				}
				else if (vibrationTimeLeft[i] > -2f)
				{
					OVRInput.SetControllerVibration(0, 0, i == 0 ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
					vibrationTimeLeft[i] = -2;
				}
			}
		}


		public override float GetRawValue(InputStrings key, Side side)
		{
			if (side == Side.None) return 0;

			float left = 0, right = 0;
			if (side != Side.Right)
			{
				left = OVRInput.Get(InputStringsToOVRInputAxis1D(key), Side2OVRController(Side.Left));
			}
			if (side != Side.Left)
			{
				right = OVRInput.Get(InputStringsToOVRInputAxis1D(key), Side2OVRController(Side.Right));
			}

			switch (side)
			{
				case Side.Both:
					return Mathf.Min(left, right);
				case Side.Either:
					return Mathf.Max(left, right);
				case Side.Left:
					return left;
				case Side.Right:
					return right;
				default:
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
			if (side == Side.None) return false;

			bool left = false, right = false;
			if (side != Side.Right)
			{
				if (key == InputStrings.VR_Thumbstick_X)
				{
					left =
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_X_Left), Side2OVRController(Side.Left)) ||
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_X_Right), Side2OVRController(Side.Left));
				}
				else if (key == InputStrings.VR_Thumbstick_Y)
				{
					left =
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_Y_Up), Side2OVRController(Side.Left)) ||
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_Y_Down), Side2OVRController(Side.Left));
				}
				else
				{
					left = OVRInput.Get(InputStringsToOVRInputButton(key), Side2OVRController(Side.Left));
				}
			}
			if (side != Side.Left)
			{
				if (key == InputStrings.VR_Thumbstick_X)
				{
					right =
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_X_Left), Side2OVRController(Side.Right)) ||
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_X_Right), Side2OVRController(Side.Right));
				}
				else if (key == InputStrings.VR_Thumbstick_Y)
				{
					right =
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_Y_Up), Side2OVRController(Side.Right)) ||
						OVRInput.Get(InputStringsToOVRInputButton(InputStrings.VR_Thumbstick_Y_Down), Side2OVRController(Side.Right));
				}
				else
				{
					right = OVRInput.Get(InputStringsToOVRInputButton(key), Side2OVRController(Side.Right));
				}
			}

			switch (side)
			{
				case Side.Both:
					return left && right;
				case Side.Either:
					return left || right;
				case Side.Left:
					return left;
				case Side.Right:
					return right;
				default:
					return false;
			}
		}

		public override bool GetRawDown(InputStrings key, Side side)
		{
			if (side == Side.None) return false;

			bool left = false, right = false, leftHold = false, rightHold = false;
			if (side != Side.Right)
			{
				left = OVRInput.GetDown(InputStringsToOVRInputButton(key), Side2OVRController(Side.Left));
			}
			if (side != Side.Left)
			{
				right = OVRInput.GetDown(InputStringsToOVRInputButton(key), Side2OVRController(Side.Right));
			}
			if (side == Side.Both)
			{
				leftHold = OVRInput.Get(InputStringsToOVRInputButton(key), Side2OVRController(Side.Left));
				rightHold = OVRInput.Get(InputStringsToOVRInputButton(key), Side2OVRController(Side.Right));
			}

			switch (side)
			{
				case Side.Both:
					return (leftHold && rightHold) && (left || right);
				case Side.Either:
					return left || right;
				case Side.Left:
					return left;
				case Side.Right:
					return right;
				default:
					return false;
			}
		}

		public override bool GetRawUp(InputStrings key, Side side)
		{
			if (side == Side.None) return false;

			bool left = false, right = false, leftHold = false, rightHold = false;
			if (side != Side.Right)
			{
				left = OVRInput.GetUp(InputStringsToOVRInputButton(key), Side2OVRController(Side.Left));
			}
			if (side != Side.Left)
			{
				right = OVRInput.GetUp(InputStringsToOVRInputButton(key), Side2OVRController(Side.Right));
			}
			if (side == Side.Both)
			{
				leftHold = !OVRInput.Get(InputStringsToOVRInputButton(key), Side2OVRController(Side.Left));
				rightHold = !OVRInput.Get(InputStringsToOVRInputButton(key), Side2OVRController(Side.Right));
			}

			switch (side)
			{
				case Side.Both:
					return (leftHold && rightHold) && (left || right);
				case Side.Either:
					return left || right;
				case Side.Left:
					return left;
				case Side.Right:
					return right;
				default:
					return false;
			}
		}

		public override Vector3 ControllerVelocity(Side side, Space space)
		{
			Vector3 vel;
			if (space == Space.World)
			{
				// TODO convert to global space from tracking volume
				vel = OVRInput.GetLocalControllerVelocity(Side2OVRController(side));
			}
			else
			{
				vel = OVRInput.GetLocalControllerVelocity(Side2OVRController(side));
			}

			return vel;
		}

		public override Vector3 ControllerAngularVelocity(Side side, Space space)
		{
			Vector3 vel;
			if (space == Space.World)
			{
				// TODO convert to world space
				vel = OVRInput.GetLocalControllerAngularVelocity(Side2OVRController(side));
			}
			else
			{
				vel = OVRInput.GetLocalControllerAngularVelocity(Side2OVRController(side));
			}
			return vel;
		}

		private static Side OVRController2Side(OVRInput.Controller controller)
		{
			switch (controller)
			{
				case OVRInput.Controller.LTouch:
					return Side.Left;
				case OVRInput.Controller.RTouch:
					return Side.Right;
				case OVRInput.Controller.None:
					return Side.None;
				case OVRInput.Controller.All:
					return Side.Both;
				default:
					return Side.None;
			}
		}

		private static OVRInput.Controller Side2OVRController(Side side)
		{
			switch (side)
			{
				case Side.Left:
					return OVRInput.Controller.LTouch;
				case Side.Right:
					return OVRInput.Controller.RTouch;
				case Side.None:
					return OVRInput.Controller.None;
				case Side.Both:
					return OVRInput.Controller.All;
				default:
					return OVRInput.Controller.None;
			}
		}

		private static OVRInput.Button InputStringsToOVRInputButton(InputStrings key)
		{
			switch (key)
			{
				case InputStrings.VR_Trigger:
					return OVRInput.Button.PrimaryIndexTrigger;
				case InputStrings.VR_Grip:
					return OVRInput.Button.PrimaryHandTrigger;
				case InputStrings.VR_Button1:
					return OVRInput.Button.One;
				case InputStrings.VR_Button2:
					return OVRInput.Button.Two;
				case InputStrings.VR_Thumbstick_X_Left:
					return OVRInput.Button.PrimaryThumbstickLeft;
				case InputStrings.VR_Thumbstick_X_Right:
					return OVRInput.Button.PrimaryThumbstickRight;
				case InputStrings.VR_Thumbstick_Y_Up:
					return OVRInput.Button.PrimaryThumbstickUp;
				case InputStrings.VR_Thumbstick_Y_Down:
					return OVRInput.Button.PrimaryThumbstickDown;
				case InputStrings.VR_Thumbstick_Press:
					return OVRInput.Button.PrimaryThumbstick;
			}

			return OVRInput.Button.None;
		}

		private static OVRInput.Axis1D InputStringsToOVRInputAxis1D(InputStrings key)
		{
			switch (key)
			{
				case InputStrings.VR_Trigger:
					return OVRInput.Axis1D.PrimaryIndexTrigger;
				case InputStrings.VR_Grip:
					return OVRInput.Axis1D.PrimaryHandTrigger;
				default:
					return OVRInput.Axis1D.None;
			}
		}

	}
}