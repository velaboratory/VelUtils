using UnityEngine;
using VelUtils.Interaction.WorldMouse;

public class WorldMouseFindCanvasesOnEnable : MonoBehaviour
{
	private void OnEnable()
	{
		WorldMouseInputModule.FindCanvases();
	}
}