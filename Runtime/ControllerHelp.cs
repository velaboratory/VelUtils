using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace unityutilities {
	[AddComponentMenu("unityutilities/Controller Help")]
	public class ControllerHelp : MonoBehaviour {
		public Rig rig;
		public GameObject labelPrefab;
		private List<ControllerLabel> instantiatedLabels = new List<ControllerLabel>();
		private bool initialized;

		private static ControllerHelp instance;

		[Tooltip("Button meshes for highlighting. Order should be:\nTrigger\nGrip\nThumbstick\nButton1\nButton2")]
		public Mesh[] leftButtonMeshes = new Mesh[5];
		[Tooltip("Button meshes for highlighting. Order should be:\nTrigger\nGrip\nThumbstick\nButton1\nButton2")]
		public Mesh[] rightButtonMeshes = new Mesh[5];

		public Material highlightMaterial;

		private static Dictionary<Side, Dictionary<ButtonHintType, Mesh>> buttonMeshesDict;


		// These offsets are only valid for the left hand. They are modified to fit the right hand.
		private readonly Dictionary<ButtonHintType, Vector3[]> riftOffsets = new Dictionary<ButtonHintType, Vector3[]>
		{
			// format for vector3 is origin, direction
			{ButtonHintType.Trigger, new[] {new Vector3(-0.0003f, -0.0209f, 0.0207f), new Vector3(-0.01f,0f,.1f)}},
			{ButtonHintType.Grip, new[] {new Vector3(-0.0008f, -0.0311f, -0.0246f), new Vector3(.07f,-.020f,-.07f)}},
			{ButtonHintType.Button1, new[] {new Vector3(0.0019f, 0f, -0.0074f), new Vector3(0.1f,.08f,-0.07f)}},
			{ButtonHintType.Button2, new[] {new Vector3(0.009f, 0f, 0.0055f), new Vector3(0.1f,.1f,0f)}},
			{ButtonHintType.Thumbstick, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.ThumbstickClick, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.ThumbstickX, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.ThumbstickY, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.OtherObject, new[] {new Vector3(0, 0f, 0f), new Vector3(0f,.2f,0f)}}
		};

		private readonly Dictionary<ButtonHintType, Vector3[]> riftSQuestOffsets = new Dictionary<ButtonHintType, Vector3[]>
		{
			// format for vector3 is origin, direction
			{ButtonHintType.Trigger, new[] {new Vector3(-0.0003f, -0.0209f, 0.0207f), new Vector3(-0.01f, 0f,.1f)}},
			{ButtonHintType.Grip, new[] {new Vector3(-0.0008f, -0.0311f, -0.0246f), new Vector3(.07f,-.020f,-.07f)}},
			{ButtonHintType.Button1, new[] {new Vector3(0.0019f, 0f, -0.0074f), new Vector3(0.1f,.08f,-0.07f)}},
			{ButtonHintType.Button2, new[] {new Vector3(0.009f, 0f, 0.0055f), new Vector3(0.1f,.1f,0f)}},
			{ButtonHintType.Thumbstick, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.ThumbstickClick, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.ThumbstickX, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.ThumbstickY, new[] {new Vector3(-0.0105f, 0.0034f, 0.005f), new Vector3(-.05f,.1f,0f)}},
			{ButtonHintType.OtherObject, new[] {new Vector3(0, 0f, 0f), new Vector3(0f,.2f,0f)}}
		};

		private static Dictionary<ButtonHintType, Vector3[]> offsets;

		public class ControllerLabel {
			public readonly Side side;
			public readonly ButtonHintType type;
			private readonly GameObject labelObj;
			private readonly RectTransform canvasObj;
			private readonly Text text;
			private readonly LineRenderer line;
			private const float lineWidth = .002f;
			private readonly Color lineColor = Color.black;
			public Color flashColor = Color.red;
			private readonly Transform controller;
			private readonly Camera cam;
			/// <summary>
			/// Change this value to fit worlds with a scaled camera rig
			/// </summary>
			private const float scale = 1f;

			public Vector3[] offset;

			public GameObject meshHighlight;


			/// <summary>
			/// Creates a new controller label
			/// </summary>
			/// <param name="parent">The camera rig, so that the label doesn't jitter so much</param>
			/// <param name="controller">Center of the particular controller in use</param>
			/// <param name="labelPrefab">The prefab of the label to create.
			/// Must have a Text object as a child somewhere.</param>
			/// <param name="side">Which controller</param>
			/// <param name="type">Type of input</param>
			/// <param name="offset">Position and direction of label offset</param>
			public ControllerLabel(Side side, ButtonHintType type, Transform parent, Transform controller, GameObject labelPrefab, Vector3[] offset, Mesh mesh = null, Material meshMaterial = null) {
				labelObj = Instantiate(labelPrefab, parent);
				canvasObj = labelObj.GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
				text = labelObj.GetComponentInChildren<Text>();

				this.side = side;
				this.offset = offset;
				this.type = type;
				this.controller = controller;
				line = labelObj.AddComponent<LineRenderer>();
				line.widthMultiplier = lineWidth * scale;
				line.material.shader = Shader.Find("Unlit/Color");
				line.material.color = lineColor;
				line.positionCount = 2;
				cam = Camera.main;

				meshHighlight = new GameObject("Highlight");
				meshHighlight.transform.SetParent(controller);
				meshHighlight.transform.localPosition = Vector3.zero;
				meshHighlight.transform.localRotation = Quaternion.identity;
				meshHighlight.AddComponent<MeshFilter>().mesh = mesh;
				meshHighlight.AddComponent<MeshRenderer>().material = meshMaterial;
			}

			public void Remove() {
				Destroy(labelObj);
				Destroy(meshHighlight);
			}

			public void UpdatePos() {
				Vector3 pos = controller.TransformPoint(offset[0] * scale);
				Vector3 dir = controller.TransformPoint(offset[1] * scale);
				labelObj.transform.position = dir;
				canvasObj.pivot = new Vector2(0, .5f);
				if (offset[1].x <= 0) {
					canvasObj.pivot = new Vector2(1, .5f);
				}
				canvasObj.anchoredPosition = Vector2.zero;
				line.SetPositions(new[] { pos, labelObj.transform.position });

				RotateToFaceCamera();
			}

			private void RotateToFaceCamera() {
				labelObj.transform.LookAt(cam.transform);
				labelObj.transform.Rotate(0, 180, 0, Space.Self);
			}

			public void SetText(string labelText) {
				text.text = labelText;
			}
		}

		public enum ButtonHintType {
			Trigger,
			Grip,
			Thumbstick,
			ThumbstickX,
			ThumbstickY,
			ThumbstickClick,
			Button1,
			Button2,
			OtherObject
		}

		private void Awake() {
			instance = this;
		}

		private void Start() {
			if (!initialized) {
				//FindControllerParts();

				if (InputMan.controllerStyle == HeadsetControllerStyle.Rift) {
					offsets = riftOffsets;
				}
				else if (InputMan.controllerStyle == HeadsetControllerStyle.RiftSQuest) {
					offsets = riftSQuestOffsets;
				}
				else {
					offsets = riftOffsets;
				}

				buttonMeshesDict = new Dictionary<Side, Dictionary<ButtonHintType, Mesh>> {
					{ Side.Left, new Dictionary<ButtonHintType, Mesh> {
						{ ButtonHintType.Trigger, leftButtonMeshes[0] },
						{ ButtonHintType.Grip, leftButtonMeshes[1] },
						{ ButtonHintType.Thumbstick, leftButtonMeshes[2] },
						{ ButtonHintType.Button1, leftButtonMeshes[3] },
						{ ButtonHintType.Button2, leftButtonMeshes[4] }
					}
					},{ Side.Right, new Dictionary<ButtonHintType, Mesh> {
						{ ButtonHintType.Trigger, rightButtonMeshes[0] },
						{ ButtonHintType.Grip, rightButtonMeshes[1] },
						{ ButtonHintType.Thumbstick, rightButtonMeshes[2] },
						{ ButtonHintType.Button1, rightButtonMeshes[3] },
						{ ButtonHintType.Button2, rightButtonMeshes[4] }
					}}
				};
			}

			initialized = true;
		}

		private void LateUpdate() {
			foreach (ControllerLabel controllerLabel in instantiatedLabels) {
				controllerLabel.UpdatePos();
			}
		}

		/// <summary>
		/// Shows a text hint for a specific input
		/// </summary>
		/// <param name="side">Which controller</param>
		/// <param name="hintType">The type of input to show the hint for</param>
		/// <param name="text">The text to show on the hint</param>
		public static ControllerLabel[] ShowHint(Side side, ButtonHintType hintType, string text) {
			if (!instance.initialized) {
				instance.Start();
			}
			// these two could be combined if destroy method also deleted gameobject
			instance.instantiatedLabels.FindAll(e => e.type == hintType).ForEach(e => e.Remove());
			instance.instantiatedLabels.RemoveAll(e => e.type == hintType);

			List<ControllerLabel> labels = new List<ControllerLabel>();

			InputMan.Vibrate(side, 1);

			if (side == Side.Left || side == Side.Both) {
				ControllerLabel label;
				if (buttonMeshesDict[Side.Left].ContainsKey(hintType)) {
					label = new ControllerLabel(side, hintType, instance.rig.transform, instance.rig.leftHand, instance.labelPrefab, offsets[hintType], buttonMeshesDict[Side.Left][hintType], instance.highlightMaterial);
				}
				else {
					label = new ControllerLabel(side, hintType, instance.rig.transform, instance.rig.leftHand, instance.labelPrefab, offsets[hintType]);
				}
				label.SetText(text);
				instance.instantiatedLabels.Add(label);
				labels.Add(label);
			}
			if (side == Side.Right || side == Side.Both) {
				Vector3[] flippedOffsets = new Vector3[] { offsets[hintType][0], offsets[hintType][1] };
				flippedOffsets[0].x *= -1;
				flippedOffsets[1].x *= -1;
				ControllerLabel label;
				if (buttonMeshesDict[Side.Right].ContainsKey(hintType)) {
					label = new ControllerLabel(side, hintType, instance.rig.transform, instance.rig.rightHand, instance.labelPrefab, flippedOffsets, buttonMeshesDict[Side.Right][hintType], instance.highlightMaterial);
				}
				else {
					label = new ControllerLabel(side, hintType, instance.rig.transform, instance.rig.rightHand, instance.labelPrefab, flippedOffsets);
				}
				label.SetText(text);
				instance.instantiatedLabels.Add(label);
				labels.Add(label);
			}

			return labels.ToArray();
		}

		/// <summary>
		/// Hides the hint text for the desired type
		/// </summary>
		/// <param name="side">Which controller</param>
		/// <param name="hintType">The type of hint to hide</param>
		public static void HideHint(Side side, ButtonHintType hintType) {
			// these two could be combined if destroy method also deleted gameobject
			instance.instantiatedLabels.FindAll(e => (e.type == hintType && e.side == side)).ForEach(e => e.Remove());
			instance.instantiatedLabels.RemoveAll(e => (e.type == hintType && e.side == side));
		}

		public static void HideAllHints(Side side = Side.Both) {
			if (!instance) return;

			List<ControllerLabel> deletedLabels = new List<ControllerLabel>();
			foreach (ControllerLabel label in instance.instantiatedLabels) {
				if (label.side == side || side == Side.Both) {
					label.Remove();
					deletedLabels.Add(label);
				}
			}

			instance.instantiatedLabels = instance.instantiatedLabels.Except(deletedLabels).ToList();
		}
	}
}
