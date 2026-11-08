using UnityEngine;

namespace OpenFrontier.IP
{
	[ExecuteInEditMode]
	public class SimpleTexture : MonoBehaviour
	{
		public Color Color = Color.white;

		public Texture2D Texture;

		private void OnGUI()
		{
			Draw();
		}

		private void Draw()
		{
			if (Texture != null)
			{
				GUI.color = Color;
				GUI.DrawTexture(new Rect(transform.localPosition.x, transform.localPosition.y, transform.localScale.x, transform.localScale.y), Texture);
				GUI.color = Color.white;
			}
		}

		private void OnDrawGizmos()
		{
			Draw();
		}
	}
}
