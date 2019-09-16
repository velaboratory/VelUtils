using System;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Provides an example for how to use the controller help texts
	/// </summary>
	[AddComponentMenu("unityutilities/ControllerHelpTester")]
	public class ControllerHelpTester : MonoBehaviour
	{
		// Use this for initialization
		private void Start () {
			for (int i = 0; i<Enum.GetNames(typeof(ControllerHelp.ButtonHintType)).Length; i++)
			{
				ControllerHelp.ShowHint(Side.Both, (ControllerHelp.ButtonHintType)i, "TESTSETSET");
			}
		}
	}
}