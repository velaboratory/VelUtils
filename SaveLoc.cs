using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace unityutilities
{
	/// <summary>
	/// Saves the location and rotation of an object to playerprefs. Local or global coordinates.
	/// </summary>
	public class SaveLoc : MonoBehaviour
	{
		public Space coordinateSystem = Space.Self;

		void Start()
		{
			if (coordinateSystem == Space.Self)
			{
				transform.localPosition = new Vector3(
					PlayerPrefs.GetFloat(name + "_xLPos", transform.localPosition.x),
					PlayerPrefs.GetFloat(name + "_yLPos", transform.localPosition.y),
					PlayerPrefs.GetFloat(name + "_zLPos", transform.localPosition.z));
				transform.eulerAngles = new Vector3(
					PlayerPrefs.GetFloat(name + "_xLRot", transform.localEulerAngles.x),
					PlayerPrefs.GetFloat(name + "_yLRot", transform.localEulerAngles.y),
					PlayerPrefs.GetFloat(name + "_zLRot", transform.localEulerAngles.z));
			}
			else if (coordinateSystem == Space.World)
			{
				transform.position = new Vector3(
					PlayerPrefs.GetFloat(name + "_xPos", transform.position.x),
					PlayerPrefs.GetFloat(name + "_yPos", transform.position.y),
					PlayerPrefs.GetFloat(name + "_zPos", transform.position.z));
				transform.eulerAngles = new Vector3(
					PlayerPrefs.GetFloat(name + "_xRot", transform.eulerAngles.x),
					PlayerPrefs.GetFloat(name + "_yRot", transform.eulerAngles.y),
					PlayerPrefs.GetFloat(name + "_zRot", transform.eulerAngles.z));
			}
		}

		private void OnApplicationQuit()
		{
			if (coordinateSystem == Space.Self)
			{
				PlayerPrefs.SetFloat(name + "_xLPos", transform.localPosition.x);
				PlayerPrefs.SetFloat(name + "_yLPos", transform.localPosition.y);
				PlayerPrefs.SetFloat(name + "_zLPos", transform.localPosition.z);
				PlayerPrefs.SetFloat(name + "_xLRot", transform.localEulerAngles.x);
				PlayerPrefs.SetFloat(name + "_yLRot", transform.localEulerAngles.y);
				PlayerPrefs.SetFloat(name + "_zLRot", transform.localEulerAngles.z);
			}
			else if (coordinateSystem == Space.World)
			{
				PlayerPrefs.SetFloat(name + "_xPos", transform.position.x);
				PlayerPrefs.SetFloat(name + "_yPos", transform.position.y);
				PlayerPrefs.SetFloat(name + "_zPos", transform.position.z);
				PlayerPrefs.SetFloat(name + "_xRot", transform.eulerAngles.x);
				PlayerPrefs.SetFloat(name + "_yRot", transform.eulerAngles.y);
				PlayerPrefs.SetFloat(name + "_zRot", transform.eulerAngles.z);
			}
		}
	}
}