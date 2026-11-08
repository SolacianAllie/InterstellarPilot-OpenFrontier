using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.IP.UI
{
	public class TestMenuLoader : MonoBehaviour
	{
		private void OnGUI()
		{
			if (GUI.Button(new Rect(50f, 200f, 200f, 50f), "Testing"))
			{
				SceneManager.LoadScene("game_scene", LoadSceneMode.Single);
			}
		}
	}
}
