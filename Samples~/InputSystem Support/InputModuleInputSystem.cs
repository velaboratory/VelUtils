#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;

namespace unityutilities
{
	public class InputModuleInputSystem : InputModule
	{
		private InputActions inputActions;
		
		private void Awake()
		{
			inputActions = new InputActions();
		}

		private void OnEnable()
		{
			inputActions.Enable();
		}

		private void OnDisable()
		{
			inputActions.Disable();
		}


		public override void Vibrate(Side side, float intensity, float duration)
		{
			if (side == Side.Both)
			{
				Vibrate(Side.Left, intensity, duration);
				Vibrate(Side.Right, intensity, duration);
			}
			else
			{
				// inputActions.Left.HapticDevice.(side).SendHapticImpulse(0, intensity, duration);
			}
		}


		public override float GetRawValue(InputStrings key, Side side)
		{
			return side switch
			{
				Side.Both => Math.Min(GetRawValue(key, Side.Left), GetRawValue(key, Side.Right)),
				Side.Either => Math.Max(GetRawValue(key, Side.Left), GetRawValue(key, Side.Right)),
				Side.None => 0,
				Side.Left => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Left.Trigger.ReadValue<float>(),
					InputStrings.VR_Grip => inputActions.Left.Grip.ReadValue<float>(),
					InputStrings.VR_Thumbstick_X => inputActions.Left.ThumbstickAxis.ReadValue<Vector2>().x,
					InputStrings.VR_Thumbstick_Y => inputActions.Left.ThumbstickAxis.ReadValue<Vector2>().y,
					InputStrings.VR_Thumbstick_X_Left => Mathf.Clamp01(-inputActions.Left.ThumbstickAxis.ReadValue<Vector2>().x),
					InputStrings.VR_Thumbstick_X_Right => Mathf.Clamp01(inputActions.Left.ThumbstickAxis.ReadValue<Vector2>().x),
					InputStrings.VR_Thumbstick_Y_Up => Mathf.Clamp01(inputActions.Left.ThumbstickAxis.ReadValue<Vector2>().y),
					InputStrings.VR_Thumbstick_Y_Down => Mathf.Clamp01(-inputActions.Left.ThumbstickAxis.ReadValue<Vector2>().y),
					InputStrings.VR_Thumbstick_Press => inputActions.Left.ThumbstickPress.ReadValue<float>(),
					InputStrings.VR_Button1 => inputActions.Left.Button1.ReadValue<float>(),
					InputStrings.VR_Button2 => inputActions.Left.Button2.ReadValue<float>(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				Side.Right => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Right.Trigger.ReadValue<float>(),
					InputStrings.VR_Grip => inputActions.Right.Grip.ReadValue<float>(),
					InputStrings.VR_Thumbstick_X => inputActions.Right.ThumbstickAxis.ReadValue<Vector2>().x,
					InputStrings.VR_Thumbstick_Y => inputActions.Right.ThumbstickAxis.ReadValue<Vector2>().y,
					InputStrings.VR_Thumbstick_X_Left => Mathf.Clamp01(-inputActions.Right.ThumbstickAxis.ReadValue<Vector2>().x),
					InputStrings.VR_Thumbstick_X_Right => Mathf.Clamp01(inputActions.Right.ThumbstickAxis.ReadValue<Vector2>().x),
					InputStrings.VR_Thumbstick_Y_Up => Mathf.Clamp01(inputActions.Right.ThumbstickAxis.ReadValue<Vector2>().y),
					InputStrings.VR_Thumbstick_Y_Down => Mathf.Clamp01(-inputActions.Right.ThumbstickAxis.ReadValue<Vector2>().y),
					InputStrings.VR_Thumbstick_Press => inputActions.Right.ThumbstickPress.ReadValue<float>(),
					InputStrings.VR_Button1 => inputActions.Right.Button1.ReadValue<float>(),
					InputStrings.VR_Button2 => inputActions.Right.Button2.ReadValue<float>(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
			};
		}

		/// <summary>
		/// Returns true if the axis is past the threshold
		/// or true if the button is held down
		/// </summary>
		/// <param name="key"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public override bool GetRaw(InputStrings key, Side side)
		{
			return side switch
			{
				Side.Both => GetRaw(key, Side.Left) && GetRaw(key, Side.Right),
				Side.Either => GetRaw(key, Side.Left) || GetRaw(key, Side.Right),
				Side.None => false,
				Side.Left => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Left.Trigger.IsPressed(),
					InputStrings.VR_Grip => inputActions.Left.Grip.IsPressed(),
					InputStrings.VR_Thumbstick_X => GetRaw(InputStrings.VR_Thumbstick_X_Left, side) || GetRaw(InputStrings.VR_Thumbstick_X_Right, side),
					InputStrings.VR_Thumbstick_Y => GetRaw(InputStrings.VR_Thumbstick_Y_Up, side) || GetRaw(InputStrings.VR_Thumbstick_Y_Down, side),
					InputStrings.VR_Thumbstick_X_Left => inputActions.Left.ThumbstickLeft.IsPressed(),
					InputStrings.VR_Thumbstick_X_Right => inputActions.Left.ThumbstickRight.IsPressed(),
					InputStrings.VR_Thumbstick_Y_Up => inputActions.Left.ThumbstickUp.IsPressed(),
					InputStrings.VR_Thumbstick_Y_Down => inputActions.Left.ThumbstickDown.IsPressed(),
					InputStrings.VR_Thumbstick_Press => inputActions.Left.ThumbstickPress.IsPressed(),
					InputStrings.VR_Button1 => inputActions.Left.Button1.IsPressed(),
					InputStrings.VR_Button2 => inputActions.Left.Button2.IsPressed(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				Side.Right => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Right.Trigger.IsPressed(),
					InputStrings.VR_Grip => inputActions.Right.Grip.IsPressed(),
					InputStrings.VR_Thumbstick_X => GetRaw(InputStrings.VR_Thumbstick_X_Left, side) || GetRaw(InputStrings.VR_Thumbstick_X_Right, side),
					InputStrings.VR_Thumbstick_Y => GetRaw(InputStrings.VR_Thumbstick_Y_Up, side) || GetRaw(InputStrings.VR_Thumbstick_Y_Down, side),
					InputStrings.VR_Thumbstick_X_Left => inputActions.Right.ThumbstickLeft.IsPressed(),
					InputStrings.VR_Thumbstick_X_Right => inputActions.Right.ThumbstickRight.IsPressed(),
					InputStrings.VR_Thumbstick_Y_Up => inputActions.Right.ThumbstickUp.IsPressed(),
					InputStrings.VR_Thumbstick_Y_Down => inputActions.Right.ThumbstickDown.IsPressed(),
					InputStrings.VR_Thumbstick_Press => inputActions.Right.ThumbstickPress.IsPressed(),
					InputStrings.VR_Button1 => inputActions.Right.Button1.IsPressed(),
					InputStrings.VR_Button2 => inputActions.Right.Button2.IsPressed(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
			};
		}

		public override bool GetRawDown(InputStrings key, Side side)
		{
			return side switch
			{
				Side.Both => GetRawDown(key, Side.Left) && GetRawDown(key, Side.Right),
				Side.Either => GetRawDown(key, Side.Left) || GetRawDown(key, Side.Right),
				Side.None => false,
				Side.Left => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Left.Trigger.WasPressedThisFrame(),
					InputStrings.VR_Grip => inputActions.Left.Grip.WasPressedThisFrame(),
					InputStrings.VR_Thumbstick_X => GetRawDown(InputStrings.VR_Thumbstick_X_Left, side) || GetRawDown(InputStrings.VR_Thumbstick_X_Right, side),
					InputStrings.VR_Thumbstick_Y => GetRawDown(InputStrings.VR_Thumbstick_Y_Up, side) || GetRawDown(InputStrings.VR_Thumbstick_Y_Down, side),
					
					InputStrings.VR_Thumbstick_X_Left => inputActions.Left.ThumbstickLeft.WasPerformedThisFrame(),
					InputStrings.VR_Thumbstick_X_Right => inputActions.Left.ThumbstickRight.WasPerformedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Up => inputActions.Left.ThumbstickUp.WasPerformedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Down => inputActions.Left.ThumbstickDown.WasPerformedThisFrame(),
					
					InputStrings.VR_Thumbstick_Press => inputActions.Left.ThumbstickPress.WasPressedThisFrame(),
					InputStrings.VR_Button1 => inputActions.Left.Button1.WasPressedThisFrame(),
					InputStrings.VR_Button2 => inputActions.Left.Button2.WasPressedThisFrame(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				Side.Right => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Right.Trigger.WasPressedThisFrame(),
					InputStrings.VR_Grip => inputActions.Right.Grip.WasPressedThisFrame(),
					InputStrings.VR_Thumbstick_X => GetRawDown(InputStrings.VR_Thumbstick_X_Left, side) || GetRawDown(InputStrings.VR_Thumbstick_X_Right, side),
					InputStrings.VR_Thumbstick_Y => GetRawDown(InputStrings.VR_Thumbstick_Y_Up, side) || GetRawDown(InputStrings.VR_Thumbstick_Y_Down, side),
					
					InputStrings.VR_Thumbstick_X_Left => inputActions.Right.ThumbstickLeft.WasPerformedThisFrame(),
					InputStrings.VR_Thumbstick_X_Right => inputActions.Right.ThumbstickRight.WasPerformedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Up => inputActions.Right.ThumbstickUp.WasPerformedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Down => inputActions.Right.ThumbstickDown.WasPerformedThisFrame(),
					
					InputStrings.VR_Thumbstick_Press => inputActions.Right.ThumbstickPress.WasPressedThisFrame(),
					InputStrings.VR_Button1 => inputActions.Right.Button1.WasPressedThisFrame(),
					InputStrings.VR_Button2 => inputActions.Right.Button2.WasPressedThisFrame(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
			};
		}

		public override bool GetRawUp(InputStrings key, Side side)
		{
			return side switch
			{
				Side.Both => GetRawUp(key, Side.Left) && GetRawUp(key, Side.Right),
				Side.Either => GetRawUp(key, Side.Left) || GetRawUp(key, Side.Right),
				Side.None => false,
				Side.Left => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Left.Trigger.WasReleasedThisFrame(),
					InputStrings.VR_Grip => inputActions.Left.Grip.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_X => GetRawUp(InputStrings.VR_Thumbstick_X_Left, side) || GetRawUp(InputStrings.VR_Thumbstick_X_Right, side),
					InputStrings.VR_Thumbstick_Y => GetRawUp(InputStrings.VR_Thumbstick_Y_Up, side) || GetRawUp(InputStrings.VR_Thumbstick_Y_Down, side),
					InputStrings.VR_Thumbstick_X_Left => inputActions.Left.ThumbstickLeft.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_X_Right => inputActions.Left.ThumbstickRight.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Up => inputActions.Left.ThumbstickUp.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Down => inputActions.Left.ThumbstickDown.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_Press => inputActions.Left.ThumbstickPress.WasReleasedThisFrame(),
					InputStrings.VR_Button1 => inputActions.Left.Button1.WasReleasedThisFrame(),
					InputStrings.VR_Button2 => inputActions.Left.Button2.WasReleasedThisFrame(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				Side.Right => (key) switch
				{
					InputStrings.VR_Trigger => inputActions.Right.Trigger.WasReleasedThisFrame(),
					InputStrings.VR_Grip => inputActions.Right.Grip.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_X => GetRawUp(InputStrings.VR_Thumbstick_X_Left, side) || GetRawUp(InputStrings.VR_Thumbstick_X_Right, side),
					InputStrings.VR_Thumbstick_Y => GetRawUp(InputStrings.VR_Thumbstick_Y_Up, side) || GetRawUp(InputStrings.VR_Thumbstick_Y_Down, side),
					InputStrings.VR_Thumbstick_X_Left => inputActions.Right.ThumbstickLeft.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_X_Right => inputActions.Right.ThumbstickRight.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Up => inputActions.Right.ThumbstickUp.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_Y_Down => inputActions.Right.ThumbstickDown.WasReleasedThisFrame(),
					InputStrings.VR_Thumbstick_Press => inputActions.Right.ThumbstickPress.WasReleasedThisFrame(),
					InputStrings.VR_Button1 => inputActions.Right.Button1.WasReleasedThisFrame(),
					InputStrings.VR_Button2 => inputActions.Right.Button2.WasReleasedThisFrame(),
					_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
				},
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
			};
		}

		public override Vector3 ControllerVelocity(Side side, Space space)
		{
			return (side) switch
			{
				Side.Left => inputActions.Left.Velocity.ReadValue<Vector3>(),
				Side.Right => inputActions.Right.Velocity.ReadValue<Vector3>(),
				_ => Vector3.zero
			};
		}

		public override Vector3 ControllerAngularVelocity(Side side, Space space)
		{
			return (side) switch
			{
				Side.Left => inputActions.Left.AngularVelocity.ReadValue<Vector3>(),
				Side.Right => inputActions.Right.AngularVelocity.ReadValue<Vector3>(),
				_ => Vector3.zero
			};
		}
	}
}
#endif