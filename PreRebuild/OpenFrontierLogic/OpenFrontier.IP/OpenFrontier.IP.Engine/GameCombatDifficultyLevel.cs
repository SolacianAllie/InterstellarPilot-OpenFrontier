using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class GameCombatDifficultyLevel : MonoBehaviour
	{
		public string Name = "Normal";

		[FormerlySerializedAs("PlayerDamageMultiplier")]
		public float PlayerDamageReceivedMultiplier = 0.75f;

		public float PlayerDamageDealtMultiplier = 1f;

		public void Apply(EngineASX engine)
		{
		}
	}
}
