using UnityEditor;
using UnityEngine;

namespace unityutilities
{

#if UNITY_EDITOR
	[CustomEditor(typeof(UUWorldMouse))]
	[CanEditMultipleObjects]
	public class UUWorldMouseEditor : Editor
	{
		UUWorldMouse uuWorldMouse;

		private void OnEnable()
		{
			uuWorldMouse = target as UUWorldMouse;
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Provides a laser pointer as well as interaction with UI.");
			EditorGUILayout.Space();

			base.OnInspectorGUI();
		}
	}
#endif

	public class UUWorldMouse : WorldMouse
	{
		public VRInput input = VRInput.Trigger;

		[Space]

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

		void Start()
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

		private void Update()
		{
			base.Update();
			if (rayDistance < Mathf.Infinity)
			{
				SetLaserLength(rayDistance);
				ShowLaser(true);
			}
			ShowLaserUpdate();
		}

		public override bool PressDown()
		{
			return InputMan.GetDown(input);
		}

		public override bool PressUp()
		{
			return InputMan.GetUp(input);
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
			lineRend.gameObject.SetActive(showThisFrame);
			showThisFrame = false;
		}

		public void SetLaserLength(float length)
		{
			lineRend.SetPositions(new Vector3[] { transform.position, transform.position + transform.forward * length });
		}
	}
}