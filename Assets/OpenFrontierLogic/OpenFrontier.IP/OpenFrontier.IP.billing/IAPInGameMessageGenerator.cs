using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.billing
{
	public class IAPInGameMessageGenerator : MonoBehaviour
	{
		public WorldBase World;

		private void Awake()
		{
			if (World != null)
			{
				World.NewGame += world_NewGame;
			}
			else
			{
				Debug.LogError("Expected world ");
			}
		}

		private void world_NewGame(WorldBase sender)
		{
			if (sender.Engine.LocalPlayer != null)
			{
				IPProduct[] products = GameController.Instance.PurchaseBridge.Products;
				foreach (IPProduct iPProduct in products)
				{
					if (iPProduct != null && iPProduct.IsPurchased)
					{
						PlayerActiveMessage playerActiveMessage = GenerateMessageForProduct(iPProduct);
						if (playerActiveMessage != null)
						{
							sender.Engine.LocalPlayer.AddMessage(playerActiveMessage, notifications: true, important: true);
							break;
						}
					}
				}
			}
			sender.NewGame -= world_NewGame;
		}

		private PlayerActiveMessage GenerateMessageForProduct(IPProduct product)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.MessageText = "Thank you purchasing access to restricted ship classes. These are now accessible from your nearest shipyard";
			playerActiveMessage.SubjectText = "Restricted Ship Access";
			playerActiveMessage.FromText = "";
			AttemptToSetSubjectOnMessage(playerActiveMessage, product);
			return playerActiveMessage;
		}

		private void AttemptToSetSubjectOnMessage(PlayerActiveMessage message, IPProduct product)
		{
			Unit localUnit = World.Engine.LocalUnit;
			if (localUnit != null && localUnit.IsValidAndNotDestroyed)
			{
				Unit subjectUnitAndPosition = AttemptToFindSellerOfProduct(product, localUnit.Sector, localUnit.SectorPosition);
				message.SetSubjectUnitAndPosition(subjectUnitAndPosition);
			}
		}

		private Unit AttemptToFindSellerOfProduct(IPProduct product, Sector originSector, Vector3 originSectorPosition)
		{
			UnitClass unitClass = World.Engine.UnitClasses.FirstOrDefault((UnitClass e) => e.IsUsable && e.RequiredProduct == product);
			return WorldHelper.GetNearestSellerOfUnitClass(World.Engine, originSector, originSectorPosition, unitClass);
		}
	}
}
