using UnityEngine;

namespace OpenFrontier.IP.Testing
{
	public class QualitySetter : MonoBehaviour
	{
		private void OnGUI()
		{
			for (int i = 0; i < QualitySettings.names.Length; i++)
			{
				if (GUI.Button(new Rect(0f, i * 30, 100f, 25f), QualitySettings.names[i]))
				{
					QualitySettings.SetQualityLevel(i, applyExpensiveChanges: true);
				}
			}
		}
	}
}
