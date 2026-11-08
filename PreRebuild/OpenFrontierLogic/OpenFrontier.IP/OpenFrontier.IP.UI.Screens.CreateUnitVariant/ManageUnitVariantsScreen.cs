using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.IO;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class ManageUnitVariantsScreen : ScreenBase
	{
		public bool MultiExportSupported = true;

		public bool MultiImportSupported = true;

		public PopupMenu ContextPopupMenu;

		public Button ExportButton;

		public Button DeleteButton;

		public Button EditButton;

		public Button CreateButton;

		public ManageUnitVariantsList VariantsList;

		public Button ImportVariantButton;

		protected override void awake()
		{
			base.awake();
			EditButton.onClick.AddListener(EditButtonClick);
			DeleteButton.onClick.AddListener(DeleteButtonClick);
			CreateButton.onClick.AddListener(CreateButtonClick);
			ImportVariantButton.onClick.AddListener(ImportVariantButtonClick);
			ExportButton.onClick.AddListener(ExportButtonClick);
			VariantsList.SelectedItemsChanged += VariantsList_SelectedItemsChanged;
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshSelectedItemInfo();
			IOrderedEnumerable<ManageUnitVariantItemWrapper> items = from e in CustomUnitVariantIO.LoadCustomVariantsWithFileName()
				orderby e.CustomUnitVariant.UnitClass.UnitSeries.DisplayOrder, CustomUnitVariantHelper.CalculateMoneyValue(e.CustomUnitVariant)
				select e;
			VariantsList.SetItems(items);
		}

		private void ImportVariantButtonClick()
		{
			if (!ShouldEnableImportButton())
			{
				return;
			}
			if (!IsMultiImportSupported())
			{
				FileImporter.TryGetFileFromBrowser(this, (string filePath) =>
				{
					ImportVariantFromFilePath(filePath);
				});
				return;
			}
			FileImporter.TryGetFilesFromBrowser(this, (string[] filePaths) =>
			{
				if (filePaths.Length != 0)
				{
					if (filePaths.Length == 1)
					{
						ImportVariantFromFilePath(filePaths[0]);
					}
					else
					{
						ImportMultipleVariantsFromPaths(filePaths);
					}
				}
			});
		}

		private void ImportMultipleVariantsFromPaths(string[] filePaths)
		{
			List<CustomUnitVariant> result = new List<CustomUnitVariant>();
			IEnumerable<ManageUnitVariantItemWrapper> source = CustomUnitVariantIO.LoadCustomVariantsWithFileName();
			if (!TryLoadCustomVariantsFromFilePaths(filePaths, source.Select((ManageUnitVariantItemWrapper e) => e.CustomUnitVariant), out result))
			{
				return;
			}
			foreach (CustomUnitVariant item in result)
			{
				try
				{
					CustomUnitVariantIO.SaveCustomVariant(item);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					UIController.Instance.ShowMessageBox("An error occured while attempting to save the imported variant \"" + item.Name + "\"", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
					Refresh();
					return;
				}
			}
			UIController.Instance.ShowMessageBox($"{result.Count} custom variants were imported.", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			Refresh();
		}

		private bool TryLoadCustomVariantsFromFilePaths(string[] filePaths, IEnumerable<CustomUnitVariant> existingVariants, out List<CustomUnitVariant> result)
		{
			result = new List<CustomUnitVariant>();
			for (int i = 0; i < filePaths.Length; i++)
			{
				if (CustomUnitVariantIO.TryLoadValidCustomVariantOrError(filePaths[i], existingVariants, out var customUnitVariant))
				{
					result.Add(customUnitVariant);
					continue;
				}
				return false;
			}
			return true;
		}

		private void ImportVariantFromFilePath(string filePath)
		{
			IEnumerable<ManageUnitVariantItemWrapper> source = CustomUnitVariantIO.LoadCustomVariantsWithFileName();
			if (CustomUnitVariantIO.TryLoadValidCustomVariantOrError(filePath, source.Select((ManageUnitVariantItemWrapper e) => e.CustomUnitVariant), out var customUnitVariant))
			{
				try
				{
					CustomUnitVariantIO.SaveCustomVariant(customUnitVariant);
					UIController.Instance.ShowMessageBox("New unit variant \"" + customUnitVariant.FullName + "\" was imported.", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
					Refresh();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					UIController.Instance.ShowMessageBox("An error occured while attempting to save the imported variant", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				}
			}
		}

		private void ExportButtonClick()
		{
			if (ShouldEnableExportButton() && VariantsList.SelectedItemCount > 0)
			{
				ManageUnitVariantItemWrapper singleSelectedItem = VariantsList.SingleSelectedItem;
				if (singleSelectedItem != null)
				{
					ExportVariant(this, singleSelectedItem);
				}
				else
				{
					ExportMultipleVariants(VariantsList.SelectedItems);
				}
			}
		}

		private void ExportMultipleVariants(IEnumerable<ManageUnitVariantItemWrapper> variants)
		{
			IEnumerable<string> source = variants.Select((ManageUnitVariantItemWrapper e) =>
			{
				string fileNameWithoutDirectory = e.FileNameWithoutDirectory;
				return e.FileNameWithoutDirectory = CustomUnitVariantIO.PrepareVariantForExport(fileNameWithoutDirectory);
			});
			FileExporter.TryExportFiles(this, source.Select((string e) => CustomUnitVariantIO.GetFullPath(e)), (bool success) =>
			{
				if (success)
				{
					UIController.Instance.ShowMessageBox("Variants were exported successfully.", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				}
			});
		}

		private static void ExportVariant(MonoBehaviour caller, ManageUnitVariantItemWrapper singleSelectedItem)
		{
			string fileNameWithoutDirectory = singleSelectedItem.FileNameWithoutDirectory;
			fileNameWithoutDirectory = (singleSelectedItem.FileNameWithoutDirectory = CustomUnitVariantIO.PrepareVariantForExport(fileNameWithoutDirectory));
			FileExporter.TryExportFile(caller, CustomUnitVariantIO.GetFullPath(fileNameWithoutDirectory), (bool success) =>
			{
				if (success)
				{
					UIController.Instance.ShowMessageBox("Variant was exported successfully.", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				}
			});
		}

		private void VariantsList_SelectedItemsChanged(ScrollListBase sender)
		{
			RefreshSelectedItemInfo();
		}

		private void RefreshSelectedItemInfo()
		{
			EditButton.gameObject.SetActive(VariantsList.SingleSelectedItem != null);
			DeleteButton.gameObject.SetActive(VariantsList.FirstSelectedItem != null);
			ExportButton.gameObject.SetActive(ShouldEnableExportButton());
			ImportVariantButton.gameObject.SetActive(ShouldEnableImportButton());
		}

		private bool ShouldEnableExportButton()
		{
			if (VariantsList.SelectedItemCount == 0)
			{
				return false;
			}
			if (VariantsList.SelectedItemCount == 1)
			{
				return FileExporter.IsExportFileSupported();
			}
			return IsMultiExportSupported();
		}

		private bool ShouldEnableImportButton()
		{
			return FileImporter.IsImportFileSupported();
		}

		public bool IsMultiImportSupported()
		{
			if (FileImporter.IsMultiFileImportSupported())
			{
				return MultiImportSupported;
			}
			return false;
		}

		public bool IsMultiExportSupported()
		{
			if (FileExporter.IsMultiFileExportSupported())
			{
				return MultiExportSupported;
			}
			return false;
		}

		private void EditButtonClick()
		{
			if (VariantsList.FirstSelectedItem != null)
			{
				UIController.Instance.ScreenNavigator.ShowCreateCustomVariantScreen(VariantsList.FirstSelectedItem.CustomUnitVariant, isNewItem: false, VariantsList.FirstSelectedItem.FileNameWithoutDirectory);
			}
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (!navigatedForward)
			{
				Refresh();
			}
		}

		private void CreateButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCreateShipVariantWizard(null);
		}

		private void DeleteButtonClick()
		{
			if (VariantsList.SelectedItemCount <= 0)
			{
				return;
			}
			string text = ((VariantsList.SelectedItemCount != 1) ? $"{VariantsList.SelectedItemCount} selected variants" : "selected variant");
			UIController.Instance.ShowMessageBox("Are you sure you want to delete the " + text + "?", MessageBoxButtons.OkCancel, (MessageBoxScreen sender, MessageBoxResult result) =>
			{
				if (result == MessageBoxResult.Ok)
				{
					CustomUnitVariantIO.DeleteCustomVariants(VariantsList.SelectedItems.Select((ManageUnitVariantItemWrapper e) => e.FileNameWithoutDirectory));
					Refresh();
				}
			}, MessageBoxIcon.Warning, "Delete custom variant" + ((VariantsList.SelectedItemCount != 1) ? "s" : string.Empty));
		}
	}
}
