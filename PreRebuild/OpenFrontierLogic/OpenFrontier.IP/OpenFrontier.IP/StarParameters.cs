using System;
using UnityEngine;

namespace OpenFrontier.IP
{
	[Serializable]
	public class StarParameters
	{
		public double alphaBias = 32.0;

		public double colorBias = 0.5;

		public float maxAlpha = 0.8f;

		public Color maxColor = Color.white;

		public int maxSize = 4;

		public float minAlpha = 0.05f;

		public Color minColor = Color.black;

		public int minSize = 1;

		public int minSpacing = 2;

		public double placementProbability = 0.98;

		public double sizeBias = 32.0;
	}
}
