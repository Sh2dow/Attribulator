using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.Plugins.SpeedProfiles;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Windows;
using VaultLib.Core.DB;
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

		private Database database;
		private IEnumerable<LoadedFile> files;

		public MainWindow()
		{
			InitializeComponent();

			// Setup

			var services = new ServiceCollection();
			var loaders = Program.GetPluginLoaders();

			// Register services
			services.AddSingleton<ICommandService, CommandServiceImpl>();
			services.AddSingleton<IProfileService, ProfileServiceImpl>();
			services.AddSingleton<IStorageFormatService, StorageFormatServiceImpl>();
			services.AddSingleton<IPluginService, PluginServiceImpl>();

			// Set up logging
			Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
			services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

			var plugins = Program.ConfigurePlugins(services, loaders);
			serviceProvider = services.BuildServiceProvider();

			// Load everything from DI container
			Program.LoadCommands(services, serviceProvider);
			Program.LoadProfiles(services, serviceProvider);
			Program.LoadStorageFormats(services, serviceProvider);
			Program.LoadPlugins(plugins, serviceProvider);
		}

		private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new Forms.FolderBrowserDialog())
			{
				Forms.DialogResult result = dialog.ShowDialog();

				if (result == Forms.DialogResult.OK)
				{
					this.gameFolder = dialog.SelectedPath;

					var profile = this.GetProfile();
					database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
					files = profile.LoadFiles(database, this.gameFolder + "\\GLOBAL");
					database.CompleteLoad();
				}
			}
		}

		private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
		{
			if (this.database != null)
			{
				var profile = this.GetProfile();
				foreach(var file in files)
				{
					file.Group = "GLOBAL";
				}

				profile.SaveFiles(database, this.gameFolder, files);
			}
		}

		private IProfile GetProfile()
		{
			var ProfileName = this.DetectGame();
			return serviceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
		}

		private string DetectGame()
		{
			return "CARBON";
		}

		private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
		{
			if(!this.CheckUnsaved())
			{
				this.Close();
			}
		}

		private bool CheckUnsaved()
		{
			// add message box confirmation
			return true;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!this.CheckUnsaved())
			{
				e.Cancel = true;
			}
		}
	}
}
