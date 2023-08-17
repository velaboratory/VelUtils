﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace VelUtils
{
	public enum HeadsetSystem
	{
		None,
		Oculus,
		SteamVR,
		Pico
	}

	public enum HeadsetControllerLayout // TODO not used or set yet
	{
		None,
		Thumbstick,
		Touchpad
	}

	public enum HeadsetControllerStyle
	{
		None,
		Rift,
		RiftSQuest,
		Quest2,
		Vive,
		Index,
		WMR,
		QuestHands,
		Pico
	}

	/// <summary>
	/// Both and None are not supported by most operations
	/// </summary>
	public enum Side
	{
		Left,
		Right,
		Both,
		Either,
		None
	}

	public static class SideMethods
	{
		public static bool Contains(this Side s, Side other)
		{
			switch (s)
			{
				case Side.Left:
					return other == Side.Left;
				case Side.Right:
					return other == Side.Right;
				case Side.Both:
					return other == Side.Both;
				case Side.Either:
					return other == Side.Left || other == Side.Right || other == Side.Both;
				case Side.None:
					return false;
				default:
					return false;
			}
		}

		/// <summary>
		/// Gets the "other" side from the current value
		/// Only works for Left and Right
		/// </summary>
		public static Side OtherSide(this Side side)
		{
			switch (side)
			{
				case Side.Left:
					return Side.Right;
				case Side.Right:
					return Side.Left;
				default:
					return Side.None;
			}
		}

		public static Side AddOption(this Side side, Side other)
		{
			// this wouldn't make sense
			if (other == Side.Both)
			{
				throw new Exception();
			}
			// if we are actually adding an option to the left side
			else if (side == Side.Left &&
			         (other == Side.Right || other == Side.Either))
			{
				return Side.Either;
			}
			// if we are actually adding an option to the right side
			else if (side == Side.Right &&
			         (other == Side.Left || other == Side.Either))
			{
				return Side.Either;
			}
			// just use the new value
			else if (side == Side.None)
			{
				return other;
			}
			// nothing has changed
			else
			{
				return side;
			}
		}

		public static Side SubtractOption(this Side side, Side other)
		{
			// this wouldn't make sense
			if (other == Side.Both)
			{
				throw new Exception();
			}
			// if we are actually subtracting the left side
			else if (side == Side.Either && other == Side.Left)
			{
				return Side.Right;
			}
			// if we are actually subtracting the right side
			else if (side == Side.Either && other == Side.Right)
			{
				return Side.Left;
			}
			else if (
				(side == Side.Left && other == Side.Left) ||
				(side == Side.Right && other == Side.Right))
			{
				return Side.None;
			}
			// nothing has changed
			else
			{
				return side;
			}
		}
	}

	public enum Axis
	{
		X,
		Y
	}

	public enum VRInput
	{
		None,
		Trigger,
		Grip
	}

	public enum InputStrings
	{
		VR_Trigger,
		VR_Grip,
		VR_Thumbstick_X,
		VR_Thumbstick_Y,
		VR_Thumbstick_X_Left,
		VR_Thumbstick_X_Right,
		VR_Thumbstick_Y_Up,
		VR_Thumbstick_Y_Down,
		VR_Thumbstick_Press,
		VR_Button1,
		VR_Button2,
		VR_Menu,
	}

	public static class InputStringsMethods
	{
		public static bool IsAxis(this InputStrings key)
		{
			switch (key)
			{
				case InputStrings.VR_Trigger:
				case InputStrings.VR_Grip:
				case InputStrings.VR_Thumbstick_X:
				case InputStrings.VR_Thumbstick_Y:
				case InputStrings.VR_Thumbstick_X_Left:
				case InputStrings.VR_Thumbstick_X_Right:
				case InputStrings.VR_Thumbstick_Y_Up:
				case InputStrings.VR_Thumbstick_Y_Down:
					return true;
				case InputStrings.VR_Thumbstick_Press:
				case InputStrings.VR_Button1:
				case InputStrings.VR_Button2:
				case InputStrings.VR_Menu:
					return false;
				default:
					return false;
			}
		}

		public static bool IsThumbstickAxis(this InputStrings key)
		{
			switch (key)
			{
				case InputStrings.VR_Thumbstick_X:
				case InputStrings.VR_Thumbstick_Y:
				case InputStrings.VR_Thumbstick_X_Left:
				case InputStrings.VR_Thumbstick_X_Right:
				case InputStrings.VR_Thumbstick_Y_Up:
				case InputStrings.VR_Thumbstick_Y_Down:
					return true;
				case InputStrings.VR_Trigger:
				case InputStrings.VR_Grip:
				case InputStrings.VR_Thumbstick_Press:
				case InputStrings.VR_Button1:
				case InputStrings.VR_Button2:
				case InputStrings.VR_Menu:
					return false;
				default:
					return false;
			}
		}

		public static bool IsDoubleSidedAxis(this InputStrings key)
		{
			switch (key)
			{
				case InputStrings.VR_Thumbstick_X:
				case InputStrings.VR_Thumbstick_Y:
					return true;
				case InputStrings.VR_Thumbstick_X_Left:
				case InputStrings.VR_Thumbstick_X_Right:
				case InputStrings.VR_Thumbstick_Y_Up:
				case InputStrings.VR_Thumbstick_Y_Down:
				case InputStrings.VR_Trigger:
				case InputStrings.VR_Grip:
				case InputStrings.VR_Thumbstick_Press:
				case InputStrings.VR_Button1:
				case InputStrings.VR_Button2:
				case InputStrings.VR_Menu:
					return false;
				default:
					return false;
			}
		}
	}

	/// <summary>
	/// Makes input from VR devices accessible from a unified set of methods. Can treat axes as button down.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	[AddComponentMenu("VelUtils/InputMan")]
	public class InputMan : MonoBehaviour
	{
		public InputModule inputModule;

		public static HeadsetSystem headsetSystem;
		public static HeadsetControllerLayout controllerLayout;
		public static HeadsetControllerStyle controllerStyle;


		public static Side DominantHand { get; set; }

		public static Side NonDominantHand
		{
			get
			{
				switch (DominantHand)
				{
					case Side.Left:
						return Side.Right;
					case Side.Right:
						return Side.Left;
					default:
						Debug.LogError("No dominant side selected");
						return Side.None;
				}
			}
		}

		private List<InputDevice> devices = new List<InputDevice>();


		private static InputMan instance;
		protected static bool init;

		protected static void Init()
		{
			if (!init)
			{
				instance = new GameObject("InputMan").AddComponent<InputMan>();
				instance.inputModule = instance.gameObject.AddComponent<InputModuleUnity>();
				DontDestroyOnLoad(instance.gameObject);
				init = true;
			}
		}

		private void Awake()
		{
			instance = this;
			init = true;

			UpdateInputDevices();
		}

		private void UpdateInputDevices()
		{
			InputDevices.GetDevices(devices);
			
			Debug.Log($"Number of connected devices: {devices.Count}");

			string xrDeviceName = XRSettings.loadedDeviceName;

			string modelName = SystemInfo.deviceModel; // e.g. Oculus Quest
			string deviceName = SystemInfo.deviceName; // e.g. Oculus Quest 2

			// if (!string.IsNullOrEmpty(modelName))
			{
				Debug.Log($"[UU] Loaded device: {XRSettings.loadedDeviceName}", instance);
			}

			controllerLayout = HeadsetControllerLayout.Thumbstick; // TODO correctly assign this

			if (modelName == "Oculus Quest")
			{
				headsetSystem = HeadsetSystem.Oculus;
			}

			if (deviceName == "Oculus Quest 2")
			{
				controllerStyle = HeadsetControllerStyle.Quest2;
			}
			else if (xrDeviceName.Contains("oculus") || deviceName == "Oculus Quest 1") // TODO Quest 1 id?
			{
				headsetSystem = HeadsetSystem.Oculus;
				controllerStyle = HeadsetControllerStyle.RiftSQuest;
				// TODO detect if using hands
			}
			else if (xrDeviceName.Contains("Vive"))
			{
				headsetSystem = HeadsetSystem.SteamVR;
				controllerStyle = HeadsetControllerStyle.Vive;
			}
			else if (xrDeviceName.Contains("Mixed") || xrDeviceName.Contains("WMR"))
			{
				headsetSystem = HeadsetSystem.SteamVR;
				controllerStyle = HeadsetControllerStyle.WMR;
			}
			
		}

		// TODO implement
		public static bool UserPresent()
		{
			return true;
		}

		#region Generic Input

		public static bool Get(VRInput input, Side side = Side.Either)
		{
			switch (input)
			{
				case VRInput.Trigger:
					return Trigger(side);
				case VRInput.Grip:
					return Grip(side);
				default:
					return false;
			}
		}

		public static bool GetDown(VRInput input, Side side = Side.Either)
		{
			switch (input)
			{
				case VRInput.Trigger:
					return TriggerDown(side);
				case VRInput.Grip:
					return GripDown(side);
				default:
					return false;
			}
		}

		public static bool GetUp(VRInput input, Side side = Side.Either)
		{
			switch (input)
			{
				case VRInput.Trigger:
					return TriggerUp(side);
				case VRInput.Grip:
					return GripUp(side);
				default:
					return false;
			}
		}

		#endregion

		#region Trigger

		public static float TriggerValue(Side side = Side.Either)
		{
			return instance.inputModule.GetRawValue(InputStrings.VR_Trigger, side);
		}

		public static bool Trigger(Side side = Side.Either)
		{
			return instance.inputModule.GetRaw(InputStrings.VR_Trigger, side);
		}

		public static bool TriggerDown(Side side = Side.Either)
		{
			return instance.inputModule.GetRawDown(InputStrings.VR_Trigger, side);
		}

		public static bool TriggerUp(Side side = Side.Either)
		{
			return instance.inputModule.GetRawUp(InputStrings.VR_Trigger, side);
		}

		public static float MainTriggerValue()
		{
			return TriggerValue(DominantHand);
		}

		public static bool MainTrigger()
		{
			return Trigger(DominantHand);
		}

		public static bool MainTriggerDown()
		{
			return TriggerDown(DominantHand);
		}

		public static bool MainTriggerUp()
		{
			return TriggerUp(DominantHand);
		}

		public static float SecondaryTriggerValue()
		{
			return TriggerValue(NonDominantHand);
		}

		public static bool SecondaryTrigger()
		{
			return Trigger(NonDominantHand);
		}

		public static bool SecondaryTriggerDown()
		{
			return TriggerDown(NonDominantHand);
		}

		public static bool SecondaryTriggerUp()
		{
			return TriggerUp(NonDominantHand);
		}

		#endregion

		#region Grip

		public static float GripValue(Side side = Side.Either)
		{
			return instance.inputModule.GetRawValue(InputStrings.VR_Grip, side);
		}

		public static bool Grip(Side side = Side.Either)
		{
			return instance.inputModule.GetRaw(InputStrings.VR_Grip, side);
		}

		public static bool GripDown(Side side = Side.Either)
		{
			return instance.inputModule.GetRawDown(InputStrings.VR_Grip, side);
		}

		public static bool GripUp(Side side = Side.Either)
		{
			return instance.inputModule.GetRawUp(InputStrings.VR_Grip, side);
		}

		#endregion

		#region Thumbstick/Touchpad

		// TODO both should be one held down while other is pressed.
		public static bool ThumbstickPress(Side side = Side.Either)
		{
			return instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Press, side);
		}

		public static bool ThumbstickPressDown(Side side = Side.Either)
		{
			return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Press, side);
		}

		public static bool ThumbstickPressUp(Side side = Side.Either)
		{
			return instance.inputModule.GetRawUp(InputStrings.VR_Thumbstick_Press, side);
		}

		public static bool MainThumbstickPress()
		{
			return ThumbstickPress(DominantHand);
		}

		public static bool MainThumbstickPressDown()
		{
			return ThumbstickPressDown(DominantHand);
		}

		public static bool MainThumbstickPressUp()
		{
			return ThumbstickPressUp(DominantHand);
		}

		public static bool SecondaryThumbstickPress()
		{
			return ThumbstickPress(NonDominantHand);
		}

		public static bool SecondaryThumbstickPressDown()
		{
			return ThumbstickPressDown(NonDominantHand);
		}

		public static bool SecondaryThumbstickPressUp()
		{
			return ThumbstickPressUp(NonDominantHand);
		}

		public static bool ThumbstickIdle(Side side, Axis axis)
		{
			if (axis == Axis.X)
			{
				return ThumbstickIdleX(side);
			}

			if (axis == Axis.Y)
			{
				return ThumbstickIdleY(side);
			}

			Debug.LogError("More axes than possible.");
			return false;
		}

		public static bool ThumbstickIdleX(Side side = Side.Either)
		{
			return !instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X, side);
		}

		public static bool ThumbstickIdleY(Side side = Side.Either)
		{
			return !instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y, side);
		}

		public static bool ThumbstickIdle(Side side = Side.Either)
		{
			return ThumbstickIdleX(side) && ThumbstickIdleY(side);
		}

		public static float Thumbstick(Side side, Axis axis)
		{
			if (axis == Axis.X)
			{
				return ThumbstickX(side);
			}

			if (axis == Axis.Y)
			{
				return ThumbstickY(side);
			}

			Debug.LogError("More axes than possible.");
			return 0;
		}

		public static float ThumbstickX(Side side = Side.Either)
		{
			return instance.inputModule.GetRawValue(InputStrings.VR_Thumbstick_X, side);
		}

		public static float ThumbstickY(Side side = Side.Either)
		{
			return instance.inputModule.GetRawValue(InputStrings.VR_Thumbstick_Y, side);
		}

		// aux methods for pad
		public static bool PadClickDown(Side side)
		{
			return ThumbstickPressDown(side);
		}

		public static bool PadIdleX(Side side)
		{
			return ThumbstickIdleX(side);
		}

		public static bool PadIdleY(Side side)
		{
			return ThumbstickIdleY(side);
		}

		public static bool PadIdle(Side side)
		{
			return ThumbstickIdle(side);
		}

		public static bool PadClick(Side side)
		{
			return ThumbstickPress(side);
		}

		public static bool PadClickUp(Side side)
		{
			return ThumbstickPressUp(side);
		}

		public static float PadX(Side side)
		{
			return ThumbstickX(side);
		}

		public static float PadY(Side side)
		{
			return ThumbstickY(side);
		}

		#endregion

		#region Menu buttons

		/// <summary>
		/// A or X on Oculus controllers.
		/// True when the button is held down
		/// </summary>
		/// <param name="side">Which controller</param>
		public static bool Button1(Side side = Side.Either)
		{
			return instance.inputModule.GetRaw(InputStrings.VR_Button1, side);
		}

		public static bool Button1Down(Side side = Side.Either)
		{
			return instance.inputModule.GetRawDown(InputStrings.VR_Button1, side);
		}

		public static bool Button1Up(Side side = Side.Either)
		{
			return instance.inputModule.GetRawUp(InputStrings.VR_Button1, side);
		}

		/// <summary>
		/// B or Y on Oculus controllers.
		/// True when the button is held down
		/// </summary>
		/// <param name="side">Which controller</param>
		public static bool Button2(Side side = Side.Either)
		{
			return instance.inputModule.GetRaw(InputStrings.VR_Button2, side);
		}

		public static bool Button2Down(Side side = Side.Either)
		{
			return instance.inputModule.GetRawDown(InputStrings.VR_Button2, side);
		}

		public static bool Button2Up(Side side = Side.Either)
		{
			return instance.inputModule.GetRawUp(InputStrings.VR_Button2, side);
		}


		public static bool MenuButton(Side side = Side.Either)
		{
			return instance.inputModule.GetRaw(InputStrings.VR_Menu, side);
		}

		public static bool MenuButtonDown(Side side = Side.Either)
		{
			return instance.inputModule.GetRawDown(InputStrings.VR_Menu, side);
		}

		public static bool MenuButtonUp(Side side = Side.Either)
		{
			return instance.inputModule.GetRawUp(InputStrings.VR_Menu, side);
		}

		#endregion

		#region Directions

		public static bool Up(Side side = Side.Either)
		{
			if (!init) Init();

			if (side == Side.Both)
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return (instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Up, Side.Right)
					        && instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Up, Side.Left)) ||
					       (instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Up, Side.Right)
					        && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Up, Side.Left));
				}
				else
				{
					return instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Up, Side.Right)
					       && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Up, Side.Left) && (ThumbstickPress(Side.Both));
				}
			}
			else
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Up, side);
				}
				else
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Up, side) && ThumbstickPress(side);
				}
			}
		}

		public static bool Down(Side side = Side.Either)
		{
			if (!init) Init();

			if (side == Side.Both)
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return (instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Down, Side.Right)
					        && instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Down, Side.Left)) ||
					       (instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Down, Side.Right)
					        && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Down, Side.Left));
				}
				else
				{
					return instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Down, Side.Right)
					       && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_Y_Down, Side.Left) && (ThumbstickPress(Side.Both));
				}
			}
			else
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Down, side);
				}
				else
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_Y_Down, side) && ThumbstickPress(side);
				}
			}
		}

		public static bool Left(Side side = Side.Either)
		{
			if (!init) Init();

			if (side == Side.Both)
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return (instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Left, Side.Right)
					        && instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Left, Side.Left)) ||
					       (instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Left, Side.Right)
					        && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Left, Side.Left));
				}
				else
				{
					return instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Left, Side.Right)
					       && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Left, Side.Left) && (ThumbstickPress(Side.Both));
				}
			}
			else
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Left, side);
				}
				else
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Left, side) && ThumbstickPress(side);
				}
			}
		}

		public static bool Right(Side side = Side.Either)
		{
			if (!init) Init();

			if (side == Side.Both)
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return (instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Right, Side.Right)
					        && instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Right, Side.Left)) ||
					       (instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Right, Side.Right)
					        && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Right, Side.Left));
				}
				else
				{
					return instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Right, Side.Right)
					       && instance.inputModule.GetRaw(InputStrings.VR_Thumbstick_X_Right, Side.Left) && (ThumbstickPress(Side.Both));
				}
			}
			else
			{
				if (controllerLayout == HeadsetControllerLayout.Thumbstick)
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Right, side);
				}
				else
				{
					return instance.inputModule.GetRawDown(InputStrings.VR_Thumbstick_X_Right, side) && ThumbstickPress(side);
				}
			}
		}

		public static float Vertical(Side side = Side.Either)
		{
			if (controllerLayout == HeadsetControllerLayout.Thumbstick)
			{
				return ThumbstickY(side);
			}

			if (ThumbstickPress())
			{
				return ThumbstickY(side);
			}

			return 0;
		}

		public static float Horizontal(Side side = Side.Either)
		{
			if (controllerLayout == HeadsetControllerLayout.Thumbstick)
			{
				return ThumbstickX(side);
			}

			if (ThumbstickPress())
			{
				return ThumbstickX(side);
			}

			return 0;
		}

		#endregion

		#region Vibrations

		/// <summary>
		/// Vibrate the controller
		/// </summary>
		/// <param name="side">Which controller to vibrate</param>
		/// <param name="intensity">Intensity from 0 to 1</param>
		/// <param name="duration">Duration of the vibration (s)</param>
		/// <param name="delay">Time before the vibration starts</param>
		public static void Vibrate(Side side, float intensity, float duration = .025f, float delay = 0)
		{
			if (!init) Init();

			if (delay > 0 && instance)
			{
				instance.StartVibrateDelay(side, intensity, duration, delay);
				return;
			}


			intensity = Mathf.Clamp01(intensity);

			instance.inputModule.Vibrate(side, intensity, duration);
		}

		private void StartVibrateDelay(Side side, float intensity, float duration, float delay)
		{
			StartCoroutine(VibrateDelay(side, intensity, duration, delay));
		}

		private IEnumerator VibrateDelay(Side side, float intensity, float duration, float delay)
		{
			yield return new WaitForSeconds(delay);
			Vibrate(side, intensity, duration);
		}

		#endregion

		#region Controller Velocity

		/// <summary>
		/// Gets the controller velocity. Only works for local space for now.
		/// </summary>
		/// <param name="side">Which controller</param>
		/// <param name="space">Local or global space of the tracking volume</param>
		/// <returns></returns>
		public static Vector3 ControllerVelocity(Side side, Space space = Space.Self)
		{
			return instance.inputModule.ControllerVelocity(side, space);
		}

		/// <summary>
		/// Gets the controller angular velocity. Only works for local space for now.
		/// </summary>
		/// <param name="side">Which controller</param>
		/// <param name="space">Local or global space of the tracking volume</param>
		/// <returns></returns>
		public static Vector3 ControllerAngularVelocity(Side side, Space space = Space.Self)
		{
			return instance.inputModule.ControllerAngularVelocity(side, space);
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(InputMan))]
	[CanEditMultipleObjects]
	public class InputManEditor : Editor
	{
		InputMan inputMan;

		private void OnEnable()
		{
			inputMan = target as InputMan;
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Use this script to get input from VR Controllers.");
			EditorGUILayout.Space();

			base.OnInspectorGUI();

			if (inputMan.inputModule == null)
			{
				EditorGUILayout.HelpBox("No input module. Please add one.", MessageType.Error);
				if (GUILayout.Button("Add Default Input Module"))
				{
					inputMan.inputModule = inputMan.gameObject.AddComponent<InputModuleUnity>();
				}
			}
		}
	}
#endif
}