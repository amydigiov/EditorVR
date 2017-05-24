using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class GameObjectReferenceTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
		ISetManipulatorsVisible, IIsHoveringOverUI
	{
		[SerializeField]
		ActionMap m_ActionMap;

		GameObject m_ReferencedObject;
		GameObject m_ReferenceProxyObject;

		public Transform rayOrigin { private get; set; }
		public ActionMap actionMap { get { return m_ActionMap; } }
		public Func<Transform, bool> isRayActive;

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			if (!IsActive())
				return;

			var gameObjectReferenceInput = (GameObjectReferenceInput)input;

			var hoveredObject = this.GetFirstGameObject(rayOrigin);
			//if (!GetSelectionCandidate(ref hoveredObject))
			//	return;

			this.SetManipulatorsVisible(this, !gameObjectReferenceInput.drag.isHeld);
			
			if (gameObjectReferenceInput.drag.wasJustPressed)
			{
				m_ReferencedObject = hoveredObject;
				if (m_ReferencedObject != null)
				{
					consumeControl(gameObjectReferenceInput.drag);
					m_ReferenceProxyObject = ObjectUtils.Instantiate(m_ReferencedObject);
					// TODO: This should implement IDroppable so it can point to the original reference
				}
			}

			if (gameObjectReferenceInput.drag.wasJustReleased)
			{
				m_ReferencedObject = null;
				if (m_ReferenceProxyObject != null)
					ObjectUtils.Destroy(m_ReferenceProxyObject);
			}
		}

		//bool GetSelectionCandidate(ref GameObject hoveredObject)
		//{
		//	var selectionCandidate = this.GetSelectionCandidate(hoveredObject, true);

		//	// Can't select this object (it might be locked or static)
		//	if (hoveredObject && !selectionCandidate)
		//		return false;

		//	if (selectionCandidate)
		//		hoveredObject = selectionCandidate;

		//	return true;
		//}

		bool IsActive()
		{
			if (rayOrigin == null)
				return false;

			if (this.IsHoveringOverUI(rayOrigin))
				return false;

			if (!isRayActive(rayOrigin))
				return false;

			return true;
		}
	}
}
