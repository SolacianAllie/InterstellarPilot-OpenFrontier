using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class FlowLayoutBrokenLayoutFix : MonoBehaviour
	{
		private void Start()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
		}
	}
}
