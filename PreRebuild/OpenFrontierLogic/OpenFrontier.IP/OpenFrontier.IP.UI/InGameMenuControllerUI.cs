using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class InGameMenuControllerUI : MonoBehaviour
	{
		public delegate void MenuChangedHandler(InGameMenuControllerUI sender, InGameMenuBase oldMenu);

		public bool AutoCloseMenu;

		private InGameMenuBase currentGameMenu;

		private EngineASX engine;

		private float expiryTime;

		private HudScreen hud;

		public InGameMenuBase InitialMenu;

		public Button OpenMenuButton;

		public InGameMenuBase CurrentGameMenu
		{
			get
			{
				return currentGameMenu;
			}
			set
			{
				if (currentGameMenu != value)
				{
					InGameMenuBase oldMenu = currentGameMenu;
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log(string.Format("{0}: Changing menu from {1} to {2}", this, (currentGameMenu != null) ? currentGameMenu.ToString() : "Null", (value != null) ? value.ToString() : "Null"), this, 3);
					}
					if ((bool)currentGameMenu)
					{
						currentGameMenu.gameObject.SetActive(value: false);
					}
					currentGameMenu = value;
					if (currentGameMenu != null)
					{
						currentGameMenu.gameObject.SetActive(value: true);
						currentGameMenu.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
					}
					if (MenuChanged != null)
					{
						MenuChanged(this, oldMenu);
					}
				}
			}
		}

		public EngineASX Engine => engine;

		public HudScreen Hud => hud;

		public event MenuChangedHandler MenuChanged;

		public void Close()
		{
			CurrentGameMenu = null;
		}

		public void MenuButtonOpenClick()
		{
			if (currentGameMenu == null)
			{
				InitialMenu.gameObject.SetActive(value: true);
			}
			else
			{
				OpenPreviousMenu();
			}
		}

		public void OpenPreviousMenu()
		{
			if (currentGameMenu != null)
			{
				CurrentGameMenu = currentGameMenu.BackMenu;
			}
			else
			{
				CurrentGameMenu = null;
			}
		}

		public void ResetTimeout()
		{
			expiryTime = Time.realtimeSinceStartup + Hud.InGameMenuTimeout;
		}

		public void OpenNewMenu(InGameMenuBase newMenu)
		{
			if (newMenu != currentGameMenu)
			{
				newMenu.BackMenu = currentGameMenu;
				CurrentGameMenu = newMenu;
			}
			ResetTimeout();
		}

		private void Awake()
		{
			hud = UnityObjectHelper.FindInParentsOrSelf<HudScreen>(gameObject);
			engine = EngineASX.Instance;
			OpenMenuButton.onClick.AddListener(OpenMenuButton_Activated);
		}

		private void Start()
		{
			Button[] componentsInChildren = GetComponentsInChildren<Button>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].onClick.AddListener(button_Activated);
			}
		}

		private void OpenMenuButton_Activated()
		{
			MenuButtonOpenClick();
		}

		private void button_Activated()
		{
			ResetTimeout();
		}

		private void Update()
		{
			if (currentGameMenu != null && AutoCloseMenu && currentGameMenu.AllowAutoClose && Time.realtimeSinceStartup > expiryTime)
			{
				Close();
			}
		}
	}
}
