using System;
using System.Windows;

using Forms = System.Windows.Forms;

namespace AttribulatorUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string gameFolder;

		private IServiceProvider serviceProvider;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new Forms.FolderBrowserDialog())
			{
				Forms.DialogResult result = dialog.ShowDialog();

				if (result == Forms.DialogResult.OK)
				{
					this.gameFolder = dialog.SelectedPath;


				}
			}
		}
	}
}
