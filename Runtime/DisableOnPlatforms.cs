using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableOnPlatforms : MonoBehaviour
{
	[Header("This object is enabled on:")] 
	public bool android = true;
	public bool desktop = true;
	public bool webgl = true;
	public bool editor = true;

	// Start is called before the first frame update
	private void Start()
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