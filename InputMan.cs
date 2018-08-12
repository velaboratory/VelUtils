#undef STEAMVR_AVAILABLE // change to #define or #undef if SteamVR utilites are installed
#undef OCULUS_UTILITES_AVAILABLE

using System.Collections.Generic;
using UnityEngine;

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

public enum Side
{
	Left,
	Right
}


public class InputMan : MonoBehaviour
{
	public HeadsetType headsetType;
	public Side dominantHand;

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
	/// The second remains true when the button is held
	/// 	it represents whether the button was already down last frame
	/// </summary>
	private Dictionary<string, bool[]> firstPressed = new Dictionary<string, bool[]>();

	// the distance necessary to cout as a "press"
	public float triggerThreshold = .5f;
	public float gripThreshold = .5f;
	public float touchpadThreshold = .5f;
	public float thumbstickThreshold = .5f;

	private bool triggerDown = false;
	//private bool clickedLastFrame();

	#endregion

	// Trigger
	public float TriggerValue(Side side)
	{
		return Input.GetAxis("VR_Trigger_" + side.ToString());
	}

	public bool Trigger(Side side)
	{
		return TriggerValue(side) > triggerThreshold;
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

	// grip
	public float GripValue(Side side)
	{
		return Input.GetAxis("VR_Grip_" + side.ToString());
	}

	public bool Grip(Side side)
	{
		return GripValue(side) > gripThreshold;
	}

	// Thumbstick
	public bool ThumbstickPress(Side side)
	{
		return Input.GetButton("VR_Thumbstick_Press_" + side.ToString());
	}

	public bool ThumbstickPressDown(Side side)
	{
		return Input.GetButtonDown("VR_Thumbstick_Press_" + side.ToString());
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

	public float ThumbstickX(Side side)
	{
		return Input.GetAxis("VR_Thumbstick_X" + side);
	}

	public float ThumbstickY(Side side)
	{
		return Input.GetAxis("VR_Thumbstick_Y" + side);
	}

	public bool MainThumbstick()
	{
		return Input.GetButton("VR_Thumbstick_Press_" + DominantHand.ToString());
	}

	public bool SecondaryThumbstickDown()
	{
		return Input.GetButtonDown("VR_Thumbstick_Press_" + NonDominantHand.ToString());
	}


	// menu buttons
	public bool MainMenuDown()
	{
		return Input.GetButtonDown("Menu_" + DominantHand.ToString());
	}

	public bool SecondaryMenuDown()
	{
		return Input.GetButtonDown("Menu_" + NonDominantHand.ToString());
	}

	public bool MainMenu()
	{
		return Input.GetButton("Menu_" + DominantHand.ToString());
	}

	public bool SecondaryMenu()
	{
		return Input.GetButton("Menu_" + NonDominantHand.ToString());
	}

	// directions
	public bool Up(Side side)
	{
		return (ThumbstickY(side) > thumbstickThreshold) && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}
	
	public bool Down(Side side)
	{
		return (ThumbstickY(side) < -thumbstickThreshold) && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}
	
	public bool Left(Side side)
	{
		return (ThumbstickX(side) < -thumbstickThreshold) && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}
	
	public bool Right(Side side)
	{
		return (ThumbstickX(side) > thumbstickThreshold) && (headsetType == HeadsetType.Rift || ThumbstickPress(side));
	}

	private void Start()
	{
		Side side = Side.Left;
		for (int i = 0; i < 2; i++)
		{
			firstPressed.Add("VR_Trigger_" + side.ToString(), new bool[2]);
			firstPressed.Add("VR_Grip_" + side.ToString(), new bool[2]);

			side = Side.Right;
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

	void Update()
	{
		Side side = Side.Left;
		for (int i = 0; i < 2; i++)
		{
			UpdateDictionary(Trigger(side), "VR_Trigger_" + side);
			UpdateDictionary(Grip(side), "VR_Grip_" + side);

			side = Side.Right;
		}
	}
}