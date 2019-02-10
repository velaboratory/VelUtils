#undef STEAMVR_AVAILABLE // change to #define or #undef if SteamVR utilites are installed
#define OCULUS_UTILITES_AVAILABLE

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if STEAMVR_AVAILABLE
using Valve.VR;
#endif

public enum HeadsetType
{
	none,
	Rift,
	Vive,
	WMR
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

public enum Axis
{
	X,
	Y
}

public enum VRPackage
{
	none,
	SteamVR,
	Oculus
}

/// <summary>
/// Makes input from VR devices accessible from a unified set of methods. Can treat axes as button down.
/// </summary>
public class InputMan : MonoBehaviour
{
	private static HeadsetType headsetType;
	private static VRPackage VRPackageInUse;

	public static Side DominantHand { get; set; }

	public static Side NonDominantHand
	{
		get
		{
			if (DominantHand == Side.Left)
			{
				return Side.Right;
			}
			else
			{
				return Side.Left;
			}
		}
	}

	private static InputMan instance;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(this.gameObject);
		}
		else
		{
			instance = this;
		}
	}

	#region navigation vars

	/// <summary>
	/// Contains a pair of bools for each axis input that can act as a button.
	/// The first is true only for the first frame the axis is active
	/// The second remains true when the button is held.
	/// 	it represents whether the button was already down last frame
	/// </summary>
	private static Dictionary<string, bool[]> firstPressed = new Dictionary<string, bool[]>();

	// the distance necessary to count as a "press"
	public static float triggerThreshold = .5f;
	public static float gripThreshold = .5f;
	public static float touchpadThreshold = .5f;
	public static float thumbstickThreshold = .5f;
	public static float thumbstickIdleThreshold = .1f;
	public static float directionalTimeout = 1f;
	private static Dictionary<string, float> directionalTimeoutValue = new Dictionary<string, float>();

	private static bool triggerDown = false;
	//private bool clickedLastFrame();

	#endregion

	#region Trigger

	public static float TriggerValue(Side side)
	{
		#if STEAMVR_AVAILABLE
		return SteamVR_Input.InputMan.inActions.TriggerValue.GetAxis(SideToInputSources(side));
		#endif
		return Input.GetAxis("VR_Trigger_" + side.ToString());
		
	}

	public static bool Trigger()
	{
		return Trigger(Side.Left) || Trigger(Side.Right);
	}
	
	public static bool Trigger(Side side)
	{
		#if STEAMVR_AVAILABLE
		return SteamVR_Input.InputMan.inActions.Trigger.GetState(SideToInputSources(side));
		#endif
		return TriggerValue(side) > triggerThreshold;
	}
	
	public static bool TriggerDown()
	{
		return TriggerDown(Side.Left) || TriggerDown(Side.Right);
	}

	public static bool TriggerDown(Side side)
	{
		#if STEAMVR_AVAILABLE
		return SteamVR_Input.InputMan.inActions.Trigger.GetStateDown(SideToInputSources(side));
		#endif
		return firstPressed["VR_Trigger_" + side.ToString()][0];
	}

	public static float MainTrigger()
	{
		return Input.GetAxis("VR_Trigger_" + DominantHand.ToString());
	}

	public static bool MainTriggerDown()
	{
		return triggerDown;
	}

	public static float SecondaryTrigger()
	{
		return Input.GetAxis("VR_Trigger_" + NonDominantHand.ToString());
	}

	public static bool SecondaryTriggerDown()
	{
		return false;
	}

	#endregion

	#region Grip

	public static float GripValue(Side side)
	{
		#if STEAMVR_AVAILABLE
		return SteamVR_Input.InputMan.inActions.GripValue.GetAxis(SideToInputSources(side));
		#endif
		return Input.GetAxis("VR_Grip_" + side.ToString());
	}

	public static bool Grip()
	{
		return Grip(Side.Left) || Grip(Side.Right);
	}

	public static bool Grip(Side side)
	{
		#if STEAMVR_AVAILABLE
		return SteamVR_Input.InputMan.inActions.Trigger.GetState(SideToInputSources(side));
		#endif
		return GripValue(side) > gripThreshold;
	}

	public static bool GripDown()
	{
		return GripDown(Side.Left) || GripDown(Side.Right);
	}

	public static bool GripDown(Side side)
	{
		#if STEAMVR_AVAILABLE
			return SteamVR_Input.InputMan.inActions.Grip.GetStateDown(SideToInputSources(side));
		#endif
		return firstPressed["VR_Grip_" + side.ToString()][0];
	}
	
//	public static bool GripUp()
//	{
//		return GripUp(Side.Left) || GripUp(Side.Right);
//	}
//	
//	public static bool GripUp(Side side)
//	{
//		return firstPressed["VR_Grip_" + side.ToString()][0];
//	}

	#endregion

	#region Thumbstick/Touchpad
	
	public static bool ThumbstickPress()
	{
		return ThumbstickPress(Side.Left) || ThumbstickPress(Side.Right);
	}

	public static bool ThumbstickPress(Side side)
	{
		return Input.GetButton("VR_Thumbstick_Press_" + side.ToString());
	}
	
	public static bool ThumbstickPressDown()
	{
		return ThumbstickPressDown(Side.Left) || ThumbstickPressDown(Side.Right);
	}

	public static bool ThumbstickPressDown(Side side)
	{
		return Input.GetButtonDown("VR_Thumbstick_Press_" + side.ToString());
	}

	public static bool ThumbstickPressUp(Side side)
	{
		return Input.GetButtonUp("VR_Thumbstick_Press_" + side.ToString());
	}

	public static bool MainThumbstickPressDown()
	{
		if (headsetType == HeadsetType.WMR)
		{
			return Input.GetButtonDown("VR_Thumbstick_Press_" + DominantHand.ToString());
		}
		else
		{
			return Input.GetButtonDown("VR_Thumbstick_Press_" + DominantHand.ToString());
		}
	}

	public static bool ThumbstickIdle(Side side, Axis axis)
	{
		if (axis == Axis.X)
		{
			return ThumbstickIdleX(side);
		}
		else if (axis == Axis.Y)
		{
			return ThumbstickIdleY(side);
		}
		else
		{
			Debug.LogError("More axes than possible.");
			return false;
		}
	}

	public static bool ThumbstickIdleX(Side side)
	{
		return Mathf.Abs(ThumbstickX(side)) < thumbstickIdleThreshold;
	}

	public static bool ThumbstickIdleY(Side side)
	{
		return Mathf.Abs(ThumbstickY(side)) < thumbstickIdleThreshold;
	}

	public static bool ThumbstickIdle(Side side)
	{
		return ThumbstickIdleX(side) && ThumbstickIdleY(side);
	}
	
	public static float Thumbstick(Side side, Axis axis)
	{
		if (axis == Axis.X)
		{
			return ThumbstickX(side);
		}
		else if (axis == Axis.Y)
		{
			return ThumbstickY(side);
		}
		else
		{
			Debug.LogError("More axes than possible.");
			return 0;
		}
	}

	public static float ThumbstickX(Side side)
	{
		return Input.GetAxis("VR_Thumbstick_X_" + side);
	}

	public static float ThumbstickY(Side side)
	{
		return Input.GetAxis("VR_Thumbstick_Y_" + side);
	}

	public static bool MainThumbstick()
	{
		return Input.GetButton("VR_Thumbstick_Press_" + DominantHand.ToString());
	}

	public static bool SecondaryThumbstickDown()
	{
		return Input.GetButtonDown("VR_Thumbstick_Press_" + NonDominantHand.ToString());
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

	public static bool MenuButton()
	{
		return MenuButton(Side.Left) || MenuButton(Side.Right);
	}

	public static bool MenuButton(Side side)
	{
		if (side == Side.Both)
		{
			return Input.GetButton("VR_MenuButton_" + Side.Left) &&
			       Input.GetButton("VR_MenuButton_" + Side.Right);
		}
		else if (side == Side.Either)
		{
			return Input.GetButton("VR_MenuButton_" + Side.Left) ||
			       Input.GetButton("VR_MenuButton_" + Side.Right);
		}
		return Input.GetButton("VR_MenuButton_" + side);
	}
	
	public static bool MenuButtonDown()
	{
		return MenuButtonDown(Side.Left) || MenuButtonDown(Side.Right);
	}

	public static bool MenuButtonDown(Side side)
	{
		if (side == Side.Both)
		{
			return Input.GetButtonDown("VR_MenuButton_" + Side.Left) &&
			       Input.GetButtonDown("VR_MenuButton_" + Side.Right);
		}
		else if (side == Side.Either)
		{
			return Input.GetButtonDown("VR_MenuButton_" + Side.Left) ||
			       Input.GetButtonDown("VR_MenuButton_" + Side.Right);
		}

		return Input.GetButtonDown("VR_MenuButton_" + side);
	}

	public static bool MainMenu()
	{
		return Input.GetButton("Menu_" + DominantHand);
	}

	public static bool SecondaryMenu()
	{
		return Input.GetButton("Menu_" + NonDominantHand);
	}

	public static bool SecondaryMenu(Side side)
	{
		if (side == Side.Both)
		{
			return Input.GetButton("VR_SecondButton_" + Side.Left) &&
			       Input.GetButton("VR_SecondButton_" + Side.Right);
		}
		else if (side == Side.Either)
		{
			return Input.GetButton("VR_SecondButton_" + Side.Left) ||
			       Input.GetButton("VR_SecondButton_" + Side.Right);
		}

		return Input.GetButton("VR_SecondButton_" + side);
	}

	public static bool SecondaryMenuDown(Side side)
	{
		if (side == Side.Both)
		{
			return Input.GetButtonDown("VR_SecondButton_" + Side.Left) &&
			       Input.GetButtonDown("VR_SecondButton_" + Side.Right);
		}
		else if (side == Side.Either)
		{
			return Input.GetButtonDown("VR_SecondButton_" + Side.Left) ||
			       Input.GetButtonDown("VR_SecondButton_" + Side.Right);
		}

		return Input.GetButtonDown("VR_SecondButton_" + side);
	}

	public static bool MainMenuDown()
	{
		return Input.GetButtonDown("Menu_" + DominantHand);
	}

	public static bool SecondaryMenuDown()
	{
		return Input.GetButtonDown("Menu_" + NonDominantHand);
	}

	#endregion

	#region Directions

	public static bool Up(Side side)
	{
		return firstPressed["VR_Thumbstick_Y_Up_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	public static bool Down(Side side)
	{
		return firstPressed["VR_Thumbstick_Y_Down_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	public static bool Left(Side side)
	{
		return firstPressed["VR_Thumbstick_X_Left_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	public static bool Right(Side side)
	{
		return firstPressed["VR_Thumbstick_X_Right_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	#endregion

	#region Vibrations
	
	/// <summary>
	/// Whether the left (0) or right (1) controllers are vibrating
	/// </summary>
	private static bool[] vibrating;
	#if OCULUS_UTILITES_AVAILABLE
	private static OVRHapticsClip[] hapticsClip;
	#endif

	/// <summary>
	/// Vibrate the controller
	/// </summary>
	/// <param name="side">Which controller to vibrate</param>
	/// <param name="intensity">Intensity from 0 to 1</param>
	public static void Vibrate(Side side, float intensity)
	{
		intensity = Mathf.Clamp01(intensity);
		
		#if OCULUS_UTILITES_AVAILABLE
			OVRHaptics.OVRHapticsChannel channel;
			if (side == Side.Left)
			{
				channel = OVRHaptics.LeftChannel;
			} else if (side == Side.Right)
			{
				channel = OVRHaptics.RightChannel;
			}
			else
			{
				Debug.LogError("Cannot vibrate on " + side);
				return;
			}
	
			int length = 10;
			byte[] bytes = new byte[length];
			for (int i = 0; i < length; i++)
			{
				bytes[i] = (byte)(intensity * 255);
			}
	
			OVRHapticsClip clip = new OVRHapticsClip(bytes, length);
			channel.Preempt(clip);
		#endif

		#if STEAMVR_AVAILABLE
			//SteamVR_Controller.Input(side == Side.Left ? 0 : 1).TriggerHapticPulse(500);
			//SteamVR_Input._default.outActions.Haptic
#endif
	}

	#endregion

	private void Start()
	{		
		#if STEAMVR_AVAILABLE
			VRPackageInUse = VRPackage.SteamVR;
		#endif
		#if OCULUS_UTILITES_AVAILABLE
				VRPackageInUse = VRPackage.Oculus;
		#endif
		
		Debug.Log("InputMan loaded device: " + XRSettings.loadedDeviceName);
		
		if (XRDevice.model.Contains("Oculus"))
		{
			headsetType = HeadsetType.Rift;
		}
		else if (XRDevice.model.Contains("Vive"))
		{
			headsetType = HeadsetType.Vive;
		}
		else if (XRDevice.model.Contains("Mixed") || XRDevice.model.Contains("WMR"))
		{
			headsetType = HeadsetType.WMR;
		}

		for (int i = 0; i < 2; i++)
		{
			firstPressed.Add("VR_Trigger_" + (Side) i, new bool[2]);
			firstPressed.Add("VR_Grip_" + (Side) i, new bool[2]);
			firstPressed.Add("VR_Thumbstick_X_Left_" + (Side) i, new bool[2]);
			firstPressed.Add("VR_Thumbstick_X_Right_" + (Side) i, new bool[2]);
			firstPressed.Add("VR_Thumbstick_Y_Up_" + (Side) i, new bool[2]);
			firstPressed.Add("VR_Thumbstick_Y_Down_" + (Side) i, new bool[2]);

			directionalTimeoutValue.Add("VR_Thumbstick_X_Left_" + (Side) i, 0);
			directionalTimeoutValue.Add("VR_Thumbstick_X_Right_" + (Side) i, 0);
			directionalTimeoutValue.Add("VR_Thumbstick_Y_Up_" + (Side) i, 0);
			directionalTimeoutValue.Add("VR_Thumbstick_Y_Down_" + (Side) i, 0);
		}

		
	}
	
	#if STEAMVR_AVAILABLE
	SteamVR_Input_Sources SideToInputSources(Side side)
	{
		if (side == Side.Left)
		{
			return SteamVR_Input_Sources.LeftHand;
		} else if (side == Side.Right)
		{
			return SteamVR_Input_Sources.RightHand;
		} else if (side == Side.Both)
		{
			return SteamVR_Input_Sources.Any;
		}
		else
		{
			Debug.LogError("Cannot convert that side to an input source.");
		}

		return SteamVR_Input_Sources.Any;
	}
	#endif

	void UpdateDictionary(bool currentVal, string key)
	{
		if (currentVal)
		{
			if (!firstPressed[key][1])
			{
				firstPressed[key][0] = true;
				firstPressed[key][1] = true;
			}
			else
			{
				firstPressed[key][0] = false;
			}
		}
		else
		{
			firstPressed[key][0] = false;
			firstPressed[key][1] = false;
		}
	}

	void UpdateDictionaryDirection(bool currentVal, string key)
	{
		if (currentVal)
		{
			if (directionalTimeoutValue[key] > directionalTimeout)
			{
				firstPressed[key][1] = false;
				directionalTimeoutValue[key] = 0;
			}

			directionalTimeoutValue[key] += Time.deltaTime;
		}
		else
		{
			directionalTimeoutValue[key] = 0;
		}

		UpdateDictionary(currentVal, key);
	}

	void Update()
	{
		
		for (int i = 0; i < 2; i++)
		{
			UpdateDictionary(Trigger((Side) i), "VR_Trigger_" + (Side) i);
			UpdateDictionary(Grip((Side) i), "VR_Grip_" + (Side) i);
			
			
			UpdateDictionaryDirection(ThumbstickX((Side) i) < -thumbstickThreshold, "VR_Thumbstick_X_Left_" + (Side) i);
			UpdateDictionaryDirection(ThumbstickX((Side) i) > thumbstickThreshold, "VR_Thumbstick_X_Right_" + (Side) i);
			UpdateDictionaryDirection(ThumbstickY((Side) i) > thumbstickThreshold, "VR_Thumbstick_Y_Up_" + (Side) i);
			UpdateDictionaryDirection(ThumbstickY((Side) i) < -thumbstickThreshold, "VR_Thumbstick_Y_Down_" + (Side) i);
		}
	}
}