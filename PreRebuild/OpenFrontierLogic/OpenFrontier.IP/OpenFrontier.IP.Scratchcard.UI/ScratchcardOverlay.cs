using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.Scratchcard.UI
{
	public class ScratchcardOverlay : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		public RawImage RawImage;

		public Button Button;

		private bool isPointerDown;

		private Texture2D texture;

		private Color[] textureColors;

		private int erasedPixelsCount;

		public int ScratchRadius = 45;

		private bool hasInit;

		private Texture2D originalTexture;

		public float Progress => (float)erasedPixelsCount / (float)(texture.width * texture.height);

		private void Awake()
		{
			if (!hasInit)
			{
				GetComponent<RectTransform>();
				originalTexture = (Texture2D)RawImage.texture;
				texture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, mipChain: false);
				ResetColors();
				RawImage.texture = texture;
				hasInit = true;
			}
		}

		private void ResetColors()
		{
			textureColors = originalTexture.GetPixels();
		}

		public void Reset()
		{
			if (!hasInit)
			{
				Awake();
			}
			ResetColors();
			erasedPixelsCount = 0;
			ApplyTexture();
		}

		private void ApplyTexture()
		{
			texture.SetPixels(textureColors);
			texture.Apply();
		}

		private void Update()
		{
			if (!isPointerDown)
			{
				return;
			}
			RectTransform component = GetComponent<RectTransform>();
			Vector3 vector = Input.mousePosition - transform.position;
			vector = new Vector3(vector.x / transform.lossyScale.x, vector.y / transform.lossyScale.y, 0f);
			int num = (int)(vector.x / component.rect.width * (float)texture.width);
			int num2 = (int)((0f - vector.y) / component.rect.height * (float)texture.height);
			num2 = texture.height - num2;
			int scratchRadius = ScratchRadius;
			for (int i = num - scratchRadius; i < num + scratchRadius; i++)
			{
				for (int j = num2 - scratchRadius; j < num2 + scratchRadius; j++)
				{
					if (i < 0 || j < 0 || i >= texture.width || j >= texture.height)
					{
						continue;
					}
					int num3 = j * texture.width + i;
					if (num3 > 0 && num3 < textureColors.Length)
					{
						Color color = textureColors[num3];
						if (color.a > 0f && Vector2.Distance(new Vector2(num, num2), new Vector2(i, j)) < (float)scratchRadius)
						{
							textureColors[num3] = new Color(color.r, color.g, color.b, 0f);
							erasedPixelsCount++;
						}
					}
				}
			}
			ApplyTexture();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			isPointerDown = true;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			isPointerDown = false;
		}
	}
}
