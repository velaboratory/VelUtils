#undef STEAMVR_AVAILABLE // change to #define or #undef if SteamVR utilites are installed
#define OCULUS_UTILITIES_AVAILABLE

using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using unityutilities;
using IEnumerator = System.Collections.IEnumerator;

#if STEAMVR_AVAILABLE
using Valve.VR;
#endif

public enum HeadsetType
{
	None,
	Oculus,
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

	private enum InputStrings {
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
		VR_Button2
	}

	private static readonly Dictionary<InputStrings, string[]> inputManagerStrings = new Dictionary<InputStrings,string[]>() {
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
		{InputStrings.VR_Button2, new[] {"VR_Button2_Left", "VR_Button2_Right"}},
		
	};

	public static Side DominantHand { get; set; }

	public static Side NonDominantHand {
		get {
			switch (DominantHand) {
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

	private InputDevice[] xRNodes;
	private XRNodeState[] xrNodeStates;

	private static InputDevice GetXRNode(Side side) {
		return InputDevices.GetDeviceAtXRNode(side == Side.Left ? XRNode.LeftHand : XRNode.RightHand);
	}

	private static XRNodeState GetXRNodeState(Side side)
	{
		List<XRNodeState> nodes = new List<XRNodeState>();
		InputTracking.GetNodeStates(nodes);

		foreach (XRNodeState ns in nodes) {
			if (side == Side.Left && ns.nodeType == XRNode.LeftHand)
			{
				return ns;
			}

			if (side == Side.Right && ns.nodeType == XRNode.RightHand)
			{
				return ns;
			}
		}
		return nodes[0];
	}

	private static InputMan instance;
	private static bool init;

	private static void Init() {
		if (instance != null) {
			Destroy(instance.gameObject);
		}
		
		instance = new GameObject("InputMan").AddComponent<InputMan>();
		DontDestroyOnLoad(instance.gameObject);
		
		init = true;
	}

	private void Awake() {
		if (instance != null && instance != this) {
			Destroy(gameObject);
		}
		else {
			instance = this;
			init = true;
		}

#if STEAMVR_AVAILABLE
			VRPackageInUse = VRPackage.SteamVR;
#endif
#if OCULUS_UTILITIES_AVAILABLE
		VRPackageInUse = VRPackage.Oculus;
#endif

		Debug.Log("InputMan loaded device: " + XRSettings.loadedDeviceName, instance);

		if (XRDevice.model.Contains("Oculus")) {
			headsetType = HeadsetType.Oculus;
		}
		else if (XRDevice.model.Contains("Vive")) {
			headsetType = HeadsetType.Vive;
		}
		else if (XRDevice.model.Contains("Mixed") || XRDevice.model.Contains("WMR")) {
			headsetType = HeadsetType.WMR;
		}

		firstPressed.Add(InputStrings.VR_Trigger, new bool[2, 3]);
		firstPressed.Add(InputStrings.VR_Grip, new bool[2, 3]);
		firstPressed.Add(InputStrings.VR_Thumbstick_X_Left, new bool[2, 3]);
		firstPressed.Add(InputStrings.VR_Thumbstick_X_Right, new bool[2, 3]);
		firstPressed.Add(InputStrings.VR_Thumbstick_Y_Up, new bool[2, 3]);
		firstPressed.Add(InputStrings.VR_Thumbstick_Y_Down, new bool[2, 3]);

		directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_X_Left, new float[] {0, 0});
		directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_X_Right, new float[] {0, 0});
		directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_Y_Up, new float[] {0, 0});
		directionalTimeoutValue.Add(InputStrings.VR_Thumbstick_Y_Down, new float[] {0, 0});
	}

	#region navigation vars

	/// <summary>
	/// Contains a pair of bools for each axis input that can act as a button.
	/// The first is true only for the first frame the axis is active
	/// The second is true only for the first frame the axis is inactive
	/// The third remains true when the button is held.
	/// 	it represents whether the button was already down last frame
	/// </summary>
	private static Dictionary<InputStrings, bool[,]> firstPressed = new Dictionary<InputStrings, bool[,]>();

	// the distance necessary to count as a "press"
	public static float triggerThreshold = .5f;
	public static float gripThreshold = .5f;
	public static float touchpadThreshold = .5f;
	public static float thumbstickThreshold = .5f;
	public static float thumbstickIdleThreshold = .1f;
	public static float directionalTimeout = 1f;
	
	private static Dictionary<InputStrings, float[]> directionalTimeoutValue = new Dictionary<InputStrings, float[]>();

	#endregion

	#region Trigger

	public static float TriggerValue(Side side = Side.Either) {
		return GetRawValue(InputStrings.VR_Trigger, side);
	}

	public static bool Trigger(Side side = Side.Either) {
		return TriggerValue(side) > triggerThreshold;
	}
	
	public static bool TriggerDown(Side side = Side.Either) {
		return GetRawValueDown(InputStrings.VR_Trigger, side);
	}

	public static bool TriggerUp(Side side = Side.Either) {
		return GetRawValueUp(InputStrings.VR_Trigger, side);
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
		return GetRawValue(InputStrings.VR_Grip, side);
	}
	public static bool Grip(Side side = Side.Either) {
		return GripValue(side) > gripThreshold;
	}
	public static bool GripDown(Side side = Side.Either) {
		return GetRawValueDown(InputStrings.VR_Grip, side);
	}

	#endregion

	#region Thumbstick/Touchpad
	
	public static bool ThumbstickPress(Side side = Side.Either) {
		return GetRawButton(InputStrings.VR_Thumbstick_Press, side);
	}

	public static bool ThumbstickPressDown(Side side = Side.Either) {
		return GetRawButtonDown(InputStrings.VR_Thumbstick_Press, side);
	}
	
	public static bool ThumbstickPressUp(Side side = Side.Either) {
		return GetRawButtonUp(InputStrings.VR_Thumbstick_Press, side);
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
		return GetRawValue(InputStrings.VR_Thumbstick_X, side);
	}

	public static float ThumbstickY(Side side = Side.Either) {
		return GetRawValue(InputStrings.VR_Thumbstick_Y, side);
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
		return Button1(side);
	}

	public static bool MenuButtonDown(Side side = Side.Either) {
		return Button1Down(side);
	}
	
	public static bool Button1(Side side = Side.Either) {
		return GetRawButton(InputStrings.VR_Button1, side);
	}

	public static bool Button1Down(Side side = Side.Either) {
		return GetRawButtonDown(InputStrings.VR_Button1, side);
	}
	
	public static bool Button2(Side side = Side.Either) {
		return GetRawButton(InputStrings.VR_Button2, side);
	}

	public static bool Button2Down(Side side = Side.Either) {
		return GetRawButtonDown(InputStrings.VR_Button2, side);
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
			return (firstPressed[InputStrings.VR_Thumbstick_Y_Up][0,0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Left))) ||
			       (firstPressed[InputStrings.VR_Thumbstick_Y_Up][1,0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed[InputStrings.VR_Thumbstick_Y_Up][(int)side,0] &&
			       (headsetType == HeadsetType.Oculus || ThumbstickPress(side));
		}
	}

	public static bool Down(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed[InputStrings.VR_Thumbstick_Y_Down][0, 0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Left))) ||
			       (firstPressed[InputStrings.VR_Thumbstick_Y_Down][1, 0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed[InputStrings.VR_Thumbstick_Y_Down][(int) side, 0] &&
			       (headsetType == HeadsetType.Oculus || ThumbstickPress(side));
		}
	}

	public static bool Left(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed[InputStrings.VR_Thumbstick_X_Left][0, 0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Left))) ||
			       (firstPressed[InputStrings.VR_Thumbstick_X_Left][1, 0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed[InputStrings.VR_Thumbstick_X_Left][(int) side, 0] &&
			       (headsetType == HeadsetType.Oculus || ThumbstickPress(side));
		}
	}

	public static bool Right(Side side = Side.Either) {
		if (side == Side.Both || side == Side.Either) {
			return (firstPressed[InputStrings.VR_Thumbstick_X_Right][0, 0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Left))) ||
			       (firstPressed[InputStrings.VR_Thumbstick_X_Right][1, 0] &&
			        (headsetType == HeadsetType.Oculus || ThumbstickPress(Side.Right)));
		}
		else {
			return firstPressed[InputStrings.VR_Thumbstick_X_Right][(int) side, 0] &&
			       (headsetType == HeadsetType.Oculus || ThumbstickPress(side));
		}
	}

	public static float Vertical(Side side = Side.Either) {
		if (headsetType == HeadsetType.Oculus) {
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
		if (headsetType == HeadsetType.Oculus) {
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
	/// <param name="duration">Duration of the vibration</param>
	/// <param name="delay">Time before the vibration starts</param>
	public static void Vibrate(Side side, float intensity, float duration = 1, float delay = 0) {
		if (!init) Init();

		if (delay > 0 && instance) {
			instance.StartVibrateDelay(side, intensity, duration, delay);
			return;
		}


		intensity = Mathf.Clamp01(intensity);

#if OCULUS_UTILITIES_AVAILABLE
		OVRHaptics.OVRHapticsChannel channel;
		switch (side) {
			case Side.Left:
				channel = OVRHaptics.LeftChannel;
				break;
			case Side.Right:
				channel = OVRHaptics.RightChannel;
				break;
			default:
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
	/// <param name="duration">Duration of the vibration</param>
	/// <param name="delay">Time before the vibration starts</param>
	public static void Vibrate(OVRInput.Controller side, float intensity, float duration = 1, float delay = 0) {
		Vibrate(OVRController2Side(side), intensity, duration, delay);
	}
#endif

	private void StartVibrateDelay(Side side, float intensity, float duration, float delay) {
		StartCoroutine(VibrateDelay(side, intensity, duration, delay));
	}

	private IEnumerator VibrateDelay(Side side, float intensity, float duration, float delay) {
		yield return new WaitForSeconds(delay);
		Vibrate(side, intensity, duration);
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

	private static float GetRawValue(InputStrings key, Side side) {
		if (!init) Init();
		
		float left, right;
		switch (side) {
			case Side.Both:
				left = Input.GetAxis(inputManagerStrings[key][(int) Side.Left]);
				right = Input.GetAxis(inputManagerStrings[key][(int) Side.Right]);
				return Mathf.Abs(left) < Mathf.Abs(right) ? left : right;
			case Side.Either:
				left = Input.GetAxis(inputManagerStrings[key][(int) Side.Left]);
				right = Input.GetAxis(inputManagerStrings[key][(int) Side.Right]);
				return Mathf.Abs(left) > Mathf.Abs(right) ? left : right;
			case Side.None:
				return 0;
			default:
				return Input.GetAxis(inputManagerStrings[key][(int) side]);
		}
	}

	private static bool GetRawValueDown(InputStrings key, Side side) {
		if (!init) Init();
		
		switch (side) {
			case Side.Both:
				return firstPressed[key][0, 0] &&
				       firstPressed[key][1, 0];
			case Side.Either:
				return firstPressed[key][0, 0] ||
				       firstPressed[key][1, 0];
			case Side.None:
				return false;
			default:
				return firstPressed[key][(int) side, 0];
		}
	}

	private static bool GetRawValueUp(InputStrings key, Side side) {
		if (!init) Init();

		switch (side) {
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

	private static bool GetRawButton(InputStrings key, Side side) {
		if (!init) Init();
		
		switch (side) {
			case Side.Both:
				return Input.GetButton(inputManagerStrings[key][(int) Side.Left]) &&
				       Input.GetButton(inputManagerStrings[key][(int) Side.Right]);
			case Side.Either:
				return Input.GetButton(inputManagerStrings[key][(int) Side.Left]) ||
				       Input.GetButton(inputManagerStrings[key][(int) Side.Right]);
			case Side.None:
				return false;
			default:
				return Input.GetButton(inputManagerStrings[key][(int) side]);
		}
	}

	private static bool GetRawButtonDown(InputStrings key, Side side) {
		if (!init) Init();
		
		switch (side) {
			case Side.Both:
				return Input.GetButtonDown(inputManagerStrings[key][(int) Side.Left]) &&
				       Input.GetButtonDown(inputManagerStrings[key][(int) Side.Right]);
			case Side.Either:
				return Input.GetButtonDown(inputManagerStrings[key][(int) Side.Left]) ||
				       Input.GetButtonDown(inputManagerStrings[key][(int) Side.Right]);
			case Side.None:
				return false;
			default:
				return Input.GetButtonDown(inputManagerStrings[key][(int) side]);
		}
	}
	
	private static bool GetRawButtonUp(InputStrings key, Side side) {
		if (!init) Init();
		
		switch (side) {
			case Side.Both:
				return Input.GetButtonUp(inputManagerStrings[key][(int) Side.Left]) &&
				       Input.GetButtonUp(inputManagerStrings[key][(int) Side.Right]);
			case Side.Either:
				return Input.GetButtonUp(inputManagerStrings[key][(int) Side.Left]) ||
				       Input.GetButtonUp(inputManagerStrings[key][(int) Side.Right]);
			case Side.None:
				return false;
			default:
				return Input.GetButtonUp(inputManagerStrings[key][(int) side]);
		}
	}

	private void UpdateDictionary(bool currentVal, int side, InputStrings key) {
		// if down right now
		if (currentVal) {
			// if it wasn't down last frame
			if (!firstPressed[key][side, 2] && !firstPressed[key][side, 0]) {
				// activate the down event 
				firstPressed[key][side, 0] = true;
			}
			else {
				// deactive the down event
				firstPressed[key][side, 0] = false;
			}
			// save that the input was down for next frame
			firstPressed[key][side, 2] = true;
		}
		// if up right now
		else {
			// if it wasn't up last frame
			if (firstPressed[key][side, 2] && !firstPressed[key][side, 1]) {
				// activate the up event
				firstPressed[key][side, 1] = true;
			}
			else {
				// deactivate the up event
				firstPressed[key][side, 1] = false;
			}
			// save that the input was up for next frame
			firstPressed[key][side, 2] = false;

			firstPressed[key][side, 0] = false;
		}
	}

	private void UpdateDictionaryDirection(bool currentVal, int side, InputStrings key) {
		if (currentVal) {
			if (directionalTimeoutValue[key][side] > directionalTimeout) {
				firstPressed[key][side, 1] = false;
				directionalTimeoutValue[key][side] = 0;
			}

			//directionalTimeoutValue[key][side] += Time.deltaTime;
		}
		else {
			directionalTimeoutValue[key][side] = 0;
		}

		UpdateDictionary(currentVal, side, key);
	}

	private void Update()
	{
		
		for (int i = 0; i < 2; i++)
		{
			UpdateDictionary(Trigger((Side) i), i, InputStrings.VR_Trigger);
			UpdateDictionary(Grip((Side) i), i,InputStrings.VR_Grip);


			UpdateDictionaryDirection(ThumbstickX((Side)i) < -thumbstickThreshold, i, InputStrings.VR_Thumbstick_X_Left);
			UpdateDictionaryDirection(ThumbstickX((Side)i) > thumbstickThreshold, i, InputStrings.VR_Thumbstick_X_Right);
			UpdateDictionaryDirection(ThumbstickY((Side)i) < -thumbstickThreshold, i, InputStrings.VR_Thumbstick_Y_Up);
			UpdateDictionaryDirection(ThumbstickY((Side)i) > thumbstickThreshold, i, InputStrings.VR_Thumbstick_Y_Down);
		}
	}
}
