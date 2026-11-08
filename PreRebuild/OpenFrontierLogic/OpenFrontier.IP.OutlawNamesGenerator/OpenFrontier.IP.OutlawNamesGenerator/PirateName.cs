using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.OutlawNamesGenerator
{
	public class PirateName
	{
		public PirateNamePrefixType PrefixType { get; set; }

		public string SingularName { get; set; }

		public string PluralName { get; set; }

		public GenderChoice Gender { get; set; }

		public bool AllowNameAfterCount { get; set; }
	}
}
