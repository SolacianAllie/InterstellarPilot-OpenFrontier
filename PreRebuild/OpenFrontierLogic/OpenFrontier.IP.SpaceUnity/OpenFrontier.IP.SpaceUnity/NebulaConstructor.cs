using System;
using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.SpaceUnity
{
	public static class NebulaConstructor
	{
		public static void InstantiateNebulas(List<string> nebulaAssetPaths, Transform _nebulaParent, GameObject _nebulaPrefab, int nebulaCount, int maxPossibleTextureCount, System.Random rand)
		{
			nebulaAssetPaths = LimitedRandomList(nebulaAssetPaths, maxPossibleTextureCount, rand);
			for (int i = 1; i <= nebulaCount; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(_nebulaPrefab);
				gameObject.transform.parent = _nebulaParent.transform;
				string text = nebulaAssetPaths[rand.Next(0, nebulaAssetPaths.Count)].ToString();
				Material material = Resources.Load<Material>(text);
				if (material == null)
				{
					throw new Exception("Could not load nebula material at path: " + text);
				}
				gameObject.GetComponent<Renderer>().material = material;
				gameObject.transform.eulerAngles = new Vector3(rand.NextFloat(0f, 360f), rand.NextFloat(0f, 360f), rand.NextFloat(0f, 360f));
				gameObject.isStatic = true;
			}
		}

		private static List<string> LimitedRandomList(List<string> _list, int _maxEntries, System.Random rand)
		{
			int num = 10000;
			List<string> list = new List<string>();
			if (_maxEntries > _list.Count)
			{
				_maxEntries = _list.Count;
			}
			for (int i = 0; i < _maxEntries; i++)
			{
				string text = "";
				int num2 = 0;
				while (text.Length == 0 || list.Contains(text))
				{
					text = _list[rand.Next(0, _list.Count)];
					if (num2++ > num)
					{
						break;
					}
				}
				list.Add(text);
			}
			return list;
		}
	}
}
