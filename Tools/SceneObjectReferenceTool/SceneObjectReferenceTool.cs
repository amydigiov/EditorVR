using System;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class SceneObjectReferenceTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
		ISetManipulatorsVisible
	{
		[SerializeField]
		ActionMap m_ActionMap;

		SceneObjectProxy m_SceneObjectProxy;
		Vector3 m_PositionOffset;
		Quaternion m_RotationOffset;

		public Transform rayOrigin { private get; set; }
		public ActionMap actionMap { get { return m_ActionMap; } }
		public event Action<GameObject, Transform> dragStarted;
		public event Action<Transform> dragEnded;
		public Func<Transform, bool> isRayActive;

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			if (!IsActive())
				return;

			var hoveredObject = this.GetFirstGameObject(rayOrigin);

			var sceneObjectReferenceInput = (SceneObjectReferenceInput)input;
			this.SetManipulatorsVisible(this, !sceneObjectReferenceInput.drag.isHeld);
			
			if (sceneObjectReferenceInput.drag.wasJustPressed && hoveredObject != null)
			{
				consumeControl(sceneObjectReferenceInput.drag);

				m_SceneObjectProxy = ObjectUtils.Instantiate(hoveredObject).AddComponent<SceneObjectProxy>();
				m_SceneObjectProxy.sceneObject = hoveredObject;
				var sceneObjectProxyTransform = m_SceneObjectProxy.transform;
				MathUtilsExt.GetTransformOffset(
					rayOrigin, sceneObjectProxyTransform, out m_PositionOffset, out m_RotationOffset);

				var maxComponent = ObjectUtils.GetBounds(sceneObjectProxyTransform).size.MaxComponent();
				var scaleFactor = 1 / maxComponent;
				sceneObjectProxyTransform.localScale = sceneObjectProxyTransform.localScale * scaleFactor;

				// Turn off expensive render settings
				foreach (var renderer in sceneObjectProxyTransform.GetComponentsInChildren<Renderer>())
				{
					renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					renderer.receiveShadows = false;
					renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
					renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
				}

				// Turn off lights
				foreach (var light in sceneObjectProxyTransform.GetComponentsInChildren<Light>())
				{
					light.enabled = false;
				}

				// Disable any SmoothMotion that may be applied to a cloned scene object
				var smoothMotion = m_SceneObjectProxy.GetComponent<SmoothMotion>();
				if (smoothMotion != null)
					smoothMotion.enabled = false;

				if (dragStarted != null)
					dragStarted(m_SceneObjectProxy.gameObject, rayOrigin);
			}

			if (sceneObjectReferenceInput.drag.wasJustReleased)
			{
				if (dragEnded != null)
					dragEnded(rayOrigin);

				if (m_SceneObjectProxy != null)
					ObjectUtils.Destroy(m_SceneObjectProxy.gameObject);
				m_SceneObjectProxy = null;

			}

			if (m_SceneObjectProxy != null)
			{
				MathUtilsExt.SetTransformOffset(rayOrigin, m_SceneObjectProxy.transform, m_PositionOffset, m_RotationOffset);
			}
		}

		bool IsActive()
		{
			if (rayOrigin == null)
				return false;

			if (!isRayActive(rayOrigin))
				return false;

			return true;
		}
	}
}
