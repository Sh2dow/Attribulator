using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.UI;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using Forms = System.Windows.Forms;

namespace AttribulatorUI
{
    public partial class MainWindow : Window
    {
        private string gameExe;
        private string gameFolder;

        private IServiceProvider serviceProvider;

        private Database database;
        private IEnumerable<LoadedFile> files;

        private MenuItem[] gameMenuItems;

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

            this.gameMenuItems = new MenuItem[] { this.MenuItemMostWanted, this.MenuItemCarbon, this.MenuItemProStreet, this.MenuItemUndercover, this.MenuItemWorld };
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            if (this.gameMenuItems.Any(x => x.IsChecked))
            {
                using (var dialog = new Forms.OpenFileDialog())
                {
                    dialog.Filter = "Game executable|*.exe";
                    dialog.Title = "Open game executable";

                    Forms.DialogResult result = dialog.ShowDialog();

                    if (result == Forms.DialogResult.OK)
                    {
                        this.gameExe = dialog.FileName;
                        this.gameFolder = System.IO.Path.GetDirectoryName(dialog.FileName);
                        this.GameFolderLabel.Content = this.gameFolder;

                        var profile = this.GetProfile();
                        this.database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
                        this.files = profile.LoadFiles(database, this.gameFolder + "\\GLOBAL");
                        this.database.CompleteLoad();

                        this.PopulateTreeView();
                    }
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
            var checkedGame = this.gameMenuItems.FirstOrDefault(x => x.IsChecked);
            return checkedGame?.Tag as string;
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
                    var childNode = new VltTreeViewItem(child);
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
                var node = new VltTreeViewItem(null);
                node.Header = cls.Name;

                var collections = this.database.RowManager.GetFlattenedCollections(cls.Name).OrderBy(x => x.Name);
                foreach (var collection in collections)
                {
                    if (collection.Parent == null)
                    {
                        var childNode = new VltTreeViewItem(collection);
                        node.Items.Add(childNode);
                        PopulateTreeNode(collection, childNode);
                    }
                }

                this.TreeView.Items.Add(node);
            }
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.Source as VltTreeViewItem;
            if (treeViewItem != null)
            {
                var collection = treeViewItem.Collection;
                this.EditGrid.Display(collection);
            }
        }

        private void MenuItem_Game_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as MenuItem;
            if (this.database != null)
            {
                s.IsChecked = false;
                return;
            }

            foreach (var item in gameMenuItems)
            {
                item.IsChecked = false;
            }

            s.IsChecked = true;
            this.GameFolderLabel.Content = "No game exe selected";
        }

        private void MenuItemGameRun_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.gameExe))
            {
                Process.Start(new ProcessStartInfo(this.gameExe) { WorkingDirectory = this.gameFolder });
            }
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }
    }
}
