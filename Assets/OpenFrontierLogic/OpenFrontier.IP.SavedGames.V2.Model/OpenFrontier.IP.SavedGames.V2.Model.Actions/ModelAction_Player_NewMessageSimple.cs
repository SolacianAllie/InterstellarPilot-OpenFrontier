using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Actions
{
	public class ModelAction_Player_NewMessageSimple : ModelAction
	{
		public override ActionType Type => ActionType.Player_NewMessageSimple;

		public bool Notifications { get; set; } = true;

		public string From { get; set; }

		public string Message { get; set; }

		public string Subject { get; set; }

		public string To { get; set; } = "#player#";
	}
}
