using OpenFrontier.IP.Engine.WorldGeneration.Models;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseBlueprint
{
	public class UniverseBlueprintMapItem : MonoBehaviour
	{
		public UniverseBlueprintSectorIcons SectorDisplayIcons;

		public Text NameLabel;

		public SectorBlueprint Sector;

		public Graphic Sprite;

		private UniverseBlueprintDrawer universeDrawer;

		public Toggle Toggle;

		public UniverseBlueprintDrawer UniverseDrawer
		{
			get
			{
				return universeDrawer;
			}
			set
			{
				universeDrawer = value;
			}
		}

		public Vector3 CalcLocalPosition()
		{
			return UniverseDrawer.GetSectorLocalPosition(Sector);
		}

		public void Reposition()
		{
			if (Sector != null)
			{
				transform.localPosition = CalcLocalPosition();
				NameLabel.transform.localPosition = UniverseDrawer.DefaultLabelOffset;
			}
		}

		public void Refresh()
		{
			Reposition();
			if (Sector != null)
			{
				NameLabel.text = Sector.Name;
			}
			else
			{
				NameLabel.text = null;
			}
			SectorDisplayIcons.SectorBlueprint = Sector;
			SectorDisplayIcons.Refresh();
			float num = Mathf.Clamp(Sector.GateDistanceMultiplier, 0.25f, 2f);
			Sprite.transform.localScale = new Vector3(num, num, num);
		}
	}
}
