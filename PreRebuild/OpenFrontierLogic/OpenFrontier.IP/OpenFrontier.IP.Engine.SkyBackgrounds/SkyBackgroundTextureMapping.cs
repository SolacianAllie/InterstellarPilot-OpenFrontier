using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SkyBackgrounds
{
	public class SkyBackgroundTextureMapping : MonoBehaviour
	{
		public List<SkyBackgroundTextureMappingItem> Items = new List<SkyBackgroundTextureMappingItem>();

		internal string GetTextureName(string activeSceneResourceName)
		{
			if (activeSceneResourceName == null)
			{
				return null;
			}
			return (from e in Items
				where e.SectorName.Trim() == activeSceneResourceName.Trim()
				select e.TextureName.Trim()).FirstOrDefault();
		}
	}
}
