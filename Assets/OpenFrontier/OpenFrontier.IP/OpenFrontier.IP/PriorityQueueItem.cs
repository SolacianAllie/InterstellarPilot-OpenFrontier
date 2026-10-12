namespace OpenFrontier.IP
{
	public struct PriorityQueueItem<TValue, TPriority>
	{
		private TValue _value;

		private TPriority _priority;

		public TValue Value => _value;

		public TPriority Priority => _priority;

		internal PriorityQueueItem(TValue val, TPriority pri)
		{
			_value = val;
			_priority = pri;
		}
	}
}
