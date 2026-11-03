using System;
using System.Collections.Generic;
using Imphenzia.SpaceForUnity;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SpaceUnity.Nebulas;
using OpenFrontier.IP.SpaceUnity.Stars;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.SpaceUnity
{
	public class SpaceConstructor : MonoBehaviour
	{
		public GameObject NebulaPrefab;

		public StaticStars StaticStars;

		public Transform NebulasTransform;

		private NebulaColour[] nebulaColours = new NebulaColour[8]
		{
			NebulaColour.BLUE,
			NebulaColour.PINK,
			NebulaColour.PURPLE,
			NebulaColour.GREEN,
			NebulaColour.YELLOW,
			NebulaColour.ORANGE,
			NebulaColour.RED,
			NebulaColour.CYAN
		};

		private bool HasMatchingNebulaColour(NebulaColour assetColours, NebulaColour requiredColours)
		{
			NebulaColour[] array = nebulaColours;
			foreach (NebulaColour nebulaColour in array)
			{
				if ((assetColours & nebulaColour) != 0 && (requiredColours & nebulaColour) == 0)
				{
					return false;
				}
			}
			return true;
		}

		public void Generate(int seed, SpaceConstructorSettings settings)
		{
			System.Random random = new System.Random(seed);
			SpaceConstructorParams constructorParams = GenerateParams(settings, random);
			Generate(random, constructorParams);
		}

		public void Generate(System.Random random, SpaceConstructorParams constructorParams)
		{
			List<string> list = new List<string>();
			NebulaAsset[] array = Resources.LoadAll<NebulaAsset>("SpaceUnity/Prefabs/NebulaAssets");
			foreach (NebulaAsset nebulaAsset in array)
			{
				if (HasMatchingNebulaColour(nebulaAsset.Colour, constructorParams.NebulaColors) && (nebulaAsset.Style & constructorParams.NebulaStyles) != 0 && (nebulaAsset.Brightness & constructorParams.NebulaBrightness) != 0)
				{
					list.Add("SpaceUnity/Materials/Nebulas/" + nebulaAsset.name);
				}
			}
			UnityObjectHelper.DestroyChildren(NebulasTransform, destroyImmediate: true);
			NebulaConstructor.InstantiateNebulas(list, NebulasTransform, NebulaPrefab, constructorParams.NebulaCount, constructorParams.NebulaTextureCount, random);
			List<string> list2 = new List<string>();
			StarsAsset[] array2 = Resources.LoadAll<StarsAsset>("SpaceUnity/Prefabs/StarsAssets");
			foreach (StarsAsset starsAsset in array2)
			{
				if ((starsAsset.Count & constructorParams.StarsCount) != 0)
				{
					list2.Add("SpaceUnity/Textures/Stars/" + starsAsset.name);
				}
			}
			StaticStars.starsIntensity = constructorParams.StarsIntensity;
			StarsConstructor.InstantiateStars(list2, StaticStars, random);
		}

		public static SpaceConstructorParams GenerateParams(SpaceConstructorSettings settings, System.Random random)
		{
			return new SpaceConstructorParams
			{
				StarsIntensity = Maths.RandomFloatWithPower(random, settings.MinStarsIntensity, settings.MaxStarsIntensity, settings.StarsIntensityPower),
				NebulaColors = settings.NebulaColourSettings.GetRandomWeighted(random).NebulaColours,
				NebulaCount = Maths.RandomIntWithPower(random, settings.MinNebulaCount, settings.MaxNebulaCount, settings.NebulaCountPower)
			};
		}
	}
}
