using System;
using System.Windows.Controls;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for AboutDialog.xaml
	/// </summary>
	public partial class AboutDialog : TkWindow {
		public TextBox AboutTextBox {
			get { return _textBlock; }
		}

		public AboutDialog() : base(Utilities.Localization.UiText.Get("About"), "help.ico") {
			
		}
		public AboutDialog(string productVersion, string assemblyVersion, string author, string programName, string imageName) : base(Utilities.Localization.UiText.Get("About ") + programName, "help.ico"){
			InitializeComponent();

			_imageBackground.Source = ApplicationManager.PreloadResourceImage(imageName);
			_labelSoftwareName.Text = programName;
			_labelSoftwareName_2.Text = programName;
			_textBlock.Text = String.Format(Utilities.Localization.UiText.Get("{0}\n\nProduct version: {1}\nAssembly version: {2}\nAuthor: {3}\n\nThis program was designed by {3}. The software is provided \"as is\" and should be used at your own risk. The author will not be held responsible for any issues it may cause."),
				programName, productVersion, assemblyVersion, author);
		}

		public AboutDialog(string productVersion, string assemblyVersion, string author, string programName) : this(productVersion, assemblyVersion, author, programName, "aboutBackground3.jpg") {
		}
	}
}
