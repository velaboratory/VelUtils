#undef STEAMVR_AVAILABLE // change to #define or #undef if SteamVR utilites are installed
#define OCULUS_UTILITIES_AVAILABLE

using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using IEnumerator = System.Collections.IEnumerator;

#if STEAMVR_AVAILABLE
using Valve.VR;
#endif

public enum HeadsetType
{
	None,
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
public class InputMan : MonoBehaviour {
	public static HeadsetType headsetType;
	private static VRPackage VRPackageInUse;

	private static Side dominantHand;

	public static Side DominantHand {
		get { return dominantHand;}
		set { dominantHand = value; }
	}

	public static Side NonDominantHand {
		get {
			if (DominantHand == Side.Left) {
				return Side.Right;
			}
			else if (DominantHand == Side.Right) {
				return Side.Left;
			}
			else {
				Debug.LogError("No dominant side selected");
				return Side.None;
			}
		}
	}

	private InputDevice[] xRNodes;
	private XRNodeState[] xrNodeStates;

	private static InputDevice GetXRNode(Side side)
	{
		if (side == Side.Left)
		{
			return InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
		} else //(side == Side.Right)
		{
			return InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		}
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
			else if (side == Side.Right && ns.nodeType == XRNode.RightHand)
			{
				return ns;
			}
		}
		return nodes[0];
	}

	private static InputMan instance;

	private void Awake() {
		if (instance != null && instance != this) {
			Destroy(gameObject);
		}
		else {
			instance = this;
		}

#if STEAMVR_AVAILABLE
			VRPackageInUse = VRPackage.SteamVR;
#endif
#if OCULUS_UTILITIES_AVAILABLE
		VRPackageInUse = VRPackage.Oculus;
#endif

		Debug.Log("InputMan loaded device: " + XRSettings.loadedDeviceName);

		if (XRDevice.model.Contains("Oculus")) {
			headsetType = HeadsetType.Rift;
		}
		else if (XRDevice.model.Contains("Vive")) {
			headsetType = HeadsetType.Vive;
		}
		else if (XRDevice.model.Contains("Mixed") || XRDevice.model.Contains("WMR")) {
			headsetType = HeadsetType.WMR;
		}

		for (int i = 0; i < 2; i++) {
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

	#endregion

	#region Trigger

	public static float TriggerValue(Side side = Side.Either) {
		return GetRawValue("VR_Trigger_", side);
	}

	public static bool Trigger(Side side = Side.Either) {
		return TriggerValue(side) > triggerThreshold;
	}
	
	public static bool TriggerDown(Side side = Side.Either) {
		return GetRawValueDown("VR_Trigger_", side);
	}

	public static bool MainTrigger() {
		return Trigger(DominantHand);
	}
	
	public static float MainTriggerValue() {
		return TriggerValue(DominantHand);
	}

	public static bool MainTriggerDown() {
		return TriggerDown(DominantHand);
	}

	public static bool SecondaryTrigger() {
		return Trigger(NonDominantHand);
	}

	public static float SecondaryTriggerValue() {
		return TriggerValue(NonDominantHand);
	}

	public static bool SecondaryTriggerDown() {
		return TriggerDown(NonDominantHand);
	}

	#endregion

	#region Grip

	public static float GripValue(Side side = Side.Either) {
		return GetRawValue("VR_Grip_", side);
	}
	public static bool Grip(Side side = Side.Either) {
		return GripValue(side) > gripThreshold;
	}
	public static bool GripDown(Side side = Side.Either) {
		return GetRawValueDown("VR_Grip_", side);
	}

	#endregion

	#region Thumbstick/Touchpad
	
	public static bool ThumbstickPress(Side side = Side.Either) {
		return GetRawButton("VR_Thumbstick_Press_", side);
	}

	public static bool ThumbstickPressDown(Side side = Side.Either) {
		return GetRawButtonDown("VR_Thumbstick_Press_", side);
	}
	
	public static bool ThumbstickPressUp(Side side = Side.Either) {
		return GetRawButtonUp("VR_Thumbstick_Press_", side);
	}

	public static bool ThumbstickIdle(Side side, Axis axis) {
		if (axis == Axis.X) {
			return ThumbstickIdleX(side);
		}
		else if (axis == Axis.Y) {
			return ThumbstickIdleY(side);
		}
		else {
			Debug.LogError("More axes than possible.");
			return false;
		}
	}

	public static bool ThumbstickIdleX(Side side = Side.Either) {
		return Mathf.Abs(ThumbstickX(side)) < thumbstickIdleThreshold;
	}

	public static bool ThumbstickIdleY(Side side = Side.Either) {
		return Mathf.Abs(ThumbstickY(side)) < thumbstickIdleThreshold;
	}

	public static bool ThumbstickIdle(Side side = Side.Either) {
		return ThumbstickIdleX(side) && ThumbstickIdleY(side);
	}
	
	public static float Thumbstick(Side side, Axis axis) {
		if (axis == Axis.X) {
			return ThumbstickX(side);
		}
		else if (axis == Axis.Y) {
			return ThumbstickY(side);
		}
		else {
			Debug.LogError("More axes than possible.");
			return 0;
		}
	}

	public static float ThumbstickX(Side side = Side.Either) {
		return GetRawValue("VR_Thumbstick_X_", side);
	}

	public static float ThumbstickY(Side side = Side.Either) {
		return GetRawValue("VR_Thumbstick_Y_", side);
	}

	public static bool MainThumbstickPress() {
		return ThumbstickPress(DominantHand);
	}
	
	public static bool MainThumbstickPressDown() {
		return ThumbstickPressDown(DominantHand);
	}

	public static bool SecondaryThumbstickPress() {
		return ThumbstickPress(NonDominantHand);
	}

	public static bool SecondaryThumbstickPressDown() {
		return ThumbstickPressDown(NonDominantHand);
	}

	// aux methods for pad
	public static bool PadClickDown(Side side) {
		return ThumbstickPressDown(side);
	}

	public static bool PadIdleX(Side side) {
		return ThumbstickIdleX(side);
	}

	public static bool PadIdleY(Side side) {
		return ThumbstickIdleY(side);
	}

	public static bool PadIdle(Side side) {
		return ThumbstickIdle(side);
	}

	public static bool PadClick(Side side) {
		return ThumbstickPress(side);
	}

	public static bool PadClickUp(Side side) {
		return ThumbstickPressUp(side);
	}

	public static float PadX(Side side) {
		return ThumbstickX(side);
	}

	public static float PadY(Side side) {
		return ThumbstickY(side);
	}

	#endregion

	#region Menu buttons

	public static bool MenuButton(Side side = Side.Either) {
		return GetRawButton("VR_MenuButton_", side);
	}

	public static bool MenuButtonDown(Side side = Side.Either) {
		return GetRawButtonDown("VR_MenuButton_", side);
	}
	
	public static bool MainMenuButton() {
		return MenuButton(DominantHand);
	}

	public static bool MainMenuButtonDown() {
		return MenuButtonDown(DominantHand);
		}

	public static bool SecondaryMenuButton() {
		return MenuButton(NonDominantHand);
	}

	public static bool SecondaryMenuButtonDown() {
		return MenuButtonDown(NonDominantHand);
	}

	#endregion

	#region Directions

	public static bool Up(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed["VR_Thumbstick_Y_Up_" + Side.Left][0] &&
			        (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Left))) ||
			       (firstPressed["VR_Thumbstick_Y_Up_" + Side.Right][0] &&
			        (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed["VR_Thumbstick_Y_Up_" + side][0] &&
			       (headsetType == HeadsetType.Rift || ThumbstickPress(side));
		}
	}

	public static bool Down(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed["VR_Thumbstick_Y_Down_" + Side.Left][0] &&
			        (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Left))) ||
			       (firstPressed["VR_Thumbstick_Y_Down_" + Side.Right][0] &&
			        (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed["VR_Thumbstick_Y_Down_" + side][0] &&
			       (headsetType == HeadsetType.Rift || ThumbstickPress(side));
		}
	}

	public static bool Left(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed["VR_Thumbstick_X_Left_" + Side.Left][0] &&
			       (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Left))) ||
			       (firstPressed["VR_Thumbstick_X_Left_" + Side.Right][0] &&
			       (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed["VR_Thumbstick_X_Left_" + side][0] &&
			       (headsetType == HeadsetType.Rift || ThumbstickPress(side));
		}
	}

	public static bool Right(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed["VR_Thumbstick_X_Right_" + Side.Left][0] &&
			        (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Left))) ||
			       (firstPressed["VR_Thumbstick_X_Right_" + Side.Right][0] &&
			        (headsetType == HeadsetType.Rift || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed["VR_Thumbstick_X_Right_" + side][0] &&
			       (headsetType == HeadsetType.Rift || ThumbstickPress(side));
		}
	}

	public static float Vertical(Side side = Side.Either) {
		if (headsetType == HeadsetType.Rift) {
			return ThumbstickY();
		}
		else if (ThumbstickPress()) {
			return ThumbstickY();
		}
		else {
			return 0;
		}
	}
	
	public static float Horizontal(Side side = Side.Either) {
		if (headsetType == HeadsetType.Rift) {
			return ThumbstickX();
		}
		else if (ThumbstickPress()) {
			return ThumbstickX();
		}
		else {
			return 0;
		}
	}

	#endregion

	#region Vibrations
	
	/// <summary>
	/// Whether the left (0) or right (1) controllers are vibrating
	/// </summary>
	private static bool[] vibrating;
	#if OCULUS_UTILITIES_AVAILABLE
	private static OVRHapticsClip[] hapticsClip;
#endif

	/// <summary>
	/// Vibrate the controller
	/// </summary>
	/// <param name="side">Which controller to vibrate</param>
	/// <param name="intensity">Intensity from 0 to 1</param>
	public static void Vibrate(Side side, float intensity, float duration = 1, float delay = 0) {

		if (delay > 0 && instance) {
			instance.StartVibrateDelay(side, intensity, duration, delay);
			return;
		}


		intensity = Mathf.Clamp01(intensity);

#if OCULUS_UTILITIES_AVAILABLE
		OVRHaptics.OVRHapticsChannel channel;
		if (side == Side.Left) {
			channel = OVRHaptics.LeftChannel;
		} else if (side == Side.Right) {
			channel = OVRHaptics.RightChannel;
		} else {
			Debug.LogError("Cannot vibrate on " + side);
			return;
		}

		int length = (int)(duration * 10);
		byte[] bytes = new byte[length];
		for (int i = 0; i < length; i++) {
			bytes[i] = (byte)(intensity * 255);
		}

		OVRHapticsClip clip = new OVRHapticsClip(bytes, length);
		channel.Preempt(clip);
#elif STEAMVR_AVAILABLE
			//SteamVR_Controller.Input(side == Side.Left ? 0 : 1).TriggerHapticPulse(500);
			//SteamVR_Input._default.outActions.Haptic
#else
		GetXRNode(side).SendHapticImpulse(0, intensity, duration);
#endif
	}

#if OCULUS_UTILITIES_AVAILABLE
	/// <summary>
	/// Vibrate the controller
	/// </summary>
	/// <param name="side">Which controller to vibrate</param>
	/// <param name="intensity">Intensity from 0 to 1</param>
	public static void Vibrate(OVRInput.Controller side, float intensity, float duration = 1, float delay = 0) {
		Vibrate(OVRController2Side(side), intensity, duration, delay);
	}
#endif

	void StartVibrateDelay(Side side, float intensity, float duration, float delay) {
		StartCoroutine(VibrateDelay(side, intensity, duration, delay));
	}

	IEnumerator VibrateDelay(Side side, float intensity, float duration, float delay) {
		yield return new WaitForSeconds(delay);
		Vibrate(side, intensity, duration, 0);
	}

#endregion

#region Controller Velocity

	public static Vector3 LocalControllerVelocity(Side side) {
		Vector3 vel;
#if OCULUS_UTILITIES_AVAILABLE
		vel = OVRInput.GetLocalControllerVelocity(Side2OVRController(side));
#else
		GetXRNodeState(side).TryGetVelocity(out vel);
#endif
		
		return vel;
		}

#endregion

	
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

#if OCULUS_UTILITIES_AVAILABLE
	public static Side OVRController2Side(OVRInput.Controller controller) {
		if (controller == OVRInput.Controller.LTouch) {
			return Side.Left;
		}
		else if (controller == OVRInput.Controller.RTouch) {
			return Side.Right;
		}
		else if (controller == OVRInput.Controller.None) {
			return Side.None;
		}
		else if (controller == OVRInput.Controller.All) {
			return Side.Both;
		}

		return Side.None;
	}

	public static OVRInput.Controller Side2OVRController(Side side) {
		if (side == Side.Left) {
			return OVRInput.Controller.LTouch;
		}
		else if (side == Side.Right) {
			return OVRInput.Controller.RTouch;
		}
		else if (side == Side.None) {
			return OVRInput.Controller.None;
		}
		else if (side == Side.Both) {
			return OVRInput.Controller.All;
		}

		return OVRInput.Controller.None;
	}
#endif

	private static float GetRawValue(string key, Side side) {
		float left, right;
		switch (side) {
			case Side.Both:
				left = Input.GetAxis(key + Side.Left);
				right = Input.GetAxis(key + Side.Right);
				return Mathf.Abs(left) < Mathf.Abs(right) ? left : right;
			case Side.Either:
				left = Input.GetAxis(key + Side.Left);
				right = Input.GetAxis(key + Side.Right);
				return Mathf.Abs(left) > Mathf.Abs(right) ? left : right;
			case Side.None:
				return 0;
			default:
				return Input.GetAxis(key + side);
		}
	}

	private static bool GetRawValueDown(string key, Side side) {
		switch (side) {
			case Side.Both:
				return firstPressed[key + Side.Left][0] &&
				       firstPressed[key + Side.Right][0];
			case Side.Either:
				return firstPressed[key + Side.Left][0] ||
				       firstPressed[key + Side.Right][0];
			case Side.None:
				return false;
			default:
				return firstPressed[key + side][0];
		}
	}
	
	private static bool GetRawButton(string key, Side side) {
		switch (side) {
			case Side.Both:
				return Input.GetButton(key + Side.Left) &&
				       Input.GetButton(key + Side.Right);
			case Side.Either:
				return Input.GetButton(key + Side.Left) ||
				       Input.GetButton(key + Side.Right);
			case Side.None:
				return false;
			default:
				return Input.GetButton(key + side);
		}
	}

	private static bool GetRawButtonDown(string key, Side side) {
		switch (side) {
			case Side.Both:
				return Input.GetButtonDown(key + Side.Left) &&
				       Input.GetButtonDown(key + Side.Right);
			case Side.Either:
				return Input.GetButtonDown(key + Side.Left) ||
				       Input.GetButtonDown(key + Side.Right);
			case Side.None:
				return false;
			default:
				return Input.GetButtonDown(key + side);
		}
	}
	
	private static bool GetRawButtonUp(string key, Side side) {
		switch (side) {
			case Side.Both:
				return Input.GetButtonUp(key + Side.Left) &&
				       Input.GetButtonUp(key + Side.Right);
			case Side.Either:
				return Input.GetButtonUp(key + Side.Left) ||
				       Input.GetButtonUp(key + Side.Right);
			case Side.None:
				return false;
			default:
				return Input.GetButtonUp(key + side);
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
			UpdateDictionaryDirection(ThumbstickY((Side) i) < -thumbstickThreshold, "VR_Thumbstick_Y_Up_" + (Side) i);
			UpdateDictionaryDirection(ThumbstickY((Side) i) > thumbstickThreshold, "VR_Thumbstick_Y_Down_" + (Side) i);
		}
	}
}
