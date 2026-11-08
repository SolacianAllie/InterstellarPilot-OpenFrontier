using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	/// <summary>
	/// Open Frontier: picks a random unit thumbnail for the loading screen
	/// each time the object is enabled, so every load shows something
	/// different. Uses every sprite in Resources/textures/ui/unitthumbnails
	/// (ships, stations, asteroids, icons - only the "missing" placeholder
	/// is excluded), loaded once and cached. Never picks the same sprite
	/// twice in a row.
	/// </summary>
	[RequireComponent(typeof(Image))]
	public class RandomShipImage : MonoBehaviour
	{
		private const string ThumbnailsPath = "textures/ui/unitthumbnails";

		private static Sprite[] thumbnails;

		private static int lastSpriteIndex = -1;

		private Image image;

		private void Awake()
		{
			image = GetComponent<Image>();
		}

		private void OnEnable()
		{
			EnsureSpritesLoaded();
			if (thumbnails.Length == 0)
			{
				return;
			}
			int num = PickRandomIndex();
			image.sprite = thumbnails[num];
		}

		private static int PickRandomIndex()
		{
			if (thumbnails.Length == 1)
			{
				return 0;
			}
			int num;
			do
			{
				num = UnityEngine.Random.Range(0, thumbnails.Length);
			}
			while (num == lastSpriteIndex);
			lastSpriteIndex = num;
			return num;
		}

		private static void EnsureSpritesLoaded()
		{
			if (thumbnails != null)
			{
				return;
			}
			List<Sprite> list = new List<Sprite>(Resources.LoadAll<Sprite>(ThumbnailsPath));
			list.RemoveAll((Sprite sprite) => sprite.name == "missing");
			if (list.Count == 0)
			{
				Debug.LogWarning("[RandomShipImage] No unit thumbnails found at Resources/" + ThumbnailsPath);
			}
			thumbnails = list.ToArray();
		}
	}
}
