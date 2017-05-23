using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides delegates used to drag objects
	/// </summary>
	public interface IDrag
	{
		/// <summary>
		/// Must be called by the implementer when a drag has started
		/// Params: the dragged object, the ray origin
		/// </summary>
		event Action<GameObject, Transform> dragStarted;

		/// <summary>
		/// Must be called by the implementer when a drag has ended
		/// Params: the ray origin
		/// </summary>
		event Action<Transform> dragEnded;
	}
}
