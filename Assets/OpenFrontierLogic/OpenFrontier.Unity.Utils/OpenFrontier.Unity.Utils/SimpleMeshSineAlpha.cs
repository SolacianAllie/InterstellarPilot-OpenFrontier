using UnityEngine;

namespace OpenFrontier.Unity.Utils
{
	public class SimpleMeshSineAlpha : MonoBehaviour
	{
		private void Update()
		{
			MeshRenderer component = GetComponent<MeshRenderer>();
			Color color = component.material.color;
			color.a = Maths.Sine01(Time.time);
			component.material.color = color;
		}
	}
}
