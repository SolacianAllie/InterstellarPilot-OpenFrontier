using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.RandomEvents
{
	public class RandomUniverseEvent : MonoBehaviour
	{
		public int MinSeedCount;

		public int MaxSeedCount;

		public WorldBase World;

		[ContextMenu("Generate Silent")]
		public void GenerateSilent()
		{
			Generate(silent: true);
		}

		[ContextMenu("Generate")]
		public void GenerateWithNotifications()
		{
			Generate();
		}

		public virtual void Generate(bool silent = false)
		{
		}

		private void Awake()
		{
			if (World == null)
			{
				World = this.FindInParents<WorldBase>();
			}
		}

		private void Start()
		{
		}
	}
}
