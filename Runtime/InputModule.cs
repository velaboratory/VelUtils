using UnityEngine;
using static unityutilities.InputMan;

namespace unityutilities
{
	public abstract class InputModule : MonoBehaviour
	{
		public abstract void Vibrate(Side side, float intensity, float duration);

		public abstract float GetRawValue(InputStrings key, Side side);

		public abstract bool GetRawValueDown(InputStrings key, Side side);

		public abstract bool GetRawValueUp(InputStrings key, Side side);

		/// <summary>
		/// Returns true if the axis is past the threshold
		/// </summary>
		/// <param name="key"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public abstract bool GetRaw(InputStrings key, Side side);

		public abstract bool GetRawButton(InputStrings key, Side side);

		public abstract bool GetRawButtonDown(InputStrings key, Side side);

		public abstract bool GetRawButtonUp(InputStrings key, Side side);

		public abstract Vector3 ControllerVelocity(Side side, Space space);

		public abstract Vector3 ControllerAngularVelocity(Side side, Space space);
	}
}