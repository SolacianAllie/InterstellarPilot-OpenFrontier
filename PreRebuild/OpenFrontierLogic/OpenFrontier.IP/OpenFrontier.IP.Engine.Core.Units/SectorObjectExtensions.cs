using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public static class SectorObjectExtensions
	{
		public static string GetSectorAndLocationText(this SectorObject unit)
		{
			Vector3 sectorPosition = unit.SectorPosition;
			string text = ((unit.Sector != null) ? unit.Sector.Name : "Unknown");
			string text2 = TextFormattingHelper.FormatSectorPosition(sectorPosition);
			return text + ", {" + text2 + "}";
		}

		public static void GetSectorAndLocationTextNonAlloc(this SectorObject unit, StringBuilder stringBuilder)
		{
			Vector3 sectorPosition = unit.SectorPosition;
			string value = ((unit.Sector != null) ? unit.Sector.Name : "Unknown");
			stringBuilder.Append(value);
			stringBuilder.Append(", {");
			TextFormattingHelper.FormatSectorPositionNonAlloc(sectorPosition, stringBuilder);
			stringBuilder.Append("}");
		}
	}
}
