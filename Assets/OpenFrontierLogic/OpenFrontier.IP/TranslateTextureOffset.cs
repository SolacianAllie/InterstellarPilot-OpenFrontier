using UnityEngine;

public class TranslateTextureOffset : MonoBehaviour
{
	private Material material;

	public Vector2 ScrollRate = Vector2.zero;

	private Vector2 textureOffset = Vector2.zero;

	public bool UseRealTime;

	// URP port: BiRP materials use _MainTex, our URP-style custom shaders
	// use _BaseMap - drive whichever the material actually has.
	private string textureProperty = "_MainTex";

	private void Start()
	{
		material = GetComponent<Renderer>().material;
		if (material.HasProperty("_BaseMap"))
		{
			textureProperty = "_BaseMap";
		}
	}

	private void Update()
	{
		float num = (UseRealTime ? RealTime.deltaTime : Time.deltaTime);
		textureOffset += ScrollRate * num;
		material.SetTextureOffset(textureProperty, textureOffset);
	}
}
