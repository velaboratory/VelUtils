#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VelUtils.Interaction.WorldMouse
{
    /*
#if UNITY_EDITOR
	[CustomEditor(typeof(WorldMouseGaze))]
	[CanEditMultipleObjects]
	public class WorldMouseGazeEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Provides a gaze pointer as well as interaction with UI.");
			EditorGUILayout.Space();

			base.OnInspectorGUI();
		}
	}
#endif
*/
	public class WorldMouseGaze : WorldMouse
	{
		public VRInput input = VRInput.Trigger;
		public Side side;
        public GameObject cursor;
		public float gazeSelectCooldown;
		bool canSelect;

		public bool isHovering = false;
		private float targetHoverTime; // Seconds hovering required to activate button
		public float defaultHoverTime;
		[SerializeField]
		public float currentHoverTime = 0f;

		public GameObject currentHoverObj;
		public GameObject lastHoverObj;

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
			targetHoverTime = defaultHoverTime;
			if (useLaser)
			{
				CreateLaser();
			}

			OnClickDown += OnClicked;
			OnHoverStartWithData += OnHoverBegin;
			OnHoverStay+=OnHovering;
            OnHoverStop += OnHoverExit;
		}


		private void OnHoverBegin(PointerEventData data)
		{
			
			currentHoverTime=0f;
			targetHoverTime=defaultHoverTime;
			isHovering = true;
			GameObject target;
			if (data.pointerCurrentRaycast.gameObject != null) {

				if (lastHoverObj == null) {
					lastHoverObj = data.pointerCurrentRaycast.gameObject;
				}
				currentHoverObj = data.pointerCurrentRaycast.gameObject;

				target = data.pointerCurrentRaycast.gameObject;
				if (target.GetComponent<GazeButtonOverride>() != null) {
					GazeButtonOverride gaze = target.GetComponent<GazeButtonOverride>();
					targetHoverTime = gaze.HoverTime;
				}


			} else {
				target = null;
			}
            
		}

		private void OnHovering(PointerEventData data) {
			if (lastHoverObj == null) {
				lastHoverObj = data.pointerCurrentRaycast.gameObject;
			}
			currentHoverObj = data.pointerCurrentRaycast.gameObject;
			if (lastHoverObj!=currentHoverObj) {
				ResetCounter();
				lastHoverObj=currentHoverObj;
				
			} else {			
				DoHoverCount();
			}
			
		}

        private void OnHoverExit()
		{	
            Debug.Log("HOVEREXIT");
			isHovering = false;
			currentHoverTime=0f;
			targetHoverTime=defaultHoverTime;
			
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

		public void ResetCanSelect() {
			canSelect = true;
		}

		void DoHoverCount()
		{
			currentHoverTime += Time.deltaTime;

			if (currentHoverTime >= targetHoverTime)
			{
				Debug.Log("Loading Complete!");
				Press();
				Release();
				ResetCounter();
			}
		}

		void ResetCounter()
		{
			currentHoverTime = 0f;
			targetHoverTime=defaultHoverTime;
		}

		
	}
}
