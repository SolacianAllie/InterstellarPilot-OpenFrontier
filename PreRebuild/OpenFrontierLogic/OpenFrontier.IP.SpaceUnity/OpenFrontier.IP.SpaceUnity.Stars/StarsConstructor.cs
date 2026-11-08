using System;
using System.Collections.Generic;
using Imphenzia.SpaceForUnity;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.SpaceUnity.Stars
{
	public static class StarsConstructor
	{
		public static void InstantiateStars(List<string> starsTexturePaths, StaticStars staticStars, System.Random random)
		{
			string text = starsTexturePaths[random.Next(0, starsTexturePaths.Count)].ToString();
			staticStars.starsTexture = Resources.Load<Texture2D>(text);
			if (staticStars.starsTexture == null)
			{
				Debug.LogWarning("Missing stars texture.. no resource found at path \"" + text + "\"");
			}
			staticStars.UpdateMaterial();
		}
	}
}
