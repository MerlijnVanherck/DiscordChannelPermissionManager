using DiscordModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel viewModel = new ViewModel();
        private Progress progressWindow;

        public MainWindow()
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void ShowProgressBox()
        {
            progressWindow = new Progress();
            this.IsEnabled = false;
            progressWindow.Show();
        }
        private void CloseProgressBox()
        {
            progressWindow.Close();
            this.IsEnabled = true;
        }

        private void DisplayError(string title, string error)
        {
            MessageBox.Show(error, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BotTokenBox.Text))
            {
                DisplayError("Failed to connect", "Please enter a bot token.");
                return;
            }

            ShowProgressBox();

            try
            {
                await viewModel.Connect(BotTokenBox.Text);
                BotTokenBox.IsEnabled = false;
                ConnectButton.Content = "Disconnect";
                GuildsBox.IsEnabled = true;
                GuildsBox.SelectedIndex = -1;
                OverwriteNamesBox.SelectedIndex = -1;
            }
            catch (Exception exc)
            {
                DisplayError("Failed to connect to Discord", exc.ToString());
            }

            ConnectButton.Click -= ConnectButton_Click;
            ConnectButton.Click += DisconnectButton_Click;

            CloseProgressBox();
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Any changes made will be lost if you haven't committed them.",
                "Disconnect?",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning)
                == MessageBoxResult.Cancel)
                return;

            ShowProgressBox();

            await viewModel.Disconnect();
            BotTokenBox.IsEnabled = true;
            ConnectButton.Content = "Connect";
            GuildsBox.IsEnabled = false;
            GuildsBox.SelectedIndex = -1;
            OverwriteNamesBox.SelectedIndex = -1;

            ConnectButton.Click -= DisconnectButton_Click;
            ConnectButton.Click += ConnectButton_Click;

            CloseProgressBox();
        }

        private void GuildsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChannelsList();

            if (GuildsBox.SelectedItem is DiscordGuild)
                OverwriteNamesBox.IsEnabled = true;
            else
                OverwriteNamesBox.IsEnabled = false;
        }

        private void OverwriteNamesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OverwriteNamesBox.SelectedItem is DiscordNamedObject)
                DiscardSavePanel.IsEnabled = true;
            else
                DiscardSavePanel.IsEnabled = false;

            UpdateOverwritesList();
        }

        private void UpdateChannelsList()
        {
            var guild = GuildsBox.SelectedItem as DiscordGuild;

            if (guild is null)
            {
                OverwriteGrid.Children.Clear();
                OverwriteGrid.RowDefinitions.Clear();
                return;
            }

            viewModel.UpdateChannelsCollection(guild);
            viewModel.UpdateOverwriteNamesCollection(guild);

            var index = OverwriteGrid.Children.IndexOf(LastFixedChild);
            OverwriteGrid.Children.RemoveRange(
                index + 1,
                OverwriteGrid.Children.Count - index - 1);

            for (int i = 0; i < viewModel.Channels.Count; i++)
            {
                OverwriteGrid.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(30) });

                var label = new Label()
                {
                    Content = viewModel.Channels[i],
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    BorderThickness = new Thickness(1, 0, 1, 1),
                    BorderBrush = new SolidColorBrush(Colors.Black)
                };

                if (viewModel.Channels[i].Type != DiscordChannelType.Category)
                    label.Padding = new Thickness(15, 0, 0, 0);

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, i);

                OverwriteGrid.Children.Add(label);
            }
        }

        private void UpdateOverwritesList()
        {
            foreach (var c in OverwriteGrid.Children.OfType<Border>().ToList())
                OverwriteGrid.Children.Remove(c);

            var guild = GuildsBox.SelectedItem as DiscordGuild;
            var overwrite = OverwriteNamesBox.SelectedItem as DiscordNamedObject;

            if (guild is null || overwrite is null)
                return;

            viewModel.UpdateOverwritesCollection(overwrite);

            ConstructOverwritesList(guild.IsAdmin);
        }

        private void ConstructOverwritesList(bool canManage = true)
        {
            DiscardSavePanel.IsEnabled = canManage;

            for (int i = 0; i < viewModel.Channels.Count; i++)
            {
                int j = 0;
                foreach (DiscordPermission b in Enum.GetValues(typeof(DiscordPermission)))
                {
                    j++;

                    var right = b == DiscordPermission.CreateInvite
                        || b == DiscordPermission.AddReactions
                        || b == DiscordPermission.PrioritySpeaker
                        ? 1 : 0;

                    var border = new Border
                    {
                        BorderThickness = new Thickness(0, 0, right, 1),
                        BorderBrush = new SolidColorBrush(Colors.Black)
                    };

                    border.IsEnabled = canManage;
                    if (!border.IsEnabled)
                        border.Background = new SolidColorBrush(Colors.LightGray);

                    if (viewModel.Overwrites[i].Permission.TryGetValue(b, out bool? perm))
                    {
                        var btn = new ToggleButton
                        {
                            IsChecked = perm,
                            Height = 25,
                            Width = 25,
                            Style = (Style)this.FindResource("PermissionToggleButton"),
                            IsThreeState = true
                        };

                        border.Child = btn;
                    }

                    Grid.SetColumn(border, j);
                    Grid.SetRow(border, i);

                    OverwriteGrid.Children.Add(border);
                }
            }
        }

        private Dictionary<ulong, DiscordOverwrite> ConstructOverwriteDictionaryFromGrid()
        {
            var overwriteName = OverwriteNamesBox.SelectedItem as DiscordNamedObject;
            var guild = GuildsBox.SelectedItem as DiscordGuild;
            var isRole = guild.Roles.Any(r => r.Id == overwriteName.Id);

            var dictionary = new Dictionary<ulong, DiscordOverwrite>();
            var borderList = OverwriteGrid.Children.OfType<Border>().ToList();

            for (int i = 0; i < viewModel.Overwrites.Count; i++)
            {
                var overwrite = new DiscordOverwrite(
                    overwriteName.Id,
                    isRole,
                    viewModel.Channels[i].Type);

                var j = 0;
                foreach (DiscordPermission b in Enum.GetValues(typeof(DiscordPermission)))
                {
                    if (overwrite.Permission.ContainsKey(b))
                        overwrite.Permission[b] = ((ToggleButton)borderList[j + Enum.GetValues(typeof(DiscordPermission)).Length * i].Child).IsChecked;
                    j++;
                }

                dictionary.Add(
                    viewModel.Channels[i].Id,
                    overwrite);
            }

            return dictionary;
        }

        private async void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            var guild = GuildsBox.SelectedItem as DiscordGuild;

            ShowProgressBox();
            try
            {
                await viewModel.CommitChangedOverwrite(
                    guild.Id,
                    ConstructOverwriteDictionaryFromGrid());
            }
            catch (Exception exc)
            {
                DisplayError("Could not commit overwrites", exc.ToString());
            }

            UpdateOverwritesList();

            CloseProgressBox();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateOverwritesList();
        }
    }
}
