using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AsteroidFieldSpawner : MonoBehaviour
	{
		public AsteroidFieldPrefabSettings PrefabSettings;

		public AsteroidFieldPlacementSettings Settings;

		[ContextMenu("Spawn")]
		public void Spawn()
		{
			throw new NotImplementedException();
		}
	}
}
