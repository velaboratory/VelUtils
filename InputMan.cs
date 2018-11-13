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
	public HeadsetType headsetType;
	public Side dominantHand;
	public VRPackage VRPackageInUse;

	public Side DominantHand
	{
		get { return dominantHand; }
		set { dominantHand = value; }
	}

	public Side NonDominantHand
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

	#region navigation vars

	/// <summary>
	/// Contains a pair of bools for each axis input that can act as a button.
	/// The first is true only for the first frame the axis is active
	/// The second remains true when the button is held.
	/// 	it represents whether the button was already down last frame
	/// </summary>
	private Dictionary<string, bool[]> firstPressed = new Dictionary<string, bool[]>();

	// the distance necessary to count as a "press"
	public float triggerThreshold = .5f;
	public float gripThreshold = .5f;
	public float touchpadThreshold = .5f;
	public float thumbstickThreshold = .5f;
	public float thumbstickIdleThreshold = .1f;
	public float directionalTimeout = 1f;
	private Dictionary<string, float> directionalTimeoutValue = new Dictionary<string, float>();

	private bool triggerDown = false;
	//private bool clickedLastFrame();

	#endregion

	#region Trigger

	public float TriggerValue(Side side)
	{
		return Input.GetAxis("VR_Trigger_" + side.ToString());
	}

	public bool Trigger()
	{
		return Trigger(Side.Left) || Trigger(Side.Right);
	}
	
	public bool Trigger(Side side)
	{
		return TriggerValue(side) > triggerThreshold;
	}
	
	public bool TriggerDown()
	{
		return TriggerDown(Side.Left) || TriggerDown(Side.Right);
	}

	public bool TriggerDown(Side side)
	{
		return firstPressed["VR_Trigger_" + side.ToString()][0];
	}

	public float MainTrigger()
	{
		return Input.GetAxis("VR_Trigger_" + DominantHand.ToString());
	}

	public bool MainTriggerDown()
	{
		return triggerDown;
	}

	public float SecondaryTrigger()
	{
		return Input.GetAxis("VR_Trigger_" + NonDominantHand.ToString());
	}

	public bool SecondaryTriggerDown()
	{
		return false;
	}

	#endregion

	#region Grip

	public float GripValue(Side side)
	{
		return Input.GetAxis("VR_Grip_" + side.ToString());
	}

	public bool Grip()
	{
		return Grip(Side.Left) || Grip(Side.Right);
	}

	public bool Grip(Side side)
	{
		return GripValue(side) > gripThreshold;
	}

	public bool GripDown()
	{
		return GripDown(Side.Left) || GripDown(Side.Right);
	}

	public bool GripDown(Side side)
	{
		return firstPressed["VR_Grip_" + side.ToString()][0];
	}
	
//	public bool GripUp()
//	{
//		return GripUp(Side.Left) || GripUp(Side.Right);
//	}
//	
//	public bool GripUp(Side side)
//	{
//		return firstPressed["VR_Grip_" + side.ToString()][0];
//	}

	#endregion

	#region Thumbstick/Touchpad
	
	public bool ThumbstickPress()
	{
		return ThumbstickPress(Side.Left) || ThumbstickPress(Side.Right);
	}

	public bool ThumbstickPress(Side side)
	{
		return Input.GetButton("VR_Thumbstick_Press_" + side.ToString());
	}
	
	public bool ThumbstickPressDown()
	{
		return ThumbstickPressDown(Side.Left) || ThumbstickPressDown(Side.Right);
	}

	public bool ThumbstickPressDown(Side side)
	{
		return Input.GetButtonDown("VR_Thumbstick_Press_" + side.ToString());
	}

	public bool ThumbstickPressUp(Side side)
	{
		return Input.GetButtonUp("VR_Thumbstick_Press_" + side.ToString());
	}

	public bool MainThumbstickPressDown()
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

	public bool ThumbstickIdle(Side side, Axis axis)
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

	public bool ThumbstickIdleX(Side side)
	{
		return Mathf.Abs(ThumbstickX(side)) < thumbstickIdleThreshold;
	}

	public bool ThumbstickIdleY(Side side)
	{
		return Mathf.Abs(ThumbstickY(side)) < thumbstickIdleThreshold;
	}

	public bool ThumbstickIdle(Side side)
	{
		return ThumbstickIdleX(side) && ThumbstickIdleY(side);
	}
	
	public float Thumbstick(Side side, Axis axis)
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

	public float ThumbstickX(Side side)
	{
		return Input.GetAxis("VR_Thumbstick_X_" + side);
	}

	public float ThumbstickY(Side side)
	{
		return Input.GetAxis("VR_Thumbstick_Y_" + side);
	}

	public bool MainThumbstick()
	{
		return Input.GetButton("VR_Thumbstick_Press_" + DominantHand.ToString());
	}

	public bool SecondaryThumbstickDown()
	{
		return Input.GetButtonDown("VR_Thumbstick_Press_" + NonDominantHand.ToString());
	}

	// aux methods for pad
	public bool PadClickDown(Side side)
	{
		return ThumbstickPressDown(side);
	}

	public bool PadIdleX(Side side)
	{
		return ThumbstickIdleX(side);
	}

	public bool PadIdleY(Side side)
	{
		return ThumbstickIdleY(side);
	}

	public bool PadIdle(Side side)
	{
		return ThumbstickIdle(side);
	}

	public bool PadClick(Side side)
	{
		return ThumbstickPress(side);
	}

	public bool PadClickUp(Side side)
	{
		return ThumbstickPressUp(side);
	}

	public float PadX(Side side)
	{
		return ThumbstickX(side);
	}

	public float PadY(Side side)
	{
		return ThumbstickY(side);
	}

	#endregion

	#region Menu buttons

	public bool MenuButton()
	{
		return MenuButton(Side.Left) || MenuButton(Side.Right);
	}

	public bool MenuButton(Side side)
	{
		return Input.GetButton("VR_MenuButton_" + side);
	}
	
	public bool MenuButtonDown()
	{
		return MenuButtonDown(Side.Left) || MenuButtonDown(Side.Right);
	}

	public bool MenuButtonDown(Side side)
	{
		return Input.GetButtonDown("VR_MenuButton_" + side);
	}
	
	public bool MainMenu()
	{
		return Input.GetButton("Menu_" + DominantHand);
	}

	public bool SecondaryMenu()
	{
		return Input.GetButton("Menu_" + NonDominantHand);
	}
	
	public bool SecondaryMenu(Side side)
	{
		return Input.GetButton("VR_SecondButton_" + side);
	}
	
	public bool SecondaryMenuDown(Side side)
	{
		return Input.GetButtonDown("VR_SecondButton_" + side);
	}

	public bool MainMenuDown()
	{
		return Input.GetButtonDown("Menu_" + DominantHand);
	}

	public bool SecondaryMenuDown()
	{
		return Input.GetButtonDown("Menu_" + NonDominantHand);
	}

	#endregion

	#region Directions

	public bool Up(Side side)
	{
		return firstPressed["VR_Thumbstick_Y_Up_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	public bool Down(Side side)
	{
		return firstPressed["VR_Thumbstick_Y_Down_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	public bool Left(Side side)
	{
		return firstPressed["VR_Thumbstick_X_Left_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	public bool Right(Side side)
	{
		return firstPressed["VR_Thumbstick_X_Right_" + side][0] && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	#endregion

	#region Vibrations
	
	/// <summary>
	/// Whether the left (0) or right (1) controllers are vibrating
	/// </summary>
	private bool[] vibrating;
	private OVRHapticsClip[] hapticsClip;

	/// <summary>
	/// Vibrate the controller
	/// </summary>
	/// <param name="intensity">Intensity from 0 to 1</param>
	public void Vibrate(Side side, float intensity)
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
			SteamVR_Controller.Input(side == Side.Left ? 0 : 1).TriggerHapticPulse(500);
		#endif
	}

	#endregion

	private void Start()
	{
		#if OCULUS_UTILITES_AVAILABLE
		#endif
		#if STEAMVR_AVAILABLE
		#endif
		
		Debug.Log("InputMan loaded device: " + XRSettings.loadedDeviceName);
		
		if (XRSettings.loadedDeviceName == "Oculus")
		{
			headsetType = HeadsetType.Rift;
		}
		else if (XRSettings.loadedDeviceName.Contains("Vive"))
		{
			headsetType = HeadsetType.Vive;
		}
		else if (XRSettings.loadedDeviceName.Contains("Mixed"))
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