using System;
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
				MathUtilsExt.GetTransformOffset(
					rayOrigin, m_SceneObjectProxy.transform, out m_PositionOffset, out m_RotationOffset);

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
