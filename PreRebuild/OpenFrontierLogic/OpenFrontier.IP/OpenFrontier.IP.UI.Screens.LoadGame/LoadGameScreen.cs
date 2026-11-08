using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.IO;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.LoadGame
{
	public class LoadGameScreen : ScreenBase
	{
		public Button ExportFileButton;

		public Button RenameFileButton;

		public Button ImportFileButton;

		public Button DeleteGameConfirmButton;

		public GameObject GameInfoRoot;

		public Transform MultipleItemsSelectedRoot;

		public LoadGameItemsUI ItemsList;

		public Button LoadGameButton;

		public Text ScenarioAuthorLabel;

		public Text ScenarioAuthoringToolLabel;

		public Text SaveGameCreditsLabel;

		public Text SaveGameNetWorthLabel;

		public Text SaveGameDateLabel;

		public Text SaveGameNameLabel;

		public Text SaveGamePilotLabel;

		public Text SaveGameFactionLabel;

		public Text SaveGameVersion;

		public Text SaveGameCreatedVersion;

		public Text SaveGameFilename;

		public Text SaveGameFilesize;

		public Text SaveGameSectorLabel;

		public Text SaveGamePermadeathLabel;

		public Text SaveGameScenarioDateLabel;

		public GameObject IncompatibleSaveIndicator;

		private static string[] fileSizeSuffixes = new string[7] { "B", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb" };

		public EngineSaveGameHeader FirstSelectedItem
		{
			get
			{
				return ItemsList.FirstSelectedItem;
			}
			set
			{
				ItemsList.FirstSelectedItem = value;
			}
		}

		public void LoadCurrentItem()
		{
			if (ItemsList.SingleSelectedItem != null && ItemsList.SingleSelectedItem.CanBeLoaded)
			{
				EngineASX instance = EngineASX.Instance;
				if (instance != null)
				{
					UnityEngine.Object.DestroyImmediate(instance.gameObject);
				}
				Debug.Log("Loading saved game #: " + ItemsList.SingleSelectedItem.SaveNumber);
				GameController.Instance.ScenarioLoader.TryLoadScenario(FirstSelectedItem);
			}
		}

		protected override void awake()
		{
			base.awake();
			LoadGameButton.onClick.AddListener(LoadGameButton_Pressed);
			DeleteGameConfirmButton.onClick.AddListener(DeleteGameButton_Activated);
			ItemsList.SelectedItemsChanged += ItemsList_SelectedItemsChanged;
			ImportFileButton.onClick.AddListener(ImportFileButtonClick);
			RenameFileButton.onClick.AddListener(RenameFileButtonClick);
			ExportFileButton.onClick.AddListener(ExportFileButtonClick);
		}

		protected override void refresh()
		{
			base.refresh();
			ItemsList.Refresh();
			RefreshItemData();
		}

		private void ExportFileButtonClick()
		{
			if (ShouldShowExportFileButton() && ItemsList.SelectedItemCount > 0)
			{
				EngineSaveGameHeader singleSelectedItem = ItemsList.SingleSelectedItem;
				if (singleSelectedItem != null)
				{
					ExportVariant(this, singleSelectedItem);
				}
				else
				{
					ExportMultipleVariants(ItemsList.SelectedItems);
				}
			}
		}

		private void ExportMultipleVariants(IEnumerable<EngineSaveGameHeader> headers)
		{
			IEnumerable<string> paths = headers.Select((EngineSaveGameHeader e) => e.FullPath);
			FileExporter.TryExportFiles(this, paths, (bool success) =>
			{
				if (success)
				{
					UIController.Instance.ShowMessageBox("Saved games were exported successfully.", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				}
			});
		}

		private static void ExportVariant(MonoBehaviour caller, EngineSaveGameHeader singleSelectedItem)
		{
			string fullPath = singleSelectedItem.FullPath;
			FileExporter.TryExportFile(caller, fullPath, (bool success) =>
			{
				if (success)
				{
					UIController.Instance.ShowMessageBox("Saved game was exported successfully.", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				}
			});
		}

		private void ImportFileButtonClick()
		{
			if (!ShouldShowImportFileButton())
			{
				return;
			}
			LoadGameHelper.GetSaveGameHeaders(this, (IEnumerable<EngineSaveGameHeader> importedSaveGames) =>
			{
				if (importedSaveGames != null && importedSaveGames.Any())
				{
					if (LoadGameHelper.TryImportSaveGameHeaders(importedSaveGames))
					{
						ItemsList.AddRange(importedSaveGames);
						ItemsList.SetSelectedItems(importedSaveGames);
						ItemsList.ScrollToSelected();
						string message = ((importedSaveGames.Count() > 1) ? $"{importedSaveGames.Count()} files were imported successfully." : "The file was imported successfully.");
						UIController.Instance.ShowMessageBox(message, MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
					}
					else
					{
						UIController.Instance.ShowMessageBox("An error occured while attempting to copy files locally", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
						Refresh();
					}
				}
			});
		}

		private void DeleteGameButton_Activated()
		{
			if (ShouldShowDeleteGamesButton())
			{
				AttemptDeleteSelectedGames();
			}
		}

		private void AttemptDeleteAllGames()
		{
			UIController.Instance.ShowMessageBox("Are you sure you want to delete ALL saved games?", MessageBoxButtons.OkCancel, DeleteAllGamesConfirm, MessageBoxIcon.Question);
		}

		private void AttemptDeleteSelectedGames()
		{
			string message = ((ItemsList.SelectedItemCount == 1) ? "Are you sure you want to delete this game?" : $"Are you sure you want to delete the {ItemsList.SelectedItemCount} selected games?");
			UIController.Instance.ShowMessageBox(message, MessageBoxButtons.OkCancel, DeleteSelectedGamesConfirm, MessageBoxIcon.Question);
		}

		private void DeleteAllGamesConfirm(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				DeleteAllGames();
			}
		}

		private void DeleteSelectedGamesConfirm(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				DeleteGames(ItemsList.SelectedItems);
			}
		}

		private void DeleteAllGames()
		{
			DeleteGames(ItemsList.ActiveItems);
		}

		private void DeleteGames(IEnumerable<EngineSaveGameHeader> headers)
		{
			foreach (EngineSaveGameHeader header in headers)
			{
				if (!TryDeleteItem(header))
				{
					break;
				}
			}
			Refresh();
		}

		private bool TryDeleteItem(EngineSaveGameHeader item)
		{
			try
			{
				File.Delete(item.FullPath);
				return true;
			}
			catch (Exception innerException)
			{
				Debug.LogException(new Exception("Failed to delete save game " + item.FullPath, innerException));
				UIController.Instance.ShowError("An error occured whilte attempting to delete the file \"" + Path.GetFileName(item.FullPath) + "\"");
			}
			return false;
		}

		private void ItemsList_SelectedItemsChanged(ScrollListBase sender)
		{
			RefreshItemData();
		}

		private void LoadGameButton_Pressed()
		{
			if (ShouldShowLoadGameButton())
			{
				LoadCurrentItem();
			}
		}

		private void RefreshItemData()
		{
			ExportFileButton.gameObject.SetActive(ShouldShowExportFileButton());
			ImportFileButton.gameObject.SetActive(ShouldShowImportFileButton());
			GameInfoRoot.gameObject.SetActive(ItemsList.SingleSelectedItem != null);
			MultipleItemsSelectedRoot.gameObject.SetActive(ItemsList.SelectedItemCount > 1);
			LoadGameButton.gameObject.SetActive(ShouldShowLoadGameButton());
			DeleteGameConfirmButton.gameObject.SetActive(ShouldShowDeleteGamesButton());
			if (ItemsList.SingleSelectedItem != null)
			{
				RefreshSingleSelectedItemInfo(ItemsList.SingleSelectedItem);
			}
			RenameFileButton.gameObject.SetActive(ShouldShowRenameButton());
		}

		private bool ShouldShowDeleteGamesButton()
		{
			return ItemsList.SelectedItemCount > 0;
		}

		private bool ShouldShowRenameButton()
		{
			if (ItemsList.SingleSelectedItem != null)
			{
				return !ItemsList.SingleSelectedItem.IsAutoSave;
			}
			return false;
		}

		private bool ShouldShowLoadGameButton()
		{
			if (ItemsList.SingleSelectedItem != null)
			{
				return ItemsList.SingleSelectedItem.CanBeLoaded;
			}
			return false;
		}

		private void RefreshSingleSelectedItemInfo(EngineSaveGameHeader selectedItem)
		{
			IncompatibleSaveIndicator.gameObject.SetActive(!selectedItem.CanBeLoaded);
			ScenarioInfo scenarioInfoById = GameController.Instance.GetScenarioInfoById(selectedItem.ScenarioInfoId);
			string actualTitle = LoadGameItemUI.GetActualTitle(selectedItem.ScenarioTitle, scenarioInfoById);
			if (scenarioInfoById != null)
			{
				if (selectedItem.IsAutoSave)
				{
					SaveGameNameLabel.text = actualTitle + " - Auto";
				}
				else
				{
					SaveGameNameLabel.text = actualTitle + " - " + selectedItem.SaveNumber.ToString().PadLeft(3, '0');
				}
				SaveGamePilotLabel.text = selectedItem.PilotName;
				SaveGameFactionLabel.text = ((!string.IsNullOrWhiteSpace(selectedItem.FactionName)) ? selectedItem.FactionName : "-");
				SaveGameCreditsLabel.text = TextFormattingHelper.FormatCredits(selectedItem.Credits);
				SaveGameNetWorthLabel.text = ((selectedItem.NetWorth > 0) ? TextFormattingHelper.FormatCredits(selectedItem.NetWorth) : "-");
				if (EngineASX.Instance != null && selectedItem.GameStartDate != default(DateTime))
				{
					int gameWorldDay = EngineASX.Instance.DateTimeUtils.GetGameWorldDay(selectedItem.SecondsElapsed);
					DateTime gameWorldDateTimeFromElapsedRealSeconds = EngineASX.Instance.DateTimeUtils.GetGameWorldDateTimeFromElapsedRealSeconds(selectedItem.SecondsElapsed, selectedItem.GameStartDate);
					SaveGameScenarioDateLabel.text = string.Format("{0} (Day {1})", gameWorldDateTimeFromElapsedRealSeconds.ToString("dd-MMM-yyyy H:mm"), gameWorldDay);
				}
				else
				{
					SaveGameScenarioDateLabel.text = "-";
				}
				SaveGameVersion.text = selectedItem.SaveVersion.ToString();
				SaveGameDateLabel.text = selectedItem.TimeStamp.ToString("dd-MMM-yyyy H:mm");
				SaveGameCreatedVersion.text = ((selectedItem.CreatedVersion != null && selectedItem.CreatedVersion != new Version(1, 0, 0)) ? selectedItem.CreatedVersion.ToString() : "-");
				SaveGamePermadeathLabel.text = (selectedItem.Permadeath ? "Enabled" : "Disabled");
				SaveGameFilename.text = Path.GetFileName(selectedItem.FullPath);
				SaveGameFilesize.text = BytesToString(selectedItem.FileSize);
				ScenarioAuthorLabel.text = ((!string.IsNullOrWhiteSpace(selectedItem.ScenarioAuthor)) ? selectedItem.ScenarioAuthor : "-");
				ScenarioAuthoringToolLabel.text = ((!string.IsNullOrWhiteSpace(selectedItem.ScenarioAuthoringTool)) ? selectedItem.ScenarioAuthoringTool : "-");
				if (string.IsNullOrEmpty(selectedItem.SceneName))
				{
					SaveGameSectorLabel.text = "-";
				}
				else
				{
					SaveGameSectorLabel.text = selectedItem.SceneName;
				}
			}
			else
			{
				Debug.LogWarning("Cannot display saved game item. Has no scenario", this);
			}
		}

		private static string BytesToString(long byteCount)
		{
			if (byteCount == 0L)
			{
				return "0 " + fileSizeSuffixes[0];
			}
			long num = Math.Abs(byteCount);
			int num2 = Convert.ToInt32(Math.Floor(Math.Log(num, 1024.0)));
			double num3 = Math.Round((double)num / Math.Pow(1024.0, num2), 1);
			return (double)Math.Sign(byteCount) * num3 + " " + fileSizeSuffixes[num2];
		}

		private bool ShouldShowExportFileButton()
		{
			if (ItemsList.SelectedItemCount == 0)
			{
				return false;
			}
			if (ItemsList.SelectedItemCount == 1)
			{
				return FileExporter.IsExportFileSupported();
			}
			return FileExporter.IsMultiFileExportSupported();
		}

		private bool ShouldShowImportFileButton()
		{
			return FileImporter.IsImportFileSupported();
		}

		private void RenameFileButtonClick()
		{
			if (ShouldShowRenameButton())
			{
				TryRenameItem(ItemsList.SingleSelectedItem);
			}
		}

		private void TryRenameItem(EngineSaveGameHeader item)
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen((RenameUnitScreen screen) =>
			{
				screen.TitleText = "Rename save file...";
				screen.MinCharacters = 1;
				screen.MaxCharacters = 200;
				screen.RegexPatternValidation = "^[a-zA-Z0-9_-]*$";
				screen.RegexPatternValidationMessage = "The input text is invalid. Acceptable characters: a-z, A-Z, 0-9, '-' and '_'";
				screen.NewName = Path.GetFileNameWithoutExtension(item.FullPath);
				screen.RenameConfirmed += (RenameUnitScreen handler, bool rename, string newName) =>
				{
					if (rename)
					{
						try
						{
							string path = newName + "." + SaveGameUtilities.Extension;
							string text = Path.Combine(Path.GetDirectoryName(item.FullPath), path);
							File.Move(ItemsList.FirstSelectedItem.FullPath, text);
							item.FullPath = text;
							RefreshItemData();
						}
						catch (Exception exception)
						{
							Debug.LogException(exception);
							UIController.Instance.ShowMessageBox("Unable to rename the save file. Ensure that the new name is unique", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
						}
					}
				};
			});
		}
	}
}
