using System.Text;

namespace OpenFrontier.IP.Engine
{
	public static class StationPostscriptGenerator
	{
		private static char[] alphaBet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

		private static StringBuilder stringBuilder = new StringBuilder();

		public static string GetName(int stationTypeIndex)
		{
			int num = alphaBet.Length * alphaBet.Length;
			stationTypeIndex %= num;
			stringBuilder.Length = 0;
			if (stationTypeIndex < alphaBet.Length)
			{
				stringBuilder.Append(alphaBet[stationTypeIndex]);
			}
			else
			{
				int num2 = stationTypeIndex / alphaBet.Length;
				stringBuilder.Append(alphaBet[num2]);
				int num3 = stationTypeIndex % alphaBet.Length;
				stringBuilder.Append(alphaBet[num3]);
			}
			return stringBuilder.ToString();
		}
	}
}
