using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	public class SceneObjectProxy : MonoBehaviour, IDroppable
	{
		public GameObject sceneObject { private get; set; }

		public object GetDropObject()
		{
			return sceneObject;
		}
	}
}
