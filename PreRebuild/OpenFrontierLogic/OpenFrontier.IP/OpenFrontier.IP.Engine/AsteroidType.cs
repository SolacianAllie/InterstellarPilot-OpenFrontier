using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class AsteroidType : MonoBehaviour
	{
		[FormerlySerializedAs("DefaultPrefabSettings")]
		public AsteroidFieldPrefabSettings AsteroidFieldPrefabSettings;

		public Sprite AsteroidTypeSprite;

		public Sprite UniverseMapSectorTypeSprite;

		[FormerlySerializedAs("AsteroidPrefabs")]
		public List<Asteroid> AsteroidUnitPrefabs = new List<Asteroid>();

		public List<AsteroidCluster> AsteroidClusterPrefabs = new List<AsteroidCluster>();

		public float PreferredWeighting = 1f;

		public List<Unit> GasCloudPrefabs = new List<Unit>();
	}
}
