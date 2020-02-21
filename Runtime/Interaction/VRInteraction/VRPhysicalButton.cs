using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Button;

namespace unityutilities.VRInteraction
{
	public class VRPhysicalButton : MonoBehaviour
	{
		[Tooltip("Distance between button activation and bottoming out.")]
		public float depth = .01f;
		[ReadOnly]
		public float normalHeight;
		public Rigidbody rb;
		public float forceMultiplier = 1;
		private bool clicked;
		private Color normalColor;
		public Color clickedColor;
		public Renderer rend;
		public AudioSource sound;
		[Space]
		public ButtonClickedEvent onClick;

		void Start()
		{
			rb = GetComponentInChildren<Rigidbody>();
			rb.constraints = RigidbodyConstraints.FreezeAll ^ RigidbodyConstraints.FreezePositionY;
			if (!rend)
			{
				rend = rb.GetComponentInChildren<MeshRenderer>();
			}
			if (rend)
			{
				normalColor = rend.sharedMaterial.color;
			}

			normalHeight = rb.transform.localPosition.y;
			if (normalHeight < 2 * depth)
			{
				Debug.LogError("Button depth more than half the normal height.");
			}
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			float currentPos = rb.transform.localPosition.y;
			if (!clicked && currentPos < depth)
			{
				onClick.Invoke();
				if (sound)
				{
					sound.Play();
				}
				clicked = true;

				if (rend)
				{
					rend.material.color = clickedColor;
				}
			}
			else if (currentPos > normalHeight - depth)
			{
				clicked = false;

				if (rend)
				{
					rend.material.color = normalColor;
				}
			}

			// limit movement
			if (currentPos < 0)
			{
				rb.transform.localPosition = new Vector3(0, 0, 0);
				rb.velocity = Vector3.zero;
			}
			else if (currentPos > normalHeight)
			{
				rb.transform.localPosition = new Vector3(0, normalHeight, 0);
				rb.velocity = Vector3.zero;
			}

			if (currentPos < normalHeight - depth)
			{
				rb.AddForce(0, 500 * Time.fixedDeltaTime * forceMultiplier, 0);
			}
		}
	}


#if UNITY_EDITOR
	/// <summary>
	/// Sets up the interface for the CopyTransform script.
	/// </summary>
	[CustomEditor(typeof(VRPhysicalButton))]
	public class VRPhysicalButtonEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var button = target as VRPhysicalButton;

			DrawDefaultInspector();

			if (GUILayout.Button("Click Button"))
			{
				button.onClick.Invoke();
			}

		}
	}
#endif

}