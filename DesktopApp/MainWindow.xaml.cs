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
            DiscardSavePanel.IsEnabled = false;

            ConnectButton.Click -= DisconnectButton_Click;
            ConnectButton.Click += ConnectButton_Click;

            CloseProgressBox();
        }

        private void GuildsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GuildsBox.SelectedItem is DiscordGuild guild)
            {
                viewModel.UpdateChannelsCollection(guild);
                viewModel.UpdateOverwriteNamesCollection(guild);
                OverwriteNamesBox.IsEnabled = true;
            }
            else
                OverwriteNamesBox.IsEnabled = false;

            UpdateOverwriteGrid();
        }

        private void OverwriteNamesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOverwriteGrid();
        }

        private void UpdateOverwriteGrid()
        {
            OverwriteGrid.Children.Clear();
            OverwriteGrid.RowDefinitions.Clear();

            var guild = GuildsBox.SelectedItem as DiscordGuild;
            var overwriteName = OverwriteNamesBox.SelectedItem as DiscordNamedObject;

            if (guild is null || overwriteName is null)
                return;

            foreach (var channel in viewModel.Channels)
                CreateOverwriteGridRow(
                    guild,
                    overwriteName,
                    channel,
                    viewModel.Channels.IndexOf(channel));

            DiscardSavePanel.IsEnabled = false;
        }

        private void CreateOverwriteGridRow(
            DiscordGuild guild,
            DiscordNamedObject overwriteName,
            DiscordChannel channel,
            int row)
        {
            OverwriteGrid.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(30) });

            CreateOverwriteGridChannelLabel(channel, row);

            CreateOverwriteGridExistsToggle(channel, overwriteName, row);

            var permissions = Enum.GetValues(typeof(DiscordPermission));

            for (int i = 0; i < permissions.Length; i++)
                CreateOverwriteGridPermissionToggle(
                    overwriteName,
                    channel,
                    (DiscordPermission)permissions.GetValue(i),
                    row,
                    i + 2,
                    guild.IsAdmin);
        }

        private void CreateOverwriteGridChannelLabel(DiscordChannel channel, int rowIndex)
        {
            var label = new Label
            {
                Content = channel,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(1, 0, 1, 1),
                BorderBrush = new SolidColorBrush(Colors.Black)
            };

            if (channel.Type != DiscordChannelType.Category)
                label.Padding = new Thickness(15, 0, 0, 0);

            Grid.SetColumn(label, 0);
            Grid.SetRow(label, rowIndex);

            OverwriteGrid.Children.Add(label);
        }

        private void CreateOverwriteGridExistsToggle(
            DiscordChannel channel,
            DiscordNamedObject overwriteName,
            int rowIndex)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = new SolidColorBrush(Colors.Black)
            };

            var button = new ToggleButton()
            {
                Height = 20,
                Width = 20,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Style = (Style)this.FindResource("AddRemoveToggleButton")
            };

            button.IsChecked = channel.Overwrites.Exists(o => o.Id == overwriteName.Id);
            button.Click += this.OverwriteExistsButton_Click;

            border.Child = button;

            Grid.SetColumn(border, 1);
            Grid.SetRow(border, rowIndex);

            OverwriteGrid.Children.Add(border);
        }

        private void CreateOverwriteGridPermissionToggle(
            DiscordNamedObject overwriteName,
            DiscordChannel channel,
            DiscordPermission permission,
            int row,
            int col,
            bool canManage = false)
        {
            var rightBorderThickness =
                permission == DiscordPermission.CreateInvite
                || permission == DiscordPermission.AddReactions
                || permission == DiscordPermission.PrioritySpeaker
                ? 1 : 0;

            var border = new Border
            {
                BorderThickness = new Thickness(0, 0, rightBorderThickness, 1),
                BorderBrush = new SolidColorBrush(Colors.Black)
            };

            border.IsEnabled = canManage;
            if (!border.IsEnabled)
                border.Background = new SolidColorBrush(Colors.LightGray);

            var channelOverwrite = channel.Overwrites.FirstOrDefault(o => o.Id == overwriteName.Id);

            if (channelOverwrite is not null &&
                channelOverwrite.Permission.TryGetValue(permission, out bool? permissionValue))
            {
                var btn = new ToggleButton
                {
                    IsChecked = permissionValue,
                    Height = 25,
                    Width = 25,
                    Style = (Style)this.FindResource("PermissionToggleButton"),
                    IsThreeState = true
                };

                btn.Click += this.PermissionToggleButton_Click;

                border.Child = btn;
            }

            Grid.SetColumn(border, col);
            Grid.SetRow(border, row);

            OverwriteGrid.Children.Add(border);
        }

        private void PermissionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var overwriteName = OverwriteNamesBox.SelectedItem as DiscordNamedObject;

            var overwrites = new Dictionary<ulong, DiscordOverwrite>();
            foreach (var channel in viewModel.Channels)
                overwrites.Add(
                    channel.Id,
                    channel.Overwrites.FirstOrDefault(o => o.Id == overwriteName.Id));

            DiscardSavePanel.IsEnabled = !AreOverwriteDictionariesEqual(
                overwrites,
                ConstructOverwriteDictionaryFromGrid());
        }

        private bool AreOverwriteDictionariesEqual(
            Dictionary<ulong, DiscordOverwrite> dict1,
            Dictionary<ulong, DiscordOverwrite> dict2)
        {
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var p in dict1)
                if ((!p.Value?.Equals(dict2?[p.Key])) ?? dict2?[p.Key] is not null)
                    return false;

            return true;
        }

        private async void OverwriteExistsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            var row = Grid.GetRow((Border)button.Parent);

            var childrenList = OverwriteGrid.Children.OfType<UIElement>().ToList();
            var channel = (DiscordChannel)((Label)
                childrenList.Find(e =>
                Grid.GetColumn(e) == 0 &&
                Grid.GetRow(e) == row))
                .Content;

            var overwriteName = OverwriteNamesBox.SelectedItem as DiscordNamedObject;
            var guild = GuildsBox.SelectedItem as DiscordGuild;
            var isRole = guild.Roles.Any(r => r.Id == overwriteName.Id);

            ShowProgressBox();
            if (button.IsChecked == false)
                await viewModel.RemoveOverwrite(guild.Id, channel, overwriteName);
            else
                await viewModel.AddOverwrite(guild.Id, channel, overwriteName, isRole);
            CloseProgressBox();

            UpdateOverwriteGrid();
        }

        private Dictionary<ulong, DiscordOverwrite> ConstructOverwriteDictionaryFromGrid()
        {
            var guild = GuildsBox.SelectedItem as DiscordGuild;
            var overwriteName = OverwriteNamesBox.SelectedItem as DiscordNamedObject;
            var isRole = guild.Roles.Any(r => r.Id == overwriteName.Id);

            var dictionary = new Dictionary<ulong, DiscordOverwrite>();
            var childrenList = OverwriteGrid.Children.OfType<UIElement>().ToList();

            for (int i = 0; i < OverwriteGrid.RowDefinitions.Count; i++)
            {
                var rowElements = childrenList.FindAll(e => Grid.GetRow(e) == i);
                var channel = (DiscordChannel)((Label)rowElements.Find(e => Grid.GetColumn(e) == 0)).Content;
                var existsToggle = (ToggleButton)((Border)rowElements.Find(e => Grid.GetColumn(e) == 1)).Child;

                var overwrite = existsToggle.IsChecked == true
                    ? ConstructDiscordOverwriteFromList(rowElements, channel, overwriteName, isRole)
                    : null;

                dictionary.Add(
                    channel.Id,
                    overwrite);
            }

            return dictionary;
        }

        private DiscordOverwrite ConstructDiscordOverwriteFromList(
            List<UIElement> list,
            DiscordChannel channel,
            DiscordNamedObject overwriteName,
            bool isRole
            )
        {
            var overwrite = new DiscordOverwrite(
                    overwriteName.Id,
                    isRole,
                    channel.Type);

            var index = 2;
            foreach (DiscordPermission perm in Enum.GetValues(typeof(DiscordPermission)))
            {
                if (overwrite.Permission.ContainsKey(perm))
                {
                    var border = (Border)list.Find(e => Grid.GetColumn(e) == index);
                    overwrite.Permission[perm] = ((ToggleButton)border?.Child).IsChecked;
                }

                index++;
            }

            return overwrite;
        }

        private async void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            var guild = GuildsBox.SelectedItem as DiscordGuild;

            ShowProgressBox();
            try
            {
                var dict = ConstructOverwriteDictionaryFromGrid();

                foreach (var p in dict)
                    if (p.Value is null)
                        dict.Remove(p.Key);

                await viewModel.CommitChangedOverwrite(
                    guild.Id,
                    dict);
            }
            catch (Exception exc)
            {
                DisplayError("Could not commit overwrites", exc.ToString());
            }

            UpdateOverwriteGrid();

            CloseProgressBox();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateOverwriteGrid();
        }
    }
}
