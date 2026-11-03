using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleFileBrowser
{
	/// <summary>
	/// OpenFrontier-native file browser dialog. Code-built uGUI window (no prefab assets):
	/// path bar with Up/Home navigation, scrollable folder/file list (paged, folders first),
	/// extension filtering from FileBrowser's filter state, multi-select, Select/Cancel.
	/// Uses legacy uGUI Text (TMP source scripts are not in this project).
	/// Built and destroyed per open; lists are read once per navigation - no per-frame work.
	/// </summary>
	public class FileBrowserDialogWindow : MonoBehaviour
	{
		private const int PageSize = 100;
		private const float RowHeight = 62f;

		private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
		private static readonly Color WindowColor = new Color(0.14f, 0.16f, 0.19f, 0.97f);
		private static readonly Color RowColor = new Color(0.13f, 0.14f, 0.17f, 1f);
		private static readonly Color RowHighlightColor = new Color(0.18f, 0.21f, 0.27f, 1f);
		private static readonly Color ButtonColor = new Color(0.16f, 0.19f, 0.24f, 1f);
		private static readonly Color ButtonPressedColor = new Color(0.09f, 0.12f, 0.16f, 1f);
		private static readonly Color DirTextColor = new Color(0.5f, 0.7f, 0.9f, 1f);
		private static readonly Color FileTextColor = new Color(0.9f, 0.9f, 0.88f, 1f);
		private static readonly Color SelectedTextColor = new Color(1f, 0.8f, 0.2f, 1f);
		private static readonly Color StatusTextColor = new Color(1f, 0.55f, 0.35f, 1f);
		private static readonly Color CheckOnColor = new Color(0.35f, 0.65f, 0.4f, 1f);

		private Canvas _canvas;
		private RectTransform _window;
		private Text _pathText;
		private Text _statusText;
		private RectTransform _listContent;
		private ScrollRect _scroll;
		private Button _selectButton;

		private FileBrowser.PickMode _mode;
		private bool _multi;
		private string _dir;
		private string _pickButtonLabel;

		private readonly List<string> _folderNames = new List<string>();
		private readonly List<string> _fileNames = new List<string>();
		private readonly HashSet<string> _selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly List<Text> _rowLabels = new List<Text>();
		private readonly List<string> _fileRowPaths = new List<string>();
		private int _appendedCount;
		private bool _exhausted;

		private Action<bool, string[]> _done;
		private Font _font;

		public static void Open(FileBrowser.PickMode mode, bool allowMultiSelection, string initialPath,
			string initialFilename, string title, string loadButtonText, Action<bool, string[]> done)
		{
			GameObject go = new GameObject("FileBrowserDialogWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FileBrowserDialogWindow));
			GameObject.DontDestroyOnLoad(go);
			FileBrowserDialogWindow self = go.GetComponent<FileBrowserDialogWindow>();
			self.Build(mode, allowMultiSelection, initialPath, title, loadButtonText, done);
		}

		// ------------------------------------------------------------------ build

		private void Build(FileBrowser.PickMode mode, bool multi, string initialPath, string title, string pickButtonLabel, Action<bool, string[]> done)
		{
			_mode = mode;
			_multi = multi && _mode == FileBrowser.PickMode.Files;
			_pickButtonLabel = pickButtonLabel != null ? pickButtonLabel : "Select";
			_done = done;
			_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

			if (string.IsNullOrEmpty(initialPath) || !Directory.Exists(initialPath))
			{
				initialPath = Application.persistentDataPath;
			}
			_dir = Path.GetFullPath(initialPath).TrimEnd(Path.DirectorySeparatorChar);

			_canvas = GetComponent<Canvas>();
			_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			_canvas.sortingOrder = 32000;
			CanvasScaler scaler = GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280f, 720f);
			scaler.matchWidthOrHeight = 1f; // landscape-first game

			// full-screen dim plate; blocks input behind the dialog
			GameObject root = new GameObject("Dim", typeof(RectTransform), typeof(Image));
			Stretch(root.GetComponent<RectTransform>(), _canvas.transform, 0f);
			root.GetComponent<Image>().color = DimColor;

			// window
			GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
			_window = windowGO.GetComponent<RectTransform>();
			_window.SetParent(root.GetComponent<RectTransform>(), false);
			_window.anchorMin = new Vector2(0.5f, 0.5f);
			_window.anchorMax = new Vector2(0.5f, 0.5f);
			_window.pivot = new Vector2(0.5f, 0.5f);
			_window.anchoredPosition = Vector2.zero;
			_window.sizeDelta = new Vector2(760f, 620f);
			windowGO.GetComponent<Image>().color = WindowColor;

			VerticalLayoutGroup layout = windowGO.GetComponent<VerticalLayoutGroup>();
			layout.spacing = 8f;
			layout.padding = new RectOffset(14, 14, 12, 12);
			layout.childForceExpandHeight = false;
			layout.childForceExpandWidth = true;
			layout.childControlHeight = true;
			layout.childControlWidth = true;

			Text header = CreateText(layout.transform, string.IsNullOrEmpty(title) ? "Browse" : title, 30, FontStyle.Bold, Color.white);
			HeaderRow(header);

			// toolbar: path + Home/Up (+ all-files toggle when filters were offered)
			GameObject tools = NewRow(layout.transform, "Tools", 54f);
			HorizontalLayoutGroup toolsLayout = tools.AddComponent<HorizontalLayoutGroup>();
			toolsLayout.spacing = 8f;
			toolsLayout.childForceExpandWidth = false;
			toolsLayout.childControlWidth = true;
			Text pathText = CreateText(tools.transform, "", 24, FontStyle.Normal, Color.white);
			_pathText = pathText;
			SetPreferred(pathText, 44f, PathPreferredWidth());
			Button home = CreateButton(tools.transform, "Home", delegate { NavigateTo(Application.persistentDataPath); });
			SetPreferred(home, 54f, 132f);
			Button up = CreateButton(tools.transform, "Up", delegate { NavigateTo(ParentOf(_dir)); });
			SetPreferred(up, 54f, 76f);
			if (FileBrowser.ShowAllFilesAvailable && FileBrowser.AvailableFilters != null && FileBrowser.AvailableFilters.Count > 0)
			{
				GameObject toggleGO = new GameObject("ToggleAllFiles", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
				RectTransform toggleRect = toggleGO.GetComponent<RectTransform>();
				toggleRect.SetParent(tools.transform, false);
				LayoutElement toggleLayout = toggleGO.GetComponent<LayoutElement>();
				toggleLayout.preferredWidth = 168f;
				toggleLayout.preferredHeight = 48f;
				Toggle toggle = toggleGO.GetComponent<Toggle>();
				GameObject checkGO = new GameObject("Check", typeof(RectTransform), typeof(Image));
				RectTransform checkRect = checkGO.GetComponent<RectTransform>();
				checkRect.SetParent(toggleRect, false);
				checkRect.anchorMin = new Vector2(0f, 0.5f);
				checkRect.anchorMax = new Vector2(0f, 0.5f);
				checkRect.sizeDelta = new Vector2(36f, 36f);
				checkRect.anchoredPosition = new Vector2(22f, 0f);
				Image checkImage = checkGO.GetComponent<Image>();
				checkImage.color = CheckOnColor;
				toggle.graphic = checkImage;
				toggle.targetGraphic = checkImage;
				Text toggleText = CreateText(toggleRect, "All files", 22, FontStyle.Normal, Color.white);
				Stretch(toggleText.rectTransform, toggleRect, 0f);
				toggleText.rectTransform.offsetMin = new Vector2(48f, 0f);
				toggleText.alignment = TextAnchor.MiddleLeft;
				toggle.isOn = FileBrowser.ActiveFilter == null;
				// no breakpoint between toggle state and re-listing
				toggle.onValueChanged.AddListener(delegate (bool value)
				{
					FileBrowser.SetShowAllFiles(value);
					RefillList();
				});
			}

			// scrollable list (transparent plate; RectMask2D clips the paged rows)
			GameObject listGO = new GameObject("ListPanel", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D), typeof(LayoutElement));
			RectTransform listRect = listGO.GetComponent<RectTransform>();
			listRect.SetParent(layout.transform, false);
			listGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
			LayoutElement listLayout = listGO.GetComponent<LayoutElement>();
			listLayout.preferredHeight = 400f;
			listLayout.flexibleHeight = 1f;

			GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
			RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
			viewportRect.SetParent(listRect, false);
			Stretch(viewportRect, listRect, 0f);

			GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
			_listContent = contentGO.GetComponent<RectTransform>();
			_listContent.SetParent(viewportRect, false);
			Stretch(_listContent, viewportRect, 0f);
			_listContent.anchorMin = new Vector2(0f, 1f);
			_listContent.anchorMax = new Vector2(1f, 1f);
			_listContent.pivot = new Vector2(0.5f, 1f);
			_listContent.offsetMin = Vector2.zero;
			_listContent.offsetMax = Vector2.zero;
			_listContent.sizeDelta = new Vector2(0f, 0f);
			VerticalLayoutGroup contentLayout = contentGO.GetComponent<VerticalLayoutGroup>();
			contentLayout.spacing = 4f;
			contentLayout.childForceExpandHeight = false;
			contentLayout.childControlHeight = true;

			_scroll = listGO.GetComponent<ScrollRect>();
			_scroll.horizontal = false;
			_scroll.movementType = ScrollRect.MovementType.Clamped;
			_scroll.scrollSensitivity = 46f;
			_scroll.viewport = viewportRect;
			_scroll.content = _listContent;
			_scroll.onValueChanged.AddListener(delegate (Vector2 value) { OnScrollMoved(value); });

			Text status = CreateText(layout.transform, "", 22, FontStyle.Normal, StatusTextColor);
			_statusText = status;
			HeaderRow(status);
			status.alignment = TextAnchor.MiddleLeft;

			// footer: Cancel + Select
			GameObject footer = NewRow(layout.transform, "Footer", 60f);
			HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
			footerLayout.spacing = 14f;
			footerLayout.childForceExpandWidth = false;
			footerLayout.childControlWidth = true;
			Button cancel = CreateButton(footer.transform, "Cancel", delegate { Close(false, new string[0]); });
			SetPreferred(cancel, 56f, 180f);
			_selectButton = CreateButton(footer.transform, _pickButtonLabel, delegate { ConfirmSelection(); });
			SetPreferred(_selectButton, 56f, 220f);

			// pad the content so the layout math settles before the first fill
			LayoutRebuilder.ForceRebuildLayoutImmediate(_window);
			RefillList();
		}

		private void HeaderRow(Text text)
		{
			GameObject go = text.gameObject;
			LayoutElement layout = go.GetComponent<LayoutElement>();
			if (layout == null)
			{
				layout = go.AddComponent<LayoutElement>();
			}
			layout.preferredHeight = 42f;
		}

		private static float PathPreferredWidth()
		{
			return 420f;
		}

		private static GameObject NewRow(Transform parent, string name, float height)
		{
			GameObject row = new GameObject(name, typeof(RectTransform));
			RectTransform rect = row.GetComponent<RectTransform>();
			rect.SetParent(parent, false);
			row.AddComponent<LayoutElement>().preferredHeight = height;
			return row;
		}

		private static void SetPreferred(Button button, float height, float width)
		{
			LayoutElement layout = button.GetComponent<LayoutElement>();
			if (layout == null)
			{
				layout = button.gameObject.AddComponent<LayoutElement>();
			}
			layout.preferredHeight = height;
			layout.preferredWidth = width;
		}

		private static void SetPreferred(Text text, float height, float width)
		{
			LayoutElement layout = text.GetComponent<LayoutElement>();
			if (layout == null)
			{
				layout = text.gameObject.AddComponent<LayoutElement>();
			}
			layout.preferredHeight = height;
			layout.preferredWidth = width;
		}

		// ------------------------------------------------------------------ listing

		private void RefillList()
		{
			ClearRows();
			_folderNames.Clear();
			_fileNames.Clear();
			_appendedCount = 0;
			_exhausted = false;

			try
			{
				string current = Path.GetFullPath(_dir);
				FileBrowser.Filter filter = FileBrowser.ActiveFilter;
				string[] directories = Directory.GetDirectories(current);
				for (int i = 0; i < directories.Length; i++)
				{
					_folderNames.Add(Path.GetFileName(directories[i]));
				}
				string[] files = Directory.GetFiles(current);
				for (int i = 0; i < files.Length; i++)
				{
					string name = Path.GetFileName(files[i]);
					if (filter == null || filter.Matches(name))
					{
						_fileNames.Add(name);
					}
				}
			}
			catch (Exception exception)
			{
				_statusText.text = "Cannot read: " + exception.Message;
			}

			_folderNames.Sort(StringComparer.OrdinalIgnoreCase);
			_fileNames.Sort(StringComparer.OrdinalIgnoreCase);
			_pathText.text = ShortPath(_dir);
			RefreshSelectButtonEnabled();
			AppendNextPage();
		}

		private void NavigateTo(string target)
		{
			if (string.IsNullOrEmpty(target))
			{
				return;
			}
			try
			{
				target = Path.GetFullPath(target);
				target = target.TrimEnd(Path.DirectorySeparatorChar);
				if (!Directory.Exists(target))
				{
					return;
				}
			}
			catch (Exception)
			{
				return;
			}
			_dir = target;
			RefillList();
		}

		private static string ParentOf(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}
			try
			{
				string parent = Path.GetDirectoryName(Path.GetFullPath(path));
				if (parent == null || parent.Length <= Path.GetPathRoot(parent).Length)
				{
					return null; // already at the drive root
				}
				return parent.TrimEnd(Path.DirectorySeparatorChar);
			}
			catch (Exception)
			{
				return null;
			}
		}

		// ------------------------------------------------------------------ rows

		private void OnScrollMoved(Vector2 position)
		{
			if (_exhausted || position.y > 0.06f)
			{
				return;
			}
			AppendNextPage();
		}

		private void AppendNextPage()
		{
			if (_exhausted)
			{
				return;
			}
			int totalEntries = _folderNames.Count + _fileNames.Count;
			int added = 0;
			while (added < PageSize && _appendedCount < totalEntries)
			{
				int index = _appendedCount;
				if (index < _folderNames.Count)
				{
					CreateRow(_folderNames[index], true);
				}
				else
				{
					CreateRow(_fileNames[index - _folderNames.Count], false);
				}
				_appendedCount++;
				added++;
			}
			if (_appendedCount >= totalEntries)
			{
				_exhausted = true;
			}
		}

		private void CreateRow(string name, bool isFolder)
		{
			GameObject row = new GameObject(isFolder ? "Dir" + name : "File" + name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
			RectTransform rowRect = row.GetComponent<RectTransform>();
			rowRect.SetParent(_listContent, false);
			rowRect.offsetMin = Vector2.zero;
			rowRect.offsetMax = Vector2.zero;
			LayoutElement layoutElement = row.GetComponent<LayoutElement>();
			layoutElement.preferredHeight = RowHeight;
			layoutElement.minHeight = RowHeight;
			Image rowImage = row.GetComponent<Image>();
			rowImage.color = RowColor;

			Text label = CreateText(rowRect, (isFolder ? "[dir]  " : "         ") + ShortName(name), 26, FontStyle.Bold,
				isFolder ? DirTextColor : FileTextColor);
			Stretch(label.rectTransform, rowRect, 0f);
			label.rectTransform.offsetMin = new Vector2(14f, 4f);
			label.rectTransform.offsetMax = new Vector2(-10f, -4f);
			label.alignment = TextAnchor.MiddleLeft;

			Button button = row.GetComponent<Button>();
			ColorBlock colors = button.colors;
			colors.normalColor = RowColor;
			colors.highlightedColor = RowHighlightColor;
			colors.pressedColor = ButtonPressedColor;
			colors.selectedColor = RowColor;
			colors.disabledColor = RowColor;
			colors.colorMultiplier = 1f;
			colors.fadeDuration = 0.05f;
			button.colors = colors;

			if (isFolder)
			{
				string folderPath = name; // captured; the click resolves against the CURRENT dir
				button.onClick.AddListener(delegate
				{
					NavigateTo(Path.Combine(_dir, folderPath));
				});
			}
			else
			{
				string fileName = name;
				_fileRowPaths.Add(Path.Combine(_dir, fileName));
				button.onClick.AddListener(delegate
				{
					ToggleFileSelection(fileName, label);
				});
			}
			_rowLabels.Add(label);
		}

		private void ToggleFileSelection(string name, Text label)
		{
			string full = Path.Combine(_dir, name);
			if (_multi)
			{
				if (!_selected.Remove(full))
				{
					_selected.Add(full);
					SyncSelectionColors();
				}
			}
			else
			{
				_selected.Clear();
				_selected.Add(full);
				SyncSelectionColors();
			}
			RefreshSelectButtonEnabled();
		}

		private void SyncSelectionColors()
		{
			// rows map 1:1 into _rowLabels for the FILES section only after the folder count; the
			// cheap approach: look up each file row's current state against its captured name.
			for (int i = 0; i < _fileRowPaths.Count && i < _rowLabels.Count; i++)
			{
				// labels include folders first; recover the exact index into the label list
				int labelIndex = _folderNames.Count + i;
				if (labelIndex >= _rowLabels.Count)
				{
					break;
				}
				Text label = _rowLabels[labelIndex];
				string filePath = _fileRowPaths[i];
				bool selected = _selected.Contains(filePath);
				label.color = selected ? SelectedTextColor : FileTextColor;
				label.fontStyle = selected ? FontStyle.BoldAndItalic : FontStyle.Bold;
			}
		}

		private void RefreshSelectButtonEnabled()
		{
			if (_selectButton == null)
			{
				return;
			}
			bool ready = _mode == FileBrowser.PickMode.Folders || _selected.Count > 0;
			Text buttonText = _selectButton.GetComponentInChildren<Text>();
			if (buttonText != null)
			{
				buttonText.text = _pickButtonLabel + (_mode == FileBrowser.PickMode.Files && _multi && _selected.Count > 0
					? " (" + _selected.Count + ")"
					: "");
			}
			if (!ready && _selected.Count == 0 && _mode != FileBrowser.PickMode.Folders)
			{
				// remain interactable; ConfirmSelection explains the missing selection
				// (a grayed button on touch screens reads as an unresponsive game)
			}
		}

		private void ConfirmSelection()
		{
			if (_mode == FileBrowser.PickMode.Folders)
			{
				Close(true, new string[1] { _dir });
				return;
			}
			if (_selected.Count == 0)
			{
				_statusText.text = "Choose at least one file.";
				return;
			}
			List<string> result = new List<string>(_selected);
			result.Sort(StringComparer.OrdinalIgnoreCase);
			Close(true, result.ToArray());
		}

		private void Close(bool success, string[] result)
		{
			Action<bool, string[]> callback = _done;
			_done = null;
			GameObject.Destroy(gameObject);
			if (callback != null)
			{
				callback(success, result);
			}
		}

		// ------------------------------------------------------------------ uGUI helpers

		private Text CreateText(Transform parent, string content, int size, FontStyle style, Color color)
		{
			GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
			Text text = go.GetComponent<Text>();
			text.rectTransform.SetParent(parent, false);
			text.font = _font;
			text.fontSize = size;
			text.fontStyle = style;
			text.color = color;
			text.supportRichText = false;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Truncate;
			text.text = content;
			text.raycastTarget = false;
			return text;
		}

		private Button CreateButton(Transform parent, string label, UnityAction onClick)
		{
			GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
			RectTransform rect = go.GetComponent<RectTransform>();
			rect.SetParent(parent, false);
			go.GetComponent<Image>().color = ButtonColor;
			Text text = CreateText(rect, label, 26, FontStyle.Bold, Color.white);
			Stretch(text.rectTransform, rect, 0f);
			SlotIn(text.rectTransform);
			text.alignment = TextAnchor.MiddleCenter;
			Button button = go.GetComponent<Button>();
			ColorBlock colors = button.colors;
			colors.normalColor = ButtonColor;
			colors.highlightedColor = RowHighlightColor;
			colors.pressedColor = ButtonPressedColor;
			colors.colorMultiplier = 1f;
			colors.fadeDuration = 0.05f;
			button.colors = colors;
			button.onClick.AddListener(onClick);
			return button;
		}

		private static void Stretch(RectTransform rect, Transform parent, float pad)
		{
			rect.SetParent(parent, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = new Vector2(pad, pad);
			rect.offsetMax = new Vector2(-pad, -pad);
		}

		private static void SlotIn(RectTransform rect)
		{
			rect.offsetMin = new Vector2(6f, 4f);
			rect.offsetMax = new Vector2(-6f, -4f);
		}

		private void ClearRows()
		{
			_selected.Clear();
			_rowLabels.Clear();
			_fileRowPaths.Clear();
			for (int i = _listContent.childCount - 1; i >= 0; i--)
			{
				GameObject.Destroy(_listContent.GetChild(i).gameObject);
			}
			_listContent.sizeDelta = new Vector2(0f, 0f);
		}

		private static string ShortPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return "";
			}
			if (path.Length > 64)
			{
				return "..." + path.Substring(path.Length - 60);
			}
			return path;
		}

		private static string ShortName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return "";
			}
			if (name.Length > 44)
			{
				return name.Substring(0, 41) + "...";
			}
			return name;
		}
	}
}
