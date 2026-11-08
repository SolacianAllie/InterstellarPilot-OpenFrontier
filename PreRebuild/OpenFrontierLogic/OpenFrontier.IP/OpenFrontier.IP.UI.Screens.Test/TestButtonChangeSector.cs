using System;
using OpenFrontier.IP.Testing.MovePlayer;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class TestButtonChangeSector : MonoBehaviour
	{
		private Button button;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		public void OnClick()
		{
			try
			{
				MovePlayerTestUtils.MoveLocalUnitToAnotherSector();
			}
			catch (Exception ex)
			{
				UIController.Instance.ShowMessageBox(ex.Message, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
			}
		}
	}
}
