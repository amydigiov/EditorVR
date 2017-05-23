#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class DragAndDropModule : MonoBehaviour, IInterfaceConnector
	{
		readonly Dictionary<Transform, IDroppable> m_Droppables = new Dictionary<Transform, IDroppable>();
		readonly Dictionary<Transform, IDropReceiver> m_DropReceivers = new Dictionary<Transform, IDropReceiver>();

		readonly Dictionary<Transform, GameObject> m_HoverObjects = new Dictionary<Transform, GameObject>();

		public void ConnectInterface(object obj, Transform rayOrigin = null)
		{
			var drag = obj as IDrag;
			if (drag != null)
			{
				drag.dragStarted += OnDragStarted;
				drag.dragEnded += OnDragEnded;
			}
		}

		public void DisconnectInterface(object obj)
		{
			var drag = obj as IDrag;
			if (drag != null)
			{
				drag.dragStarted -= OnDragStarted;
				drag.dragEnded -= OnDragEnded;
			}
		}

		object GetCurrentDropObject(Transform rayOrigin)
		{
			IDroppable droppable;
			return m_Droppables.TryGetValue(rayOrigin, out droppable) ? droppable.GetDropObject() : null;
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
			var droppable = gameObject.GetComponent<IDroppable>();
			if (droppable != null)
				m_Droppables[rayOrigin] = droppable;
		}

		public void OnDragEnded(Transform rayOrigin)
		{
			if (!m_Droppables.ContainsKey(rayOrigin))
				return;
			var droppable = m_Droppables[rayOrigin];
			if (droppable != null)
			{
				m_Droppables.Remove(rayOrigin);

				var dropReceiver = GetCurrentDropReceiver(rayOrigin);
				var dropObject = droppable.GetDropObject();
				if (dropReceiver != null && dropReceiver.CanDrop(dropObject))
					dropReceiver.ReceiveDrop(droppable.GetDropObject());
			}
		}
	}
}
#endif
