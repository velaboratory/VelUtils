using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class InputMan : MonoBehaviour {

	public ControllerControllerController ccc;

	#region navigation vars
	public bool navUp;
	public bool navDown;
	public bool navLeft;
	public bool navRight;

	private float navigationArrowTimeout = 0f;
	private float navigationArrowTimeoutMaxTime = .75f;
	private int navigationArrowCounter = 0;

	private bool lastSecondaryTriggerDown = false;
	private bool secondaryTriggerDown = false;
	private bool lastTriggerDown = false;
	private bool triggerDown = false;
	//private bool clickedLastFrame();
	#endregion

	public float MainTrigger()
	{
		return Input.GetAxis("IndexTrigger_" + ccc.currentController.dominantHand.ToString());
	}

	public bool MainTriggerDown()
	{
		return triggerDown;
	}

	public float SecondaryTrigger()
	{
		return Input.GetAxis("IndexTrigger_" + ccc.currentController.GetOffHand().ToString());
	}

	public bool MainThumbstickDown()
	{
		if (ccc.headsetType == HeadsetType.WMR)
		{
			return Input.GetButtonDown("ThumbstickPress_" + ccc.currentController.dominantHand.ToString()) &&
				ccc.device.GetAxis()[0] > .5f &&	// horizontal
				Mathf.Abs(ccc.device.GetAxis()[1]) < .5f;	// vertical
		}
		else
		{
			return Input.GetButtonDown("ThumbstickPress_" + ccc.currentController.dominantHand.ToString());
		}
	}

	public bool Back()
	{
		if (ccc.headsetType == HeadsetType.WMR)
		{
			return Input.GetButtonDown("ThumbstickPress_" + ccc.currentController.dominantHand.ToString()) &&
				ccc.device.GetAxis()[0] < -.5f &&    // horizontal
				Mathf.Abs(ccc.device.GetAxis()[1]) < .5f;   // vertical
		}
		else
		{
			return false; // Input.GetButtonDown("ThumbstickPress_" + ccc.currentController.dominantHand.ToString());
		}
	}

	public bool MainThumbstick()
	{
		return Input.GetButton("ThumbstickPress_" + ccc.currentController.dominantHand.ToString());
	}

	public bool SecondaryThumbstickDown()
	{
		return Input.GetButtonDown("ThumbstickPress_" + ccc.currentController.GetOffHand().ToString());
	}

	public bool SecondaryTriggerDown()
	{
		return secondaryTriggerDown;
	}

	public bool MainMenuDown()
	{
		return Input.GetButtonDown("Menu_" + ccc.currentController.dominantHand.ToString());
	}

	public bool SecondaryMenuDown()
	{
		return Input.GetButtonDown("Menu_" + ccc.currentController.GetOffHand().ToString());
	}

	public bool MainMenu()
	{
		return Input.GetButton("Menu_" + ccc.currentController.dominantHand.ToString());
	}

	public bool SecondaryMenu()
	{
		return Input.GetButton("Menu_" + ccc.currentController.GetOffHand().ToString());
	}

	public bool up()
	{
		return navUp;
	}

	public bool down()
	{
		return navDown;
	}

	public bool left()
	{
		return navLeft;
	}

	public bool right()
	{
		return navRight;
	}

	public bool secondaryLeft()
	{
		return false;
	}

	public bool secondaryRight()
	{
		return false;
	}

	public bool secondaryUp()
	{
		return false;
	}

	public bool secondaryDown()
	{
		return false;
	}

	void Update()
	{
		updateNavigation();

		if (lastSecondaryTriggerDown)
		{
			secondaryTriggerDown = false;
		} else if (Input.GetAxis("IndexTrigger_" + ccc.currentController.GetOffHand().ToString()) > .5f)
		{
			secondaryTriggerDown = true;
		}
		lastSecondaryTriggerDown = Input.GetAxis("IndexTrigger_" + ccc.currentController.GetOffHand().ToString()) > .5f;

		if (lastTriggerDown)
		{
			triggerDown = false;
		}
		else if (Input.GetAxis("IndexTrigger_" + ccc.currentController.dominantHand.ToString()) > .5f)
		{
			triggerDown = true;
		}
		lastTriggerDown = Input.GetAxis("IndexTrigger_" + ccc.currentController.dominantHand.ToString()) > .5f;

	}

	// gets the axis of the thumbstick/touchpad specified, but returns 0 if a touchpad is not clicked
	public float GetAxis(int axisId, bool secondaryHand)
	{
		if (!secondaryHand)
		{
			if (ccc.headsetType == HeadsetType.Vive ||
				ccc.headsetType == HeadsetType.WMR)
			{
				if (ccc.device.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad))
					return Mathf.Clamp(ccc.device.GetAxis()[axisId]*2,-1,1);
				else return 0;
			}
			else if (ccc.headsetType == HeadsetType.Rift)
			{
				return Mathf.Clamp(ccc.device.GetAxis()[axisId] * 2, -1, 1);
			}
			else
			{
				return 0;
			}
		}
		else
		{
			if (ccc.headsetType == HeadsetType.Vive ||
				ccc.headsetType == HeadsetType.WMR)
			{
				if (ccc.offHandDevice.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad))
					return Mathf.Clamp(ccc.offHandDevice.GetAxis()[axisId] * 2, -1, 1);
				else return 0;
			}
			else if (ccc.headsetType == HeadsetType.Rift)
			{
				return ccc.offHandDevice.GetAxis()[axisId];
			}
			else
			{
				return 0;
			}
		}
	}

	private void updateNavigation()
	{
		if (ccc.device != null)
		{
			if (ccc.headsetType == HeadsetType.Vive)
			{
				navUp = ccc.device.GetAxis()[1] > .5f && ccc.device.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad);
				navDown = ccc.device.GetAxis()[1] < -.5f && ccc.device.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad);
				navLeft = ccc.device.GetAxis()[0] > .5f && ccc.device.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad);
				navRight = ccc.device.GetAxis()[0] < -.5f && ccc.device.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad);
			}
			else if (ccc.headsetType == HeadsetType.WMR)
			{
				navUp = ccc.device.GetAxis()[1] > .5f && ccc.device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad);
				navDown = ccc.device.GetAxis()[1] < -.5f && ccc.device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad);
				navRight = ccc.device.GetAxis()[0] > .5f && ccc.device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad);
				navLeft = ccc.device.GetAxis()[0] < -.5f && ccc.device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad);
			}
			else if (ccc.headsetType == HeadsetType.Rift)
			{
				if (navigationArrowTimeout > navigationArrowTimeoutMaxTime)
				{
					navUp = ccc.device.GetAxis()[1] > .5f;
					navDown = ccc.device.GetAxis()[1] < -.5f;
					navLeft = ccc.device.GetAxis()[0] < -.5f;
					navRight = ccc.device.GetAxis()[0] > .5f;
					if (navDown || navLeft || navRight || navUp)
					{
						navigationArrowTimeout = 0;
					}
				}
				else
				{
					navUp = false;
					navDown = false;
					navLeft = false;
					navRight = false;
				}
				navigationArrowTimeout += Time.deltaTime;
			}

			// if the direction has been held down, move faster
			if (navigationArrowCounter > 1)
			{
				navigationArrowTimeout += Time.deltaTime * 3;
			}
			else
			{
				navigationArrowTimeout += Time.deltaTime;
			}

			// if the direction went back to center, remove the delay
			if (ccc.device.GetAxis()[0] == 0 && ccc.device.GetAxis()[1] == 0)
			{
				navigationArrowCounter = 0;
				navigationArrowTimeout = 10;
			}
		}
	}

}
