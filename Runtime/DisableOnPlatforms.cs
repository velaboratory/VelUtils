using UnityEngine;

namespace VelUtils
{
	/// <summary>
	/// Sets the object as enabled on the specified platforms on Awake() using preprocessor statements
	/// </summary>
	public class DisableOnPlatforms : MonoBehaviour
	{
		[Header("This object is enabled on:")] public bool android = true;
		public bool desktop = true;
		public bool webgl = true;
		public bool editor = true;

		private void Awake()
		{
#if UNITY_EDITOR
			gameObject.SetActive(editor);
#elif UNITY_ANDROID
		gameObject.SetActive(android);
#elif UNITY_WEBGL
        gameObject.SetActive(webgl);
#else
        gameObject.SetActive(desktop);
#endif
		}
	}
}