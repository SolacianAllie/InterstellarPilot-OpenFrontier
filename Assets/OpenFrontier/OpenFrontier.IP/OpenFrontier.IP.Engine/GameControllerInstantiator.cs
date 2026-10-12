using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class GameControllerInstantiator : MonoBehaviour
	{
		public GameController GameContollerPrefab;

		public void Awake()
		{
			if (GameController.Instance == null)
			{
				Object.Instantiate(GameContollerPrefab.gameObject);
			}
			Object.Destroy(gameObject);
		}
	}
}
