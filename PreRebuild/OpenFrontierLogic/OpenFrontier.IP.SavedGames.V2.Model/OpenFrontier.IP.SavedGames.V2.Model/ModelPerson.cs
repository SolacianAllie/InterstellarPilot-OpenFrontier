namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPerson
	{
		public int Seed { get; set; }

		public bool IsMale { get; set; }

		public bool IsAutoPilot { get; set; }

		public ModelNpcPilotSettings NpcPilotSettings { get; set; }

		public int Id { get; set; }

		public int GeneratedFirstNameId { get; set; } = -1;

		public int GeneratedLastNameId { get; set; } = -1;

		public string CustomName { get; set; }

		public string CustomShortName { get; set; }

		public string CustomTitle { get; set; }

		public int DialogId { get; set; }

		public ModelFaction Faction { get; set; }

		public bool DestroyGameObjectOnKill { get; set; } = true;

		public int Kills { get; set; }

		public int Deaths { get; set; }

		public ModelUnit CurrentUnit { get; set; }

		public bool IsPilot { get; set; }

		public ModelNpcPilot NpcPilot { get; set; }

		public float Properness { get; set; } = 0.5f;

		public float Aggression { get; set; } = 0.5f;

		public float Greed { get; set; } = 0.5f;

		public int RankId { get; set; } = -1;

		public int AvatarProfileId { get; set; } = -1;

		public sbyte DialogProfileId { get; set; } = -1;
	}
}
