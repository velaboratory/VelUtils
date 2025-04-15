using UnityEngine;
using UnityEngine.UI;
using VelUtils;
using VelUtils.Interaction.WorldMouse;

[RequireComponent(typeof(WorldMouse))]
public class WorldMouseVibrations : MonoBehaviour
{
	private WorldMouse worldMouse;

	public Side side;

	[Header("On Click")] public float vibrateOnClick = .5f;
	public AudioClip soundOnClick;

	[Header("On Hover")] public float vibrateOnHover = .1f;
	public AudioClip soundOnHover;

	// Start is called before the first frame update
	private void Start()
	{
		worldMouse = GetComponent<WorldMouse>();
		worldMouse.OnClickDown += OnClicked;
		worldMouse.OnHoverStart += OnHover;
	}


	private void OnHover(GameObject obj)
	{
		if (obj != null && obj.GetComponentInParent<Selectable>() != null)
		{
			InputMan.Vibrate(side, vibrateOnHover);
			if (soundOnHover != null) AudioSource.PlayClipAtPoint(soundOnHover, obj.transform.position, .5f);
		}
	}

	private void OnClicked(GameObject obj)
	{
		if (obj != null && obj.GetComponentInParent<Selectable>() != null)
		{
			InputMan.Vibrate(side, vibrateOnClick);
			if (soundOnClick != null) AudioSource.PlayClipAtPoint(soundOnClick, obj.transform.position, .5f);
		}
	}
}