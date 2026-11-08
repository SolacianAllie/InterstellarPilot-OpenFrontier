using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class MessageTemplate : MonoBehaviour
	{
		public string FromText;

		[TextArea(3, 8)]
		public string MessageText;

		public string SubjectText;

		public string ToText;

		public int UniqueId;
	}
}
