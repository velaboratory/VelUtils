#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace unityutilities.Interaction.WorldMouse
{
#if UNITY_EDITOR
	[CustomEditor(typeof(WorldMouseWithLaser))]
	[CanEditMultipleObjects]
	public class WorldMouseWithLaserEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Provides a laser pointer as well as interaction with UI.");
			EditorGUILayout.Space();

			base.OnInspectorGUI();
		}
	}
#endif

	public class WorldMouseWithLaser : WorldMouse
	{
		public VRInput input = VRInput.Trigger;
		public Side side;


		[Space] public bool useLaser = true;

		/// <summary>
		/// Default laser color. Can't be changed during runtime
		/// </summary>
		public Color laserColor = Color.black;

		/// <summary>
		/// Default laser width. Can't be changed during runtime
		/// </summary>
		public float laserThickness = .005f;

		private LineRenderer lineRend;

		private bool showThisFrame = false;


		[Header("On Click")] public float vibrateOnClick = .5f;
		public AudioClip soundOnClick;

		[Header("On Hover")] public float vibrateOnHover = .1f;
		public AudioClip soundOnHover;

		protected void Start()
		{
			if (useLaser)
			{
				CreateLaser();
			}

			OnClickDown += OnClicked;
			OnHoverStart += OnHover;
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

		private void CreateLaser()
		{
			lineRend = new GameObject("UI Interaction").AddComponent<LineRenderer>();
			lineRend.transform.SetParent(transform);
			lineRend.transform.localPosition = Vector3.zero;
			lineRend.transform.localRotation = Quaternion.identity;
			lineRend.widthMultiplier = laserThickness;
			lineRend.positionCount = 2;
			lineRend.material = new Material(Shader.Find("Unlit/Color"))
			{
				color = laserColor
			};
		}

		protected override void Update()
		{
			if (useLaser)
			{
				if (currentRayLength < Mathf.Infinity)
				{
					SetLaserLength(currentRayLength);
					ShowLaser(true);
				}

				ShowLaserUpdate();
			}

			if (InputMan.GetDown(input, side))
			{
				Press();
			}

			if (InputMan.GetUp(input, side))
			{
				Release();
			}
			
			base.Update();
		}

		public void ShowLaser(bool show)
		{
			if (show)
			{
				showThisFrame = true;
			}
		}

		/// <summary>
		/// Called once an update to actually show the laser or not.
		/// </summary>
		private void ShowLaserUpdate()
		{
			if (lineRend == null) CreateLaser();
			lineRend.gameObject.SetActive(showThisFrame);
			showThisFrame = false;
		}

		public void SetLaserLength(float length)
		{
			if (lineRend == null) CreateLaser();
			lineRend.SetPositions(new Vector3[] { transform.position, transform.position + transform.forward * length });
		}
	}
}