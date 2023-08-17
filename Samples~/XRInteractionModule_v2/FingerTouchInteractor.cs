using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VelUtils {
/*
    /// <summary>
    /// Interactor used for interacting with interactables by touching UI. 
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VelUtils/Interaction/Finger Touch Interactor")]
    public class FingerTouchInteractor : XRBaseControllerInteractor, IUIInteractable {


        // Input Module for fast access to UI systems.
        XRUIInputModule m_InputModule;

        [SerializeField]
        bool m_EnableUIInteraction = true;
        /// <summary>Gets or sets whether this interactor is able to affect UI.</summary>
        public bool enableUIInteraction {
            get {
                return m_EnableUIInteraction;
            }
            set {
                if (m_EnableUIInteraction != value) {
                    m_EnableUIInteraction = value;
                    if (enabled) {
                        if (m_EnableUIInteraction) {
                            m_InputModule.RegisterInteractable(this);
                        }
                        else {
                            m_InputModule.UnregisterInteractable(this);
                        }
                    }
                }
            }
        }


        // Doesn't create an event system object if it doesn't exist.
        void FindOrCreateXRUIInputModule() {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem != null) {
                m_InputModule = eventSystem.GetComponent<XRUIInputModule>();
                if (m_InputModule == null)
                    m_InputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();

            if (m_EnableUIInteraction) {
                FindOrCreateXRUIInputModule();
                m_InputModule.RegisterInteractable(this);
            }
        }

        protected override void OnDisable() {
            base.OnDisable();

            if (m_EnableUIInteraction) {
                m_InputModule.UnregisterInteractable(this);
            }
            m_InputModule = null;
        }

        /// <summary>
        /// Updates the current UI Model to match the state of the Interactor
        /// </summary>
        /// <param name="model">The model that will match this Interactor</param>
        public void UpdateUIModel(ref TrackedDeviceModel model) {
            model.position = m_StartTransform.position;
            model.orientation = m_StartTransform.rotation;
            model.select = isUISelectActive;

            int numPoints = 0;
            GetLinePoints(ref s_CachedLinePoints, ref numPoints);

            List<Vector3> raycastPoints = model.raycastPoints;
            raycastPoints.Clear();
            if (numPoints > 0 && s_CachedLinePoints != null) {
                raycastPoints.Capacity = raycastPoints.Count + numPoints;
                for (int i = 0; i < numPoints; i++)
                    raycastPoints.Add(s_CachedLinePoints[i]);
            }
            model.raycastLayerMask = raycastMask;
        }

        /// <summary>
        /// Attempts to retrieve the current UI Model.  Returns false if not available.
        /// </summary>
        /// <param name="model"> The UI Model that matches that Interactor.</param>
        /// <returns></returns>
        public bool TryGetUIModel(out TrackedDeviceModel model) {
            if (m_InputModule != null) {
                if (m_InputModule.GetTrackedDeviceModel(this, out model))
                    return true;
            }

            model = new TrackedDeviceModel(-1);
            return false;
        }
    }*/
}
