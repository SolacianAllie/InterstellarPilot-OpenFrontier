using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.IO;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.SectorNaming
{
	public class SectorNamer : MonoBehaviour
	{
		public double PrefixProbability = 0.1;

		public double NumberPosfixProbability = 0.05;

		public TextAsset FirstPartsFile;

		public TextAsset LastPartsFile;

		public TextAsset PrefixesFile;

		private List<string> firstParts;

		private List<string> lastParts;

		private List<string> prefixes;

		private bool hasInit;

		public int SceneNameSimilarityTolerance = 3;

		public void Awake()
		{
			if (!hasInit)
			{
				LoadNames();
				hasInit = true;
			}
		}

		public void LoadNames()
		{
			firstParts = ReadNames(FirstPartsFile);
			lastParts = ReadNames(LastPartsFile);
			prefixes = ReadNames(PrefixesFile);
		}

		private List<string> ReadNames(TextAsset t)
		{
			return IOHelper.ReadAndSplitTextLines(t).ToList();
		}

		public string GenerateShortName(System.Random random)
		{
			return firstParts.GetRandom(random) + lastParts.GetRandom(random);
		}

		public string GenerateLongName(double prefixProb, double numberPostfixProb, System.Random random)
		{
			string text = GenerateShortName(random);
			if ((double)random.NextFloat() < prefixProb)
			{
				text = prefixes.GetRandom(random) + " " + text;
			}
			else if ((double)random.NextFloat() < numberPostfixProb)
			{
				text = $"{text} {100 + random.Next(0, 872):000}";
			}
			return text;
		}

		public bool TryGenerateName(IEnumerable<string> existingNames, int maxAttempts, double prefixProb, double numberPostfixProb, System.Random random, out string name)
		{
			int num = 0;
			do
			{
				name = GenerateLongName(prefixProb, numberPostfixProb, random);
				if (!MatchesExisting(name, existingNames, SceneNameSimilarityTolerance))
				{
					return true;
				}
				num++;
			}
			while (num < maxAttempts);
			return false;
		}

		public bool MatchesExisting(string str, IEnumerable<string> existingNames, int similarityTolerance)
		{
			if (existingNames != null)
			{
				foreach (string existingName in existingNames)
				{
					if (LevenshteinDistance.Dist(str, existingName) <= similarityTolerance)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
