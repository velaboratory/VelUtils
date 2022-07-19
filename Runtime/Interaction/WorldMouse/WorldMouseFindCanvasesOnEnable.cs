using UnityEngine;
using unityutilities.Interaction.WorldMouse;

public class WorldMouseFindCanvasesOnEnable : MonoBehaviour
{
	private void OnEnable()
	{
		WorldMouseInputModule.FindCanvases();
	}
}