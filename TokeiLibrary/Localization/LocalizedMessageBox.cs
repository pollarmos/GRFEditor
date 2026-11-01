using System.Windows;
using Utilities.Localization;

namespace TokeiLibrary.Localization {
	public static class LocalizedMessageBox {
		public static MessageBoxResult Show(string message) => MessageBox.Show(UiText.Message(message));
		public static MessageBoxResult Show(string message, string caption) => MessageBox.Show(UiText.Message(message), UiText.Get(caption));
		public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons) => MessageBox.Show(UiText.Message(message), UiText.Get(caption), buttons);
		public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage image) => MessageBox.Show(UiText.Message(message), UiText.Get(caption), buttons, image);
		public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult result) => MessageBox.Show(UiText.Message(message), UiText.Get(caption), buttons, image, result);
		public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton buttons) => MessageBox.Show(owner, UiText.Message(message), UiText.Get(caption), buttons);
		public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton buttons, MessageBoxImage image) => MessageBox.Show(owner, UiText.Message(message), UiText.Get(caption), buttons, image);
		public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult result) => MessageBox.Show(owner, UiText.Message(message), UiText.Get(caption), buttons, image, result);
	}
}
