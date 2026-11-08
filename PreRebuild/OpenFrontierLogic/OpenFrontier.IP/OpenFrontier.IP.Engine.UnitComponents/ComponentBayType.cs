using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ComponentBayType : MonoBehaviour
	{
		public int UniqueId;

		public bool UseCustomBayNames;

		[Tooltip("Which kind of slot this is. Append-only - stored as a number, so inserting a value in the middle silently changes every existing one.")]
		public BayType BayType = BayType.Turret;

		public string FriendlyName;

		public bool IgnoreInTradeUI;

		public int OrderInTradeUI = 1000;

		public bool ShowInInfoUI = true;

		public virtual string GetIconSpriteName()
		{
			return $"{BayType}Bay";
		}
	}
}
