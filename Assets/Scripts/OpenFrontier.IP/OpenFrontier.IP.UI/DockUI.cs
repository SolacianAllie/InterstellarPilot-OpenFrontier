using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class DockUI
	{
		public bool IsPlayerUnitOwnedByPlayer
		{
			get
			{
				if (PlayerCurrentUnit != null)
				{
					return PlayerCurrentUnit.IsOwnedByPlayer;
				}
				return false;
			}
		}

		public bool IsPlayerUnitDocked
		{
			get
			{
				if (PlayerCurrentUnit != null)
				{
					return PlayerCurrentUnit.Components.IsDocked;
				}
				return false;
			}
		}

		public bool IsPlayerUnitDockedAndOwnedByPlayer => IsUnitDockedAndOwnedByPlayer(PlayerCurrentUnit);

		public bool CanDockedShipTrade => CanUnitTrade(PlayerCurrentUnit);

		public bool CanDockedShipTradeCargo
		{
			get
			{
				Unit playerDockUnit = PlayerDockUnit;
				if (IsPlayerUnitDockedAndOwnedByPlayer && playerDockUnit.Components.CargoTrader != null)
				{
					if (playerDockUnit.IsOwnedByPlayer)
					{
						return playerDockUnit.UnitClass.CanTradeAtOwnStation;
					}
					return true;
				}
				return false;
			}
		}

		public EngineASX Engine => engine;

		public Unit PlayerDockUnit
		{
			get
			{
				Unit playerCurrentUnit = PlayerCurrentUnit;
				if (playerCurrentUnit != null)
				{
					Unit dockUnit = playerCurrentUnit.GetDockUnit();
					if (dockUnit != null)
					{
						return dockUnit;
					}
					return playerCurrentUnit;
				}
				return null;
			}
		}

		public Unit PlayerRootUnit
		{
			get
			{
				Unit playerCurrentUnit = PlayerCurrentUnit;
				if (playerCurrentUnit != null)
				{
					return playerCurrentUnit.GetRootUnit();
				}
				return null;
			}
		}

		public Unit PlayerCurrentUnit
		{
			get
			{
				if (engine.LocalPlayer != null)
				{
					return engine.LocalPlayer.Person.CurrentUnit;
				}
				return null;
			}
			set
			{
				engine.LocalPlayer.Person.CurrentUnit = value;
			}
		}

		private EngineASX engine => EngineASX.Instance;

		public Sector PlayerSector => PlayerRootUnit.Sector;

		public bool IsUnitDockedAndOwnedByPlayer(Unit unit)
		{
			if (unit != null && unit.IsDocked)
			{
				return unit.IsOwnedByPlayer;
			}
			return false;
		}

		public bool CanUnitTrade(Unit unit)
		{
			return IsUnitDockedAndOwnedByPlayer(unit);
		}

		public void MovePlayerToRootUnit()
		{
			PlayerCurrentUnit = PlayerRootUnit;
		}

		public void MovePlayerToDockUnit()
		{
			PlayerCurrentUnit = PlayerDockUnit;
		}

		public static bool AreRepairsNeeded(UnitComponentHolder componentHolder)
		{
			if (componentHolder.ShieldComponent != null && !componentHolder.ShieldComponent.IsFullyCharged)
			{
				return true;
			}
			if (componentHolder.Unit.Destructable.CurrentHealth < componentHolder.UnitClass.maxHealth)
			{
				return true;
			}
			for (int i = 0; i < componentHolder.UnitComponents.Count; i++)
			{
				if (componentHolder.UnitComponents[i].HealthNormalized < 1f)
				{
					return true;
				}
			}
			return false;
		}

		public bool CanPilotCurrentShip()
		{
			if (PlayerCurrentUnit != null && PlayerCurrentUnit.IsOwnedByPlayer && !PlayerCurrentUnit.IsDocked)
			{
				return PlayerCurrentUnit.UnitClass.IsPilottable;
			}
			return false;
		}

		public bool CanUndockCurrentShip()
		{
			return CanUndockShip(PlayerCurrentUnit.Components);
		}

		public bool CanUndockShip(UnitComponentHolder unitComponents)
		{
			if (unitComponents.IsDocked && unitComponents.CanUndock() && unitComponents.Unit.IsOwnedByPlayer && unitComponents.UnitClass.IsPilottable)
			{
				return !OrdersHelper.IsPilottedByNpc(unitComponents.Unit);
			}
			return false;
		}

		public void PilotCurrentShip()
		{
			if (CanPilotCurrentShip())
			{
				engine.IsPaused = false;
				engine.PlayerUnit.Components.PilotPerson = engine.LocalPlayer.Person;
				engine.ShowHud();
			}
			else
			{
				Debug.LogWarning("Pilot button clicked but can't pilot ship");
			}
		}

		public void UndockCurrentShip()
		{
			if (CanUndockCurrentShip())
			{
				PlayerCurrentUnit.Components.PilotPerson = engine.LocalPlayer.Person;
				PlayerCurrentUnit.Components.UndockIfDocked();
				EngineASX.Instance.HudCamera.ResetOrientation();
			}
			else
			{
				Debug.LogWarning("Dock button clicked but can't undock");
			}
		}

		public void UndockShip(UnitComponentHolder ship)
		{
			if (!CanUndockShip(ship))
			{
				Debug.LogWarning("Dock button clicked but can't undock");
				return;
			}

			// Asked before anything is done locally, for the same reason
			// docking is. Only the host knows what is in a hangar, so only the
			// host can take a ship out of one - and a client that undocked
			// itself would be somewhere the host did not put it, which is a
			// copy that can never be corrected because nothing says so.
			//
			// Returns false on a host and in singleplayer, where the ship
			// undocks here as it always did.
			if (Authority.UndockRequest != null
				&& Authority.UndockRequest(ship.Unit.UniqueId))
			{
				return;
			}

			ship.PilotPerson = engine.LocalPlayer.Person;
			ship.UndockIfDocked();
			EngineASX.Instance.HudCamera.ResetOrientation();
		}

		public void NavigateToInitialScreen()
		{
			UIController.Instance.ScreenNavigator.ShowUnitMainScreen();
		}

		public void PlayBuySellBeep()
		{
			if (engine.BuySellAudioClip != null)
			{
				AudioSource.PlayClipAtPoint(engine.BuySellAudioClip, Vector3.zero);
			}
		}
	}
}
