using System;
using System.Windows.Markup;
using Utilities.Localization;

namespace TokeiLibrary.Localization {
	/// <summary>Localizes static UI labels without touching user text or archive filenames.</summary>
	[MarkupExtensionReturnType(typeof(string))]
	public sealed class LocalizeExtension : MarkupExtension {
		public LocalizeExtension() { }
		public LocalizeExtension(string key) { Key = key; }
		public string Key { get; set; }
		public override object ProvideValue(IServiceProvider serviceProvider) {
			return UiText.GetResource(Key);
		}
	}
}
