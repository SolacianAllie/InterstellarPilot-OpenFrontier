using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.PurchasesStore;
using OpenFrontier.IP.billing;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class StoreScreen : ScreenBase
	{
		public ScrollRect BannersScrollRect;

		private IPProduct requestedProduct;

		public Button RestorePurchasesButton;

		public GameObject LoadingWidget;

		public Button GooglePlayHelpButton;

		private bool hasShownInitializationFailureMessage;

		public IPProduct RequestedProduct
		{
			get
			{
				return requestedProduct;
			}
			set
			{
				requestedProduct = value;
			}
		}

		protected override void awake()
		{
			base.awake();
			if (GameController.Instance.PurchaseBridge.Purchaser == null)
			{
				GameController.Instance.PurchaseBridge.InitialisePurchaser();
			}
			GameController.Instance.PurchaseBridge.Purchaser.PurchaseMade += Purchaser_PurchaseMade;
			GameController.Instance.PurchaseBridge.Purchaser.Initialised += Purchaser_Initialised;
			GameController.Instance.PurchaseBridge.Purchaser.PurchasesRestored += Purchaser_PurchasesRestored;
			GameController.Instance.PurchaseBridge.Purchaser.InitialisationFailed += Purchaser_InitialisationFailed;
			SetupLoadingWidget();
			SetupGooglePlayHelpButton();
			SetupRestorePurchasesButton();
		}

		protected override void start()
		{
			base.start();
			if (!hasShownInitializationFailureMessage && GameController.Instance.PurchaseBridge.Purchaser.InitializationFailureResult.HasValue)
			{
				ShowPurchaserInitializationFailedMessage();
			}
		}

		private void Purchaser_PurchasesRestored(IPurchaser sender, bool result)
		{
			if (result)
			{
				Refresh();
				UIController.Instance.ShowMessageBox("Purchases were restored successfully", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			}
			else
			{
				UIController.Instance.ShowMessageBox("An unknown error occured while attempting to restore purchases", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
			}
		}

		private void SetupLoadingWidget()
		{
			if (LoadingWidget != null)
			{
				LoadingWidget.gameObject.SetActive(ShouldShowLoadingWidget());
			}
		}

		private static bool ShouldShowLoadingWidget()
		{
			return GameController.Instance.PurchaseBridge.AnyProductsUnpurchased();
		}

		private void SetupRestorePurchasesButton()
		{
			if (RestorePurchasesButton != null)
			{
				bool active = false;
				RestorePurchasesButton.gameObject.SetActive(active);
				RestorePurchasesButton.onClick.AddListener(RestorePurchases);
			}
		}

		private void RestorePurchases()
		{
			GameController.Instance.PurchaseBridge.Purchaser.RestorePurchases();
		}

		private void SetupGooglePlayHelpButton()
		{
			if (GooglePlayHelpButton != null)
			{
				bool active = false;
				GooglePlayHelpButton.gameObject.SetActive(active);
				GooglePlayHelpButton.onClick.AddListener(GooglePlayHelpButtonClick);
			}
		}

		private void GooglePlayHelpButtonClick()
		{
			string message = "Where did my purchase go?\n\nPlease relaunch the Game from the Google Play Store while connected to the internet to sync previous purchases.\nEmail nightvizla@gmail.com for further assistance.";
			UIController.Instance.ShowMessageBox(message, MessageBoxButtons.Ok);
		}

		private void Purchaser_Initialised(IPurchaser sender)
		{
			Refresh();
		}

		private void Purchaser_InitialisationFailed(IPurchaser sender)
		{
			if (!hasShownInitializationFailureMessage)
			{
				ShowPurchaserInitializationFailedMessage();
			}
		}

		private void ShowPurchaserInitializationFailedMessage()
		{
			hasShownInitializationFailureMessage = true;
			string text = "Failed to initialize the store. Please contact support";
			int? initializationFailureResult = GameController.Instance.PurchaseBridge.Purchaser.InitializationFailureResult;
			if (initializationFailureResult.HasValue)
			{
				text = text + "\nError code: " + initializationFailureResult.Value;
			}
			UIController.Instance.ShowMessageBox(text, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
		}

		private void Purchaser_PurchaseMade(IPurchaser sender, string productId)
		{
			Refresh();
		}

		protected override void update()
		{
			base.update();
			if (LoadingWidget != null)
			{
				RefreshLoadingWidget();
			}
		}

		private void RefreshLoadingWidget()
		{
			if (LoadingWidget.gameObject.activeSelf)
			{
				IPurchaser purchaser = GameController.Instance.PurchaseBridge.Purchaser;
				bool flag = purchaser?.IsInitialised ?? false;
				LoadingWidget.SetActive(!flag && !purchaser.InitializationFailureResult.HasValue);
			}
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			if (GameController.Instance != null && GameController.Instance.PurchaseBridge != null)
			{
				GameController.Instance.PurchaseBridge.Purchaser.PurchaseMade -= Purchaser_PurchaseMade;
				GameController.Instance.PurchaseBridge.Purchaser.Initialised -= Purchaser_Initialised;
				GameController.Instance.PurchaseBridge.Purchaser.PurchasesRestored -= Purchaser_PurchasesRestored;
				GameController.Instance.PurchaseBridge.Purchaser.InitialisationFailed -= Purchaser_InitialisationFailed;
			}
		}

		protected override void refresh()
		{
			base.refresh();
			PurchaseBanner[] componentsInChildren = GetComponentsInChildren<PurchaseBanner>();
			PurchaseBanner[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Refresh();
			}
			if (requestedProduct != null)
			{
				TryScrollToRequestedProduct(componentsInChildren);
				requestedProduct = null;
			}
		}

		private void TryScrollToRequestedProduct(PurchaseBanner[] banners)
		{
			PurchaseBanner purchaseBanner = banners.FirstOrDefault((PurchaseBanner e) => e.Product == requestedProduct);
			if (purchaseBanner != null)
			{
				float normalizedPosition = (float)banners.IndexOf(purchaseBanner) / (float)(banners.Length - 1);
				BannersScrollRect.ScrollToPosition(normalizedPosition);
			}
		}
	}
}
