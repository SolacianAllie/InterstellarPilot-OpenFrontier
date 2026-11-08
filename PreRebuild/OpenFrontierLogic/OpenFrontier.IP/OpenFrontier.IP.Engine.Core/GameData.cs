using System;
using OpenFrontier.IP.IO;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class GameData : MonoBehaviour
	{
		public TextAsset BarTypeNamesTextAsset;

		[NonSerialized]
		public string[] BarTypeNames;

		public void Init()
		{
			BarTypeNames = IOHelper.ReadAndSplitTextLines(BarTypeNamesTextAsset);
		}
	}
}
