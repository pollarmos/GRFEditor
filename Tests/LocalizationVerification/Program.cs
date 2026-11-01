using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using GRFEditor.ApplicationConfiguration;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Localization;

internal static class Program {
	private static int _checks;
	private static readonly string Output = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");

	[STAThread]
	private static int Main(string[] args) {
		try {
			Directory.CreateDirectory(Output);
			string repository = Path.GetFullPath(args.Length == 0 ? "." : args[0]);
			CheckResources(repository);
			CheckSettings();
			CheckScreens(repository);
			Console.WriteLine("PASS " + _checks + " localization checks (English/Korean, placeholders, settings, WPF screens).");
			return 0;
		}
		catch (Exception error) {
			while (error != null) {
				Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message);
				error = error.InnerException;
			}
			return 1;
		}
	}

	private static void CheckResources(string repository) {
		var manager = new ResourceManager("Utilities.Localization.UiStrings", typeof(UiText).Assembly);
		var english = manager.GetResourceSet(CultureInfo.InvariantCulture, true, true).Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value);
		var korean = manager.GetResourceSet(CultureInfo.GetCultureInfo("ko"), true, true).Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value);
		Assert(english.Count == korean.Count && english.Count >= 1000, "Incomplete bilingual catalog.");
		foreach (var pair in english) {
			Assert(korean.ContainsKey(pair.Key), "Missing Korean resource: " + pair.Key);
			Assert(Fields(pair.Value).SequenceEqual(Fields(korean[pair.Key])), "Format placeholders differ: " + pair.Value);
			UiText.Initialize("en");
			Assert(UiText.Get(pair.Value) == pair.Value, "English source text changed.");
			UiText.Initialize("ko");
			Assert(UiText.Get(pair.Value) == korean[pair.Key], "Korean lookup failed.");
			if (Fields(pair.Value).Length > 0) {
				object[] values = Enumerable.Range(0, 12).Select(i => (object)("argument-" + i + "-테스트")).ToArray();
				string source = String.Format(CultureInfo.InvariantCulture, pair.Value, values);
				string expected = String.Format(CultureInfo.InvariantCulture, korean[pair.Key], values);
				Assert(UiText.Message(source) == expected, "Formatted message differs: " + pair.Value);
			}
		}
		foreach (string folder in new[] { "GRFEditor", "GrfToWpfBridge", "TokeiLibrary" }) {
			foreach (string file in Directory.GetFiles(Path.Combine(repository, folder), "*.xaml", SearchOption.AllDirectories)) {
				if (file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
				foreach (Match match in Regex.Matches(File.ReadAllText(file), @"\{loc:Localize (T[0-9a-f]+)\}"))
					Assert(english.ContainsKey(match.Groups[1].Value), "Unknown XAML resource.");
			}
		}
		var numeric = CultureInfo.GetCultureInfo("fr-FR");
		Thread.CurrentThread.CurrentCulture = numeric;
		UiText.Initialize("ko");
		Assert(Thread.CurrentThread.CurrentCulture.Name == "fr-FR", "UI language altered numeric culture.");
		Assert(UiText.Get(null) == null && UiText.Get("data\\sprite\\Save.spr") == "data\\sprite\\Save.spr", "User data changed.");
		string original = "Cannot access the file 'data\\테스트.grf'. It is locked by another process.";
		Assert(UiText.Message(original).Contains("data\\테스트.grf"), "Filename changed in error message.");
		Assert(UiText.Filter("Container Files|*.grf;*.gsf|All files (*.*)|*.*") == "컨테이너 파일|*.grf;*.gsf|모든 파일 (*.*)|*.*", "Filter patterns changed.");
		Expect<ArgumentException>(() => UiText.Initialize("fr"));
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Console.WriteLine("PASS bilingual resources and data/culture isolation: " + english.Count + " entries");
	}

	private static void CheckSettings() {
		string file = Path.Combine(Output, "language-config.txt");
		if (File.Exists(file)) File.Delete(file);
		GrfEditorConfiguration.ConfigAsker = new ConfigAsker(file);
		Assert(GrfEditorConfiguration.InterfaceLanguage == "en", "Default language must be English.");
		GrfEditorConfiguration.InterfaceLanguage = "ko";
		GrfEditorConfiguration.ConfigAsker = new ConfigAsker(file);
		Assert(GrfEditorConfiguration.InterfaceLanguage == "ko", "Korean preference did not persist.");
		Expect<ArgumentException>(() => GrfEditorConfiguration.InterfaceLanguage = "de");
		GrfEditorConfiguration.ConfigAsker["[GRFEditor - Interface language]"] = "de";
		Assert(GrfEditorConfiguration.InterfaceLanguage == "en", "Invalid stored language must fall back to English.");
		TokeiLibrary.Configuration.ConfigAsker = GrfEditorConfiguration.ConfigAsker;
		Console.WriteLine("PASS persistent language selection and unsupported-language rejection");
	}

	private static void CheckScreens(string repository) {
		var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
		ApplicationManager.RegisterAssembly(typeof(GRFEditor.App).Assembly);
		application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/GRF Editor;component/WPF/Styles/GRFEditorStyles.xaml", UriKind.Relative) });
		application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/GRF Editor;component/WPF/Styles/StyleLightBlue.xaml", UriKind.Relative) });
		foreach (string language in new[] { "en", "ko" }) {
			UiText.Initialize(language);
			var main = XDocument.Load(Path.Combine(repository, "GRFEditor/EditorMainWindow.xaml"));
			var menuXml = new XElement(main.Root.Descendants().First(e => e.Name.LocalName == "Menu"));
			foreach (var ns in main.Root.Attributes().Where(a => a.IsNamespaceDeclaration))
				if (menuXml.Attribute(ns.Name) == null) menuXml.Add(new XAttribute(ns));
			RemoveEvents(menuXml);
			var menu = (Menu)XamlReader.Parse(menuXml.ToString());
			var headers = menu.Items.OfType<MenuItem>().Select(m => m.Header is Label ? Convert.ToString(((Label)m.Header).Content) : Convert.ToString(m.Header)).ToArray();
			Assert(headers.Contains(language == "ko" ? "파일" : "File"), "Main menu not localized.");
			MenuItem tools = menu.Items.OfType<MenuItem>().Last();
			var languages = tools.Items.OfType<MenuItem>().Single(m => m.Items.OfType<MenuItem>().Any(i => Convert.ToString(i.Header) == "한국어"));
			Assert(languages.Items.Count == 2, "Only English and Korean should be selectable.");
			var frame = new Border { Background = Brushes.White, Padding = new Thickness(8), Child = menu };
			Render(frame, Path.Combine(Output, "menu-" + language + ".png"), 900, 70);

			var dialog = new MessageDialog("Please select a file first.", MessageBoxButton.YesNoCancel);
			Assert(((TextBlock)dialog.FindName("_textBoxMessage")).Text == (language == "ko" ? "먼저 파일을 선택하세요." : "Please select a file first."), "Dialog message not localized.");
			Assert(Convert.ToString(((Button)dialog.FindName("_buttonYes")).Content) == (language == "ko" ? "예" : "Yes"), "Dialog buttons not localized.");
			dialog.Close();

			var settings = XDocument.Load(Path.Combine(repository, "GRFEditor/WPF/SettingsDialog.xaml"));
			settings.Root.Attribute(XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml")).Remove();
			RemoveEvents(settings.Root);
			var settingsWindow = (Window)XamlReader.Parse(settings.ToString());
			var content = (FrameworkElement)settingsWindow.Content;
			settingsWindow.Content = null;
			var preview = new Border { Background = Brushes.White, Child = content };
			Render(preview, Path.Combine(Output, "settings-" + language + ".png"), 520, 600);
			var tabs = VisualChildren(preview).OfType<TabControl>().Single();
			Assert(tabs.Items.Count == 7, "Settings tabs are missing.");
			for (int index = 0; index < tabs.Items.Count; index++) {
				tabs.SelectedIndex = index;
				Render(preview, Path.Combine(Output, "settings-" + language + "-" + index + ".png"), 520, 600);
				string heading = Convert.ToString(((TabItem)tabs.SelectedItem).Header);
				Assert(VisualChildren(preview).OfType<Label>().Any(label => Convert.ToString(label.Content) == heading), "Settings heading does not match the localized tab.");
			}
			var encodings = new GrfToWpfBridge.Application.EncodingPicker();
			encodings.Init(null, new TypeSetting<int>(v => { }, () => 949),
				new TypeSetting<System.Text.Encoding>(v => { }, () => System.Text.Encoding.GetEncoding(949)));
			Assert(encodings.Items.Cast<Utilities.Services.EncodingView>().Any(e => e.FriendlyName == (language == "ko" ? "한국어 (949)" : "Korean (949)")), "Encoding display name was not localized.");
			Assert(encodings.CurrentEncoding.CodePage == 949, "Localization changed the selected file encoding.");
			settingsWindow.Close();
		}
		application.Shutdown();
		Console.WriteLine("PASS actual menu/settings XAML and message dialogs in both languages");
	}

	private static System.Collections.Generic.IEnumerable<DependencyObject> VisualChildren(DependencyObject parent) {
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
			var child = VisualTreeHelper.GetChild(parent, i);
			yield return child;
			foreach (var descendant in VisualChildren(child)) yield return descendant;
		}
	}

	private static void RemoveEvents(XElement element) {
		foreach (var node in element.DescendantsAndSelf().Where(e =>
			e.Name.NamespaceName.StartsWith("clr-namespace:GRFEditor", StringComparison.Ordinal) &&
			!e.Name.NamespaceName.Contains(";assembly=")).ToArray())
			node.Name = XName.Get(node.Name.LocalName, node.Name.NamespaceName + ";assembly=GRF Editor");
		foreach (var attribute in element.DescendantsAndSelf().Attributes().Where(a =>
			a.IsNamespaceDeclaration && a.Value.StartsWith("clr-namespace:GRFEditor", StringComparison.Ordinal) &&
			!a.Value.Contains(";assembly=")).ToArray())
			attribute.Value += ";assembly=GRF Editor";
		var events = new HashSet<string>(new[] { "Click", "Loaded", "Unloaded", "SelectionChanged", "TextChanged", "Checked", "Unchecked", "MouseDown", "MouseUp", "MouseMove", "KeyDown", "KeyUp", "PreviewMouseDown", "PreviewMouseLeftButtonDown", "MouseLeftButtonDown", "MouseLeftButtonUp", "MouseRightButtonDown", "PreviewKeyDown", "Drop", "DragEnter", "DragOver", "PreviewMouseWheel", "IsVisibleChanged", "SizeChanged" });
		foreach (var node in element.DescendantsAndSelf()) {
			foreach (var attribute in node.Attributes().Where(a => events.Contains(a.Name.LocalName) ||
				(a.Value.StartsWith("_", StringComparison.Ordinal) && a.Name.LocalName != "Name" && a.Name.LocalName != "Key" && a.Name.LocalName != "Uid")).ToArray())
				attribute.Remove();
		}
	}
	private static void Render(FrameworkElement element, string file, int width, int height) {
		element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height)); element.UpdateLayout();
		var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element);
		var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using (var stream = File.Create(file)) encoder.Save(stream);
	}
	private static string[] Fields(string text) { return Regex.Matches(text, @"\{\d+(?:[^{}]*)\}").Cast<Match>().Select(m => m.Value).OrderBy(v => v).ToArray(); }
	private static void Assert(bool condition, string message) { _checks++; if (!condition) throw new Exception(message); }
	private static void Expect<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
}
