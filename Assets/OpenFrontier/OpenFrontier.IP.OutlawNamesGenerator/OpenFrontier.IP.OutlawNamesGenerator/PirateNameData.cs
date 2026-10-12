using System.Collections.Generic;

namespace OpenFrontier.IP.OutlawNamesGenerator
{
	public class PirateNameData
	{
		public string[] CountNames = new string[7] { "one", "two", "three", "four", "five", "six", "seven" };

		public string[] Adjectives { get; set; }

		public List<PirateGroupName> GroupNames { get; set; }

		public List<PirateName> Names { get; set; }
	}
}
