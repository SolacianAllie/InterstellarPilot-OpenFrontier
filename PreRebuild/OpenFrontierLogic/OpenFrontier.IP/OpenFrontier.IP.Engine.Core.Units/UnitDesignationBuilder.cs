using System;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public static class UnitDesignationBuilder
	{
		public static string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		public static string numerals = "0123456789";

		private static StringBuilder stringBuilder = new StringBuilder();

		public static char RandomChar()
		{
			return chars[UnityEngine.Random.Range(0, chars.Length)];
		}

		public static char RandomNumeral()
		{
			return numerals[UnityEngine.Random.Range(0, numerals.Length)];
		}

		public static void CalculateUnitDesignation(int seed, out string designation, out string shortDesignation)
		{
			System.Random random = new System.Random(seed);
			stringBuilder.Clear();
			stringBuilder.Append(RandomAlphabet(random));
			stringBuilder.Append(RandomAlphabet(random));
			stringBuilder.Append(RandomNumeral(random));
			shortDesignation = stringBuilder.ToString();
			stringBuilder.Append("-");
			stringBuilder.Append(RandomAlphabet(random));
			stringBuilder.Append(RandomAlphabet(random));
			stringBuilder.Append(RandomNumeral(random));
			stringBuilder.Append(RandomNumeral(random));
			designation = stringBuilder.ToString();
		}

		public static void CalculateFleetDesignation(int seed, out string designation, out string shortDesignation)
		{
			System.Random random = new System.Random(seed);
			stringBuilder.Clear();
			int num = random.Next(0, 26);
			int num2 = random.Next(0, 26);
			int num3 = random.Next(0, 10);
			stringBuilder.Append(GameController.Instance.NatoAlphabetTitleCase[num]);
			stringBuilder.Append("-");
			stringBuilder.Append(GameController.Instance.NatoAlphabetTitleCase[num2]);
			stringBuilder.Append("-");
			stringBuilder.Append(GameController.Instance.CountNamesTitleCase[num3]);
			designation = stringBuilder.ToString();
			stringBuilder.Clear();
			stringBuilder.Append(GameController.Instance.NatoAlphabetTitleCase[num][0]);
			stringBuilder.Append(GameController.Instance.NatoAlphabetTitleCase[num2][0]);
			stringBuilder.Append("-");
			stringBuilder.Append(num3);
			shortDesignation = stringBuilder.ToString();
		}

		public static char RandomAlphabet(System.Random random)
		{
			return chars[random.Next(0, chars.Length)];
		}

		public static char RandomNumeral(System.Random random)
		{
			return numerals[random.Next(0, numerals.Length)];
		}
	}
}
