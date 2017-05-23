#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class DragAndDropModule : MonoBehaviour
	{
		readonly Dictionary<Transform, GameObject> m_DropGameObjects = new Dictionary<Transform, GameObject>();
		readonly Dictionary<Transform, IDropReceiver> m_DropReceivers = new Dictionary<Transform, IDropReceiver>();

		readonly Dictionary<Transform, GameObject> m_HoverObjects = new Dictionary<Transform, GameObject>();

		object GetCurrentDropObject(Transform rayOrigin)
		{
			GameObject dropGameObject;
			if (!m_DropGameObjects.TryGetValue(rayOrigin, out dropGameObject))
				return null;
			return GetDropObject(dropGameObject);
		}

		object GetDropObject(GameObject dropGameObject)
		{
			var droppable = dropGameObject.GetComponent<IDroppable>();
			return droppable != null ? droppable.GetDropObject() : dropGameObject;
		}

		void SetCurrentDropReceiver(Transform rayOrigin, IDropReceiver dropReceiver)
		{
			if (dropReceiver == null)
				m_DropReceivers.Remove(rayOrigin);
			else
				m_DropReceivers[rayOrigin] = dropReceiver;
		}

		public IDropReceiver GetCurrentDropReceiver(Transform rayOrigin)
		{
			IDropReceiver dropReceiver;
			if (m_DropReceivers.TryGetValue(rayOrigin, out dropReceiver))
				return dropReceiver;

			return null;
		}

		public void OnRayEntered(GameObject gameObject, Transform rayOrigin)
		{
			var dropReceiver = gameObject.GetComponent<IDropReceiver>();
			if (dropReceiver != null)
			{
				if (dropReceiver.CanDrop(GetCurrentDropObject(rayOrigin)))
				{
					dropReceiver.OnDropHoverStarted();
					m_HoverObjects[rayOrigin] = gameObject;
					SetCurrentDropReceiver(rayOrigin, dropReceiver);
				}
			}
		}

		public void OnRayExited(GameObject gameObject, Transform rayOrigin)
		{
			if (!gameObject)
				return;

			var dropReceiver = gameObject.GetComponent<IDropReceiver>();
			if (dropReceiver != null)
			{
				if (m_HoverObjects.Remove(rayOrigin))
				{
					dropReceiver.OnDropHoverEnded();
					SetCurrentDropReceiver(rayOrigin, null);
				}
			}
		}

		public void OnDragStarted(GameObject gameObject, Transform rayOrigin)
		{
			m_DropGameObjects[rayOrigin] = gameObject;
		}

		public void OnDragEnded(Transform rayOrigin)
		{
			if (!m_DropGameObjects.ContainsKey(rayOrigin))
				return;
			var dropGameObject = m_DropGameObjects[rayOrigin];
			if (dropGameObject != null)
			{
				m_DropGameObjects.Remove(rayOrigin);

				var dropReceiver = GetCurrentDropReceiver(rayOrigin);
				var dropObject = GetDropObject(dropGameObject);
				if (dropReceiver != null && dropReceiver.CanDrop(dropObject))
					dropReceiver.ReceiveDrop(dropObject);
			}
		}
	}
}
#endif
