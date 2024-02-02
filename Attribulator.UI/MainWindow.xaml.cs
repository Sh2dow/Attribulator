using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Serialization;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.Plugins.SpeedProfiles;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Data;
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
                    this.database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
                    this.files = profile.LoadFiles(database, this.gameFolder + "\\GLOBAL");
                    this.database.CompleteLoad();

                    this.PopulateTreeView();
                }
            }
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            if (this.database != null)
            {
                var profile = this.GetProfile();
                foreach (var file in files)
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
            if (!this.CheckUnsaved())
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

        void PopulateTreeNode(VltCollection collection, TreeViewItem node)
        {
            if (collection.Children.Count > 0)
            {
                foreach (var child in collection.Children.OrderBy(x => x.Name))
                {
                    var childNode = new TreeViewItem();
                    node.Items.Add(childNode);
                    PopulateTreeNode(child, childNode);
                }
            }

            node.Header = collection.Name;
        }

        private void PopulateTreeView()
        {
            this.TreeView.Items.Clear();

            foreach (var cls in this.database.Classes.OrderBy(x => x.Name))
            {
                var node = new TreeViewItem();
                node.Header = cls.Name;

                var collections = this.database.RowManager.GetFlattenedCollections(cls.Name).OrderBy(x => x.Name);
                foreach (var collection in collections)
                {
                    if (collection.Parent == null)
                    {
                        var childNode = new TreeViewItem();
                        node.Items.Add(childNode);
                        PopulateTreeNode(collection, childNode);
                    }
                }

                this.TreeView.Items.Add(node);
            }
        }
    }
}
