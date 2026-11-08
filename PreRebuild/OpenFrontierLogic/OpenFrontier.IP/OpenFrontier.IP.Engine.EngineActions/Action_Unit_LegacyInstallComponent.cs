using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_LegacyInstallComponent : EngineAction
	{
		public bool AddAmmoIfPossible = true;

		public bool AutoChargeComponent = true;

		public string BayName;

		public ComponentClass ComponentClass;

		public Unit TargetUnit;

		public override ActionType Type => ActionType.Unit_LegacyInstallComponent;

		public override void Execute()
		{
			base.Execute();
			if (TargetUnit != null && TargetUnit.Components != null && ComponentClass != null)
			{
				ComponentBay bayByName = TargetUnit.Components.GetBayByName(BayName);
				if (bayByName != null)
				{
					ComponentBase componentBase = bayByName.InstallComponent(ComponentClass);
					componentBase.UserPowered = true;
					if (AutoChargeComponent)
					{
						componentBase.RechargeFull();
					}
					ProjectileTurretClass projectileTurretClass = ComponentClass as ProjectileTurretClass;
					if (projectileTurretClass != null && AddAmmoIfPossible)
					{
						TargetUnit.Components.AddDefaultProjectileAmmo(1f, projectileTurretClass);
					}
					if (TargetUnit.IsPlayerCurrentUnit && engine.Hud != null)
					{
						EngineASX.Instance.Hud.RefreshTurretGrids();
						engine.Hud.UpdateTractorTurretRef();
					}
				}
				else
				{
					Debug.LogError($"Cannot install component. Bay \"{BayName}\" is unknown");
				}
			}
			else
			{
				Debug.LogError("Cannot install component. Unit does not support component or component no specified");
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(TargetUnit);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			TargetUnit = reader.ReadUnitFromId(engine);
		}
	}
}
