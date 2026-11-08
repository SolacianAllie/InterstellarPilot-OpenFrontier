using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Dialog
{
	public struct DialogRequestArguments
	{
		public List<(string, string)> KeyValues;

		public static DialogRequestArguments Init()
		{
			return new DialogRequestArguments
			{
				KeyValues = new List<(string, string)>(4)
			};
		}

		public static DialogRequestArguments Init(string key, string value)
		{
			DialogRequestArguments result = default;
			result.KeyValues = new List<(string, string)>(4);
			result.KeyValues.Add((key, value));
			return result;
		}
	}
}
