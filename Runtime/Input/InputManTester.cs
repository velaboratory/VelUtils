using UnityEngine;

namespace VelUtils
{
	[AddComponentMenu("VelUtils/InputManTester")]
	public class InputManTester : MonoBehaviour
	{
		[Header("Trigger")] 
		[ReadOnly] [Range(0, 1)] public float leftTrigger;
		[ReadOnly] [Range(0, 1)] public float rightTrigger;
		[ReadOnly] public bool leftTriggerButton;
		[ReadOnly] public bool rightTriggerButton;
		[ReadOnly] public bool leftTriggerDown;
		[ReadOnly] public bool rightTriggerDown;

		[Header("Grip")] 
		[ReadOnly] [Range(0, 1)] public float leftGrip;
		[ReadOnly] [Range(0, 1)] public float rightGrip;

		[Header("Thumbstick")]
		[ReadOnly] [Range(-1, 1)] public float leftThumbstickX;
		[ReadOnly] [Range(-1, 1)] public float leftThumbstickY;
		[ReadOnly] [Range(-1, 1)] public float rightThumbstickX;
		[ReadOnly] [Range(-1, 1)] public float rightThumbstickY;
		[Space]
		[ReadOnly] public bool left;
		[ReadOnly] public bool right;
		[ReadOnly] public bool up;
		[ReadOnly] public bool down;
		[Space]
		[ReadOnly] public bool leftThumbstickPress;
		[ReadOnly] public bool rightThumbstickPress;

		[Header("Buttons")] 
		[ReadOnly] public bool leftButton1;
		[ReadOnly] public bool rightButton1;
		[ReadOnly] public bool leftButton2;
		[ReadOnly] public bool rightButton2;


		// Update is called once per frame
		private void Update()
		{
			leftTrigger = InputMan.TriggerValue(Side.Left);
			rightTrigger = InputMan.TriggerValue(Side.Right);
			leftTriggerButton = InputMan.Trigger(Side.Left);
			rightTriggerButton = InputMan.Trigger(Side.Right);
			leftTriggerDown = InputMan.TriggerDown(Side.Left);
			rightTriggerDown = InputMan.TriggerDown(Side.Right);
			
			leftGrip = InputMan.GripValue(Side.Left);
			rightGrip = InputMan.GripValue(Side.Right);
			
			leftThumbstickX = InputMan.ThumbstickX(Side.Left);
			leftThumbstickY = InputMan.ThumbstickY(Side.Left);
			rightThumbstickX = InputMan.ThumbstickX(Side.Right);
			rightThumbstickY = InputMan.ThumbstickY(Side.Right);
			
			
			left = InputMan.Left();
			right = InputMan.Right();
			up = InputMan.Up();
			down = InputMan.Down();
			
			leftThumbstickPress = InputMan.ThumbstickPress(Side.Left);
			rightThumbstickPress = InputMan.ThumbstickPress(Side.Right);

			leftButton1 = InputMan.Button1(Side.Left);
			rightButton1 = InputMan.Button1(Side.Right);
			leftButton2 = InputMan.Button2(Side.Left);
			rightButton2 = InputMan.Button2(Side.Right);
		}
	}
}