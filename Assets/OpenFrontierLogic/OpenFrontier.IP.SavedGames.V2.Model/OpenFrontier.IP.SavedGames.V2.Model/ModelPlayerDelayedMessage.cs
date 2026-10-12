namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayerDelayedMessage
	{
		public ModelPlayerMessage Message { get; set; }

		public double ShowTime { get; set; }

		public bool Important { get; set; }

		public bool Notifications { get; set; } = true;
	}
}
