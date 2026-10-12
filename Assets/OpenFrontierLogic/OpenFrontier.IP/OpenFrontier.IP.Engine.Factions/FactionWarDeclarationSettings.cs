using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionWarDeclarationSettings : MonoBehaviour
	{
		public float BaseOpinionChangeForWar = -0.3f;

		public float OpinionChangeForJoiningFriendInWar = 0.1f;

		public float OpinionChangeForDenyingRequestToJoinFriendInWar = -0.15f;

		public double MinFactionRelationshipAgeToJoinWar = 1800.0;

		public float JoinWarRelationshipToRequestorFactor = 0.6f;

		public float JoinWarRelationshipToOppositionFactionFactor = 0.6f;

		public int MaxFriendsAskedToJoinWar = 8;

		public float RequestFriendMinOpinion = 0.04f;

		public float RequestFriendRelationshipFactor = 5f;
	}
}
