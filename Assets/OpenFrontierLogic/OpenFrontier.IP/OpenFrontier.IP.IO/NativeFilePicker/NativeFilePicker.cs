using System;
using System.IO;
using UnityEngine;
using SimpleFileBrowser;

/// <summary>
/// OpenFrontier-native replacement for the vendored NativeFilePicker plugin.
/// Lives in the global namespace on purpose: the project's IO code
/// (FileImporter/FileExporter/IOHelper) calls it with no namespace import,
/// exactly like the original plugin did, so their decompiled call sites are
/// untouched.
///
/// Bridges the used surface onto our own FileBrowserDialogWindow, so import/export
/// opens the in-app browser instead of the native SAF/iOS pickers the original
/// plugin drove: no OS permission round-trip (entry always returns Granted), and
/// a cancel reports null/false like the native counterpart did.
///
/// Used API, signatures preserved from the decompile (FileImporter/FileExporter):
///   PickFile(Action&lt;string&gt;, string[] extensions)                -> Permission
///   PickMultipleFiles(Action&lt;string[]&gt;, string[] extensions)      -> Permission
///   ExportFile(string path, Action&lt;bool&gt;)                         -> Permission
///   ExportMultipleFiles(string[], Action&lt;bool&gt;)                   -> Permission
///   ConvertExtensionToFileType(string)                             -> string
///   enum Permission { Granted, Denied, ShouldAsk }
/// </summary>
public static class NativeFilePicker
{
	public enum Permission
	{
		Granted = 0,
		Denied = 1,
		ShouldAsk = 2
	}

	// One of our dialogs at a time, like the native pickers.
	private static bool _busy;

	/// <summary>
	/// The original returned an Android MIME type / iOS UTI for the extension; ours
	/// returns the extension untouched because it only feeds our own dialog's filter,
	/// which normalises "dat"/".dat" on entry.
	/// </summary>
	public static string ConvertExtensionToFileType(string extension)
	{
		return extension;
	}

	public static Permission PickFile(Action<string> callback, string[] extensions = null, string title = null)
	{
		return PickImpl(false, extensions, title, delegate (string[] picked)
		{
			if (callback != null)
			{
				callback(picked != null && picked.Length > 0 ? picked[0] : null);
			}
		});
	}

	public static Permission PickMultipleFiles(Action<string[]> callback, string[] extensions = null, string title = null)
	{
		return PickImpl(true, extensions, title, callback);
	}

	private static Permission PickImpl(bool multi, string[] extensions, string title, Action<string[]> done)
	{
		if (!BeginDialog())
		{
			if (done != null)
			{
				done(null);
			}
			return Permission.Granted;
		}
		ApplyFilters(extensions);
		FileBrowserDialogWindow.Open(FileBrowser.PickMode.Files, multi, null, null, CreateTitle(title, "Import"), "Import",
			delegate (bool success, string[] picked)
			{
				EndDialog();
				if (done != null)
				{
					done(success && picked != null && picked.Length > 0 ? picked : null);
				}
			});
		return Permission.Granted;
	}

	public static Permission ExportFile(string sourcePath, Action<bool> callback = null, string fileMimeType = null)
	{
		return ExportImpl(new string[1] { sourcePath }, callback);
	}

	public static Permission ExportMultipleFiles(string[] sourcePaths, Action<bool> callback = null)
	{
		return ExportImpl(sourcePaths, callback);
	}

	private static Permission ExportImpl(string[] sources, Action<bool> done)
	{
		if (!BeginDialog())
		{
			if (done != null)
			{
				done(false);
			}
			return Permission.Granted;
		}
		if (sources == null || sources.Length == 0)
		{
			EndDialog();
			Debug.LogWarning("NativeFilePicker: export requested with no files.");
			if (done != null)
			{
				done(false);
			}
			return Permission.Granted;
		}
		for (int i = 0; i < sources.Length; i++)
		{
			if (string.IsNullOrEmpty(sources[i]) || !File.Exists(sources[i]))
			{
				EndDialog();
				Debug.LogWarning("NativeFilePicker: export source missing: " + sources[i]);
				if (done != null)
				{
					done(false);
				}
				return Permission.Granted;
			}
		}
		FileBrowser.SetFilters(true); // folders ignore filters; show everything while picking a destination
		FileBrowserDialogWindow.Open(FileBrowser.PickMode.Folders, false, null, null,
			"Choose folder to export to", "Export",
			delegate (bool success, string[] picked)
			{
				EndDialog();
				bool allOkay = success && picked != null && picked.Length > 0;
				if (allOkay)
				{
					string dir = picked[0];
					for (int i = 0; i < sources.Length; i++)
					{
						try
						{
							File.Copy(sources[i], UniqueDestination(dir, Path.GetFileName(sources[i])));
						}
						catch (Exception copyException)
						{
							Debug.LogException(copyException);
							allOkay = false;
						}
					}
				}
				if (done != null)
				{
					done(allOkay);
				}
			});
		return Permission.Granted;
	}

	/// <summary>
	/// Double-open guard: unlike the native pickers we hold no OS permission that
	/// could be denied, so a busy request reports an immediate cancel instead.
	/// </summary>
	private static bool BeginDialog()
	{
		if (_busy)
		{
			Debug.LogWarning("NativeFilePicker: a dialog is already open; ignoring the extra request.");
			return false;
		}
		_busy = true;
		return true;
	}

	private static void EndDialog()
	{
		_busy = false;
	}

	/// <summary>Offers the extensions as one filter; no extensions means no filtering.</summary>
	private static void ApplyFilters(string[] extensions)
	{
		if (extensions != null && extensions.Length > 0)
		{
			// SetFilters activates the first filter, so the dialog opens pre-filtered;
			// the dialog still offers its "All files" toggle.
			FileBrowser.SetFilters(true, new FileBrowser.Filter("Documents", extensions));
		}
		else
		{
			FileBrowser.SetFilters(true);
		}
	}

	private static string CreateTitle(string given, string fallback)
	{
		return string.IsNullOrEmpty(given) ? fallback : given;
	}

	/// <summary>Free destination path in dir, "name (2).ext"-style when taken.</summary>
	private static string UniqueDestination(string dir, string fileName)
	{
		string candidate = Path.Combine(dir, fileName);
		if (!File.Exists(candidate))
		{
			return candidate;
		}
		string name = Path.GetFileNameWithoutExtension(fileName);
		string ext = Path.GetExtension(fileName);
		int suffix = 2;
		while (suffix < 100000)
		{
			candidate = Path.Combine(dir, name + " (" + suffix + ")" + ext);
			if (!File.Exists(candidate))
			{
				return candidate;
			}
			suffix++;
		}
		return Path.Combine(dir, name + " (" + DateTime.Now.ToString("yyyyMMddHHmmss") + ")" + ext);
	}
}
