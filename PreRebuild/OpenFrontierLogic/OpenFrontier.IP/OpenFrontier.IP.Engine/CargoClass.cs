using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CargoClass : MonoBehaviour
	{
		public float Rarity = 0.5f;

		public float AITradeSearchProfitabilityFudge = 1f;

		public float WorldSeedQuantityFudge = 1f;

		public bool IsOre;

		public bool IsReserved;

		public int BasePrice;

		public string ClassName;

		public string ShortName;

		public string Description;

		public bool IsEquipment;

		public bool IsDeployable;

		public bool IsTraded = true;

		public bool Legal = true;

		public Cargo NonContainerPrefab;

		public GameObject RelatedPrefab;

		public int UniqueId;

		public float Volume = 1f;

		public string SpriteName;

		public List<CargoClass> ConstituentCargoClasses;

		public string ShortNameIfAssigned
		{
			get
			{
				if (!string.IsNullOrEmpty(ShortName))
				{
					return ShortName;
				}
				return ClassName;
			}
		}

		public string GetShortNameElseLong()
		{
			if (!string.IsNullOrEmpty(ShortName))
			{
				return ShortName;
			}
			return ClassName;
		}

		public Sprite GetCargoSpriteOrDefault()
		{
			return EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(this);
		}
	}
}
