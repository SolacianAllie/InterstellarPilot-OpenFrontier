namespace OpenFrontier.IP.Engine
{
	public struct MissionOption
	{
		public string Title { get; private set; }

		public string MethodName { get; private set; }

		public MissionOption(string title, string methodName)
		{
			this = default;
			Title = title;
			MethodName = methodName;
		}
	}
}
