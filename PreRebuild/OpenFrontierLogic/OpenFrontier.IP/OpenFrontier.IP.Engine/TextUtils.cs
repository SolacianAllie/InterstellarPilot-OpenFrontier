using System.Text;

namespace OpenFrontier.IP.Engine
{
	public static class TextUtils
	{
		private static StringBuilder stringBuilder = new StringBuilder();

		public static string FromTitleCase(string titleCase)
		{
			stringBuilder.Length = 0;
			if (string.IsNullOrWhiteSpace(titleCase) || titleCase.Length < 2)
			{
				return titleCase;
			}
			int num = 0;
			for (int i = 0; i < titleCase.Length; i++)
			{
				if (char.IsUpper(titleCase[i]) || i == titleCase.Length - 1)
				{
					int num2 = ((i == titleCase.Length - 1) ? (i + 1) : i);
					for (int j = num; j < num2; j++)
					{
						stringBuilder.Append(titleCase[j]);
					}
					if (i > 0 && i < titleCase.Length - 1)
					{
						stringBuilder.Append(' ');
					}
					num = i;
				}
			}
			return stringBuilder.ToString();
		}
	}
}
