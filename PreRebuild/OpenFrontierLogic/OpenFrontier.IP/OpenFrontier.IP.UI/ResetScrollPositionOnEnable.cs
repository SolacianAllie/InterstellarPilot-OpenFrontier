using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ResetScrollPositionOnEnable : MonoBehaviour
	{
		private void OnEnable()
		{
			GetComponent<ScrollRect>().ResetScrollPosition();
		}
	}
}
