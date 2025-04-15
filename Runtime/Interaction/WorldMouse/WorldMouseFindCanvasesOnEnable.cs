using UnityEngine;
using CustomVelUtils.Interaction.WorldMouse;

public class WorldMouseFindCanvasesOnEnable : MonoBehaviour
{
	private void OnEnable()
	{
		WorldMouseInputModule.FindCanvases();
	}
}