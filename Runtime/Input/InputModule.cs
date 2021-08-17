using UnityEngine;

namespace unityutilities
{
	public abstract class InputModule : MonoBehaviour
	{
		public abstract void Vibrate(Side side, float intensity, float duration);

		public abstract float GetRawValue(InputStrings key, Side side);

		/// <summary>
		/// Returns true if the axis is past the threshold
		/// </summary>
		/// <param name="key"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public abstract bool GetRaw(InputStrings key, Side side);

		public abstract bool GetRawDown(InputStrings key, Side side);

		public abstract bool GetRawUp(InputStrings key, Side side);

		public abstract Vector3 ControllerVelocity(Side side, Space space);

		public abstract Vector3 ControllerAngularVelocity(Side side, Space space);
	}
}