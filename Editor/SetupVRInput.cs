using UnityEditor;
using UnityEngine;

public enum AxisType
{
	KeyOrMouseButton = 0,
	MouseMovement = 1,
	JoystickAxis = 2
};

public class InputAxis
{
	public string name;
	public string descriptiveName;
	public string descriptiveNegativeName;
	public string negativeButton;
	public string positiveButton;
	public string altNegativeButton;
	public string altPositiveButton;

	public float gravity;
	public float dead;
	public float sensitivity;

	public bool snap = false;
	public bool invert = false;

	public AxisType type;

	public int axis;
	public int joyNum;
}

public class SetupVRInput : MonoBehaviour
{
	// Add a menu item named "Do Something" to MyMenu in the menu bar.
	[MenuItem("Edit/Project Settings/SetupVRInput", false, 2)]
	static void DoSetupVRInput()
	{
		SetupInputManager();
	}

	private static SerializedProperty GetChildProperty(SerializedProperty parent, string name)
	{
		SerializedProperty child = parent.Copy();
		child.Next(true);
		do
		{
			if (child.name == name)
			{
				return child;
			}
		} while (child.Next(false));

		return null;
	}

	private static bool AxisDefined(string axisName)
	{
		SerializedObject serializedObject =
			new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
		SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

		axesProperty.Next(true);
		axesProperty.Next(true);
		while (axesProperty.Next(false))
		{
			SerializedProperty axis = axesProperty.Copy();
			axis.Next(true);
			if (axis.stringValue == axisName)
			{
				return true;
			}
		}

		return false;
	}


	private static void AddAxis(InputAxis axis)
	{
		if (AxisDefined(axis.name)) return;

		SerializedObject serializedObject =
			new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
		SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

		axesProperty.arraySize++;
		serializedObject.ApplyModifiedProperties();

		SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);

		GetChildProperty(axisProperty, "m_Name").stringValue = axis.name;
		GetChildProperty(axisProperty, "descriptiveName").stringValue = axis.descriptiveName;
		GetChildProperty(axisProperty, "descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
		GetChildProperty(axisProperty, "negativeButton").stringValue = axis.negativeButton;
		GetChildProperty(axisProperty, "positiveButton").stringValue = axis.positiveButton;
		GetChildProperty(axisProperty, "altNegativeButton").stringValue = axis.altNegativeButton;
		GetChildProperty(axisProperty, "altPositiveButton").stringValue = axis.altPositiveButton;
		GetChildProperty(axisProperty, "gravity").floatValue = axis.gravity;
		GetChildProperty(axisProperty, "dead").floatValue = axis.dead;
		GetChildProperty(axisProperty, "sensitivity").floatValue = axis.sensitivity;
		GetChildProperty(axisProperty, "snap").boolValue = axis.snap;
		GetChildProperty(axisProperty, "invert").boolValue = axis.invert;
		GetChildProperty(axisProperty, "type").intValue = (int) axis.type;
		GetChildProperty(axisProperty, "axis").intValue = axis.axis - 1;
		GetChildProperty(axisProperty, "joyNum").intValue = axis.joyNum;

		serializedObject.ApplyModifiedProperties();
	}

	public static void SetupInputManager()
	{
		// thumbstick
		AddAxis(new InputAxis()
		{
			name = "VR_Thumbstick_X_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 1
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_Thumbstick_X_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 4
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_Thumbstick_Y_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 2
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_Thumbstick_Y_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 5
		});
		
		// trigger
		AddAxis(new InputAxis()
		{
			name = "VR_Trigger_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 9
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_Trigger_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 10
		});
		
		// grip button
		AddAxis(new InputAxis()
		{
			name = "VR_Grip_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 11
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_Grip_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.JoystickAxis,
			axis = 12
		});
		
		// top buttons
		AddAxis(new InputAxis()
		{
			name = "VR_SecondButton_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.KeyOrMouseButton,
			positiveButton = "joystick button 3"
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_SecondButton_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.KeyOrMouseButton,
			positiveButton = "joystick button 2"
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_MenuButton_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.KeyOrMouseButton,
			positiveButton = "joystick button 2"
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_MenuButton_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.KeyOrMouseButton,
			positiveButton = "joystick button 0"
		});
		
		// thumbstick/touchpad click
		AddAxis(new InputAxis()
		{
			name = "VR_Thumbstick_Press_Left",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.KeyOrMouseButton,
			positiveButton = "joystick button 8"
		});
		
		AddAxis(new InputAxis()
		{
			name = "VR_Thumbstick_Press_Right",
			dead = 0.001f,
			sensitivity = 1f,
			type = AxisType.KeyOrMouseButton,
			positiveButton = "joystick button 9"
		});
	}
}