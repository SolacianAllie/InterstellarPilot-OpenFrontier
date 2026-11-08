using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_LegacyUninstallComponent : EngineAction
	{
		public string[] BayNames;

		public Unit TargetUnit;

		public override ActionType Type => ActionType.Unit_LegacyUninstallComponent;

		public override void Execute()
		{
			base.Execute();
			if (!(TargetUnit != null) || !(TargetUnit.Components != null))
			{
				return;
			}
			for (int i = 0; i < BayNames.Length; i++)
			{
				ComponentBay bayByName = TargetUnit.Components.GetBayByName(BayNames[i]);
				if (bayByName != null)
				{
					bayByName.DestroyInstalledComponent(immediate: true);
				}
				else
				{
					Debug.LogError($"Cannot install component. Bay \"{BayNames[i]}\" is unknown");
				}
			}
			if (TargetUnit.IsPlayerCurrentUnit && engine.Hud != null)
			{
				EngineASX.Instance.Hud.RefreshTurretGrids();
				engine.Hud.UpdateTractorTurretRef();
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
