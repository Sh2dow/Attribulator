using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.Plugins.SpeedProfiles;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
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
					D
					var profile = new CarbonProfile();
					var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
					var files = profile.LoadFiles(database, this.gameFolder + "\\GLOBAL");
					database.CompleteLoad();

					var storageFormat = serviceProvider.GetRequiredService<IStorageFormatService>().GetStorageFormat("yml");
					storageFormat.Serialize(database, this.gameFolder + "\\GLOBAL123", files);

					int a;
					a = 1;
					a++;
				}
			}
		}
	}
}
