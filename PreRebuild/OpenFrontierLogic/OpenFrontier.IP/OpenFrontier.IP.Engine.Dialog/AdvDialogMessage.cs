using UnityEngine;

namespace OpenFrontier.IP.Engine.Dialog
{
	public class AdvDialogMessage : MonoBehaviour
	{
		public AdvDialogType DialogType;

		public string Message;

		public float MinAggression;

		public float MaxAggression = 1f;

		public float MinGreed;

		public float MaxGreed = 1f;

		public float MinVirtue;

		public float MaxVirtue = 1f;

		public float MinOpinion = -1f;

		public float MaxOpinion = 1f;

		public float MinProperness;

		public float MaxProperness = 1f;

		public bool ExcludeFreelancer;

		public string SingularVersion { get; set; }

		public string PluralVersion { get; set; }

		public bool HasSingularPluralVariants { get; set; }

		public void Compile()
		{
			if (Message.Contains("{"))
			{
				SingularVersion = Message.Replace("{I_or_We}", "I").Replace("{my_or_our}", "my").Replace("{me_or_us}", "me");
				PluralVersion = Message.Replace("{I_or_We}", "We").Replace("{my_or_our}", "our").Replace("{me_or_us}", "us");
				HasSingularPluralVariants = true;
			}
		}
	}
}
