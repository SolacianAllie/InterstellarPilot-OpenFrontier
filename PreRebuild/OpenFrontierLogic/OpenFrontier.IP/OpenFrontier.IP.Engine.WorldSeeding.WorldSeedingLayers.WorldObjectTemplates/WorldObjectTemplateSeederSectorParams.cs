using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.WorldObjectTemplates
{
	public class WorldObjectTemplateSeederSectorParams : MonoBehaviour
	{
		public float MinDistanceMultiplier;

		public float MaxDistanceMultiplier = 0.8f;

		public WorldObjectTemplateSeederSectorParams_PositionType DistanceType;

		public float MinSectorSecurity;

		public float MaxSectorSecurity = 1f;

		public SecurityTypePreference SecurityTypePreference;

		public float SecurityTypePreferenceMultiplier = 0.2f;

		public float BorderSectorPreference;

		public const int MaxSectorPositionFindIterations = 8;

		public float RequiredDeploymentRadius;

		internal bool GetSectorAndPosition(float templateRadius, out Sector sector, out Vector3 sectorPosition)
		{
			sector = null;
			sectorPosition = Vector3.zero;
			sector = GetSector();
			if (sector != null)
			{
				Vector3? sectorPositionOrNull = GetSectorPositionOrNull(templateRadius, sector);
				if (sectorPositionOrNull.HasValue)
				{
					sectorPosition = sectorPositionOrNull.Value;
					return true;
				}
			}
			return false;
		}

		private Vector3? GetSectorPositionOrNull(float templateRadius, Sector sector)
		{
			for (int i = 0; i < 8; i++)
			{
				Vector3 sectorPositionFromParams = GetSectorPositionFromParams(sector);
				float num = Mathf.Max(RequiredDeploymentRadius, templateRadius);
				if (num > 0f)
				{
					Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, sectorPositionFromParams, num, GameController.Instance.StaticNonOverlappingMask);
					if (vector.HasValue)
					{
						return vector.Value;
					}
				}
			}
			return null;
		}

		private Vector3 GetSectorPositionFromParams(Sector sector)
		{
			switch (DistanceType)
			{
			case WorldObjectTemplateSeederSectorParams_PositionType.Gate:
				return sector.GetRandomSectorPositionWithinGateDistance(Random.Range(MinDistanceMultiplier, MaxDistanceMultiplier));
			case WorldObjectTemplateSeederSectorParams_PositionType.Bounds:
				return sector.GetRandomSectorPositionWithinBounds(Random.Range(MinDistanceMultiplier, MaxDistanceMultiplier));
			case WorldObjectTemplateSeederSectorParams_PositionType.Gutter:
			{
				float actualGateDistance = sector.GetActualGateDistance();
				float maxUnitDistanceFromOriginLowerBound = GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound;
				return Geometry.RandomXZUnitVector() * Mathf.Lerp(actualGateDistance, maxUnitDistanceFromOriginLowerBound, Random.Range(MinDistanceMultiplier, MaxDistanceMultiplier));
			}
			default:
				return Vector3.zero;
			}
		}

		public Sector GetSector()
		{
			float num = 0f;
			Sector sector = null;
			foreach (Sector sector2 in EngineASX.Instance.Sectors)
			{
				sector2.RefreshSecurityLevel();
				if (sector2.SecurityLevel >= MinSectorSecurity && sector2.SecurityLevel <= MaxSectorSecurity)
				{
					float sectorScore = GetSectorScore(sector2);
					if (sector == null || sectorScore > num)
					{
						sector = sector2;
						num = sectorScore;
					}
				}
			}
			return sector;
		}

		private float GetSectorScore(Sector sector)
		{
			float num = 0f;
			if (BorderSectorPreference > 0f)
			{
				if (sector.IsSectorAnUncontrolledBorderSector())
				{
					num += BorderSectorPreference * 70f;
				}
				num += GetScoreBasedOnAdjustedSecurityType(sector);
			}
			return num;
		}

		public float GetScoreBasedOnAdjustedSecurityType(Sector sector)
		{
			float adjustedSecurityLevel = sector.AdjustedSecurityLevel;
			return SecurityTypePreference switch
			{
				SecurityTypePreference.Secure => adjustedSecurityLevel * SecurityTypePreferenceMultiplier, 
				SecurityTypePreference.Insecure => (0f - adjustedSecurityLevel) * SecurityTypePreferenceMultiplier, 
				_ => 0f, 
			};
		}
	}
}
