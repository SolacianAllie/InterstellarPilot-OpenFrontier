using UnityEngine;

namespace OpenFrontier.IP
{
	public class DeletePlayerPrefsUI : MonoBehaviour
	{
		public void OnGUI()
		{
			if (GUI.Button(new Rect(20f, 20f, 300f, 40f), "Delete Em!"))
			{
				PlayerPrefs.DeleteAll();
				PlayerPrefs.Save();
			}
		}
	}
}
