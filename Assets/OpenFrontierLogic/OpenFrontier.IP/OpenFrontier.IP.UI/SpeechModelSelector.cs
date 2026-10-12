using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class SpeechModelSelector : MonoBehaviour
	{
		private void OnClick()
		{
			SpeechModel speechModel = UnityObjectHelper.FindInParentsOrSelf<SpeechModel>(gameObject);
			if (speechModel != null && speechModel.ActiveRequest != null && speechModel.ActiveRequest.SourcePerson != null && speechModel.ActiveRequest.SourcePerson.CurrentUnit != null)
			{
				EngineASX.Instance.Hud.CurrentTarget = speechModel.ActiveRequest.SourcePerson.CurrentUnit;
			}
		}
	}
}
