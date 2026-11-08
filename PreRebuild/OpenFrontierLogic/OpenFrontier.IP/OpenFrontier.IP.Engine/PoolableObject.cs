using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PoolableObject : MonoBehaviour
	{
		[NonSerialized]
		public int PrefabId = -1;
	}
}
