using UnityEngine;
using UnityEngine.UI;

public class TestSizeButton : MonoBehaviour
{
	public Graphic Graphic;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void OnGui()
	{
		GUI.Label(new Rect(0f, 0f, 1000f, 30f), "Width: " + Graphic.rectTransform.rect.width);
	}
}
