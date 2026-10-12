using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Components
{
	public class ShipIconsController : MonoBehaviour
	{
		public struct ShipIconItem
		{
			public UnitClass UnitClass;

			public float HealthNormalized;

			public ShipIconItem(UnitClass unitClass, float healthNormalized)
			{
				this = default;
				UnitClass = unitClass;
				HealthNormalized = healthNormalized;
			}
		}

		private Image[] shipImages;

		public Transform ShipImagesTransform;

		public bool ColorByHealth = true;

		public bool ReverseOrder;

		private void Awake()
		{
			shipImages = ShipImagesTransform.GetComponentsInChildren<Image>();
			Image[] array = shipImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}

		public void RefreshShipImages(IList<ShipIconItem> shipIconItems)
		{
			int num = 0;
			int num2 = Mathf.Min(shipIconItems.Count(), shipImages.Length);
			if (ReverseOrder)
			{
				for (int num3 = num2 - 1; num3 >= 0; num3--)
				{
					ShipIconItem shipIconItem = shipIconItems[num3];
					shipImages[num].enabled = true;
					if (ColorByHealth)
					{
						shipImages[num].color = EngineASX.Instance.GetHullColor(shipIconItem.HealthNormalized);
					}
					shipImages[num].sprite = shipIconItem.UnitClass.GetThumbnailIconSprite();
					num++;
				}
			}
			else
			{
				for (int i = 0; i < num2; i++)
				{
					ShipIconItem shipIconItem2 = shipIconItems[i];
					shipImages[num].enabled = true;
					if (ColorByHealth)
					{
						shipImages[num].color = EngineASX.Instance.GetHullColor(shipIconItem2.HealthNormalized);
					}
					shipImages[num].sprite = shipIconItem2.UnitClass.GetThumbnailIconSprite();
					num++;
				}
			}
			for (int j = num; j < shipImages.Length; j++)
			{
				shipImages[j].enabled = false;
			}
		}
	}
}
