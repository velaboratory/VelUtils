using System.Linq;
using UnityEditor;
using UnityEngine;

namespace unityutilities.Editor
{
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

		public const bool snap = false;
		public const bool invert = false;

		public AxisType type;

		public int axis;
		public int joyNum;
	}

	/// <summary>
	/// Adds a dropdown menu option for automatically populating the Input system for use with the InputMan script for use with a VR device.
	/// </summary>
	public class SetupVRInput : EditorWindow
	{
		private static readonly InputAxis[] allAxes =
		{
			#region Thumbstick

			new InputAxis()
			{
				name = "VR_Thumbstick_X_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 1
			},
			new InputAxis()
			{
				name = "VR_Thumbstick_X_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 4
			},
			new InputAxis()
			{
				name = "VR_Thumbstick_Y_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 2
			},
			new InputAxis()
			{
				name = "VR_Thumbstick_Y_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 5
			},

			// secondary axis for WMR
			new InputAxis()
			{
				name = "VR_Thumbstick_X_Left",
				descriptiveName = "Secondary 2D Axis for WMR",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 17
			},
			new InputAxis()
			{
				name = "VR_Thumbstick_X_Right",
				descriptiveName = "Secondary 2D Axis for WMR",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 19
			},

			new InputAxis()
			{
				name = "VR_Thumbstick_Y_Left",
				descriptiveName = "Secondary 2D Axis for WMR",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 18
			},

			new InputAxis()
			{
				name = "VR_Thumbstick_Y_Right",
				descriptiveName = "Secondary 2D Axis for WMR",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 20
			},

			#endregion

			#region Trigger

			new InputAxis()
			{
				name = "VR_Trigger_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 9
			},

			new InputAxis()
			{
				name = "VR_Trigger_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 10
			},

			#endregion

			// grip button
			new InputAxis()
			{
				name = "VR_Grip_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 11
			},

			new InputAxis()
			{
				name = "VR_Grip_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.JoystickAxis,
				axis = 12
			},

			// top buttons
			new InputAxis()
			{
				name = "VR_Button2_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.KeyOrMouseButton,
				positiveButton = "joystick button 3"
			},

			new InputAxis()
			{
				name = "VR_Button2_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.KeyOrMouseButton,
				positiveButton = "joystick button 1"
			},

			new InputAxis()
			{
				name = "VR_Button1_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.KeyOrMouseButton,
				positiveButton = "joystick button 2"
			},

			new InputAxis()
			{
				name = "VR_Button1_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.KeyOrMouseButton,
				positiveButton = "joystick button 0"
			},

			// thumbstick/touchpad click
			new InputAxis()
			{
				name = "VR_Thumbstick_Press_Left",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.KeyOrMouseButton,
				positiveButton = "joystick button 8"
			},

			new InputAxis()
			{
				name = "VR_Thumbstick_Press_Right",
				dead = 0.001f,
				sensitivity = 1f,
				type = AxisType.KeyOrMouseButton,
				positiveButton = "joystick button 9"
			}
		};


		private static bool allDefined;

		[MenuItem("Window/Setup VR Input")]
		public static void ShowWindow()
		{
			allDefined = CheckAllDefined();
			GetWindow(typeof(SetupVRInput));
		}

		[InitializeOnLoadMethod]
		public static void AutoSetup()
		{
			allDefined = CheckAllDefined();
			if (!allDefined)
			{
				SetupInputManager();
			}
		}

		public void OnGUI()
		{
			GUILayout.Space(20);

			if (!allDefined)
			{
				GUILayout.Label("Not all required input axes are defined.\nClick the button to add them.");
			}

			if (GUILayout.Button("Add the missing inputs"))
			{
				SetupInputManager();
				allDefined = CheckAllDefined();
			}

			GUILayout.Space(20);

			if (GUILayout.Button("Refresh", GUILayout.MaxWidth(80)))
			{
				allDefined = CheckAllDefined();
			}
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

		private static bool AxisDefined(string axisName, string descriptiveName)
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
				if (axis.stringValue != axisName) continue;
				axis.Next(false);

				if ((string.IsNullOrEmpty(axis.stringValue) && string.IsNullOrEmpty(descriptiveName)) ||
				    axis.stringValue == descriptiveName) return true;
			}

			return false;
		}

		private static bool CheckAllDefined()
		{
			return allAxes.All(axis => AxisDefined(axis.name, axis.descriptiveName));
		}

		private static void AddAxis(InputAxis axis)
		{
			if (AxisDefined(axis.name, axis.descriptiveName)) return;

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
			GetChildProperty(axisProperty, "snap").boolValue = InputAxis.snap;
			GetChildProperty(axisProperty, "invert").boolValue = InputAxis.invert;
			GetChildProperty(axisProperty, "type").intValue = (int) axis.type;
			GetChildProperty(axisProperty, "axis").intValue = axis.axis - 1;
			GetChildProperty(axisProperty, "joyNum").intValue = axis.joyNum;

			serializedObject.ApplyModifiedProperties();
		}


		private static void SetupInputManager()
		{
			foreach (InputAxis axis in allAxes)
			{
				AddAxis(axis);
			}
		}
	}
}