using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using NameAnonymizer.Annotations;
using SourceChord.FluentWPF;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace NameAnonymizer
{
    /// <inheritdoc cref="AcrylicWindow" />
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private bool _displaySettings;
        private List<Player> _foundPlayers;
        private bool _isLoading;
        private string _rootDir;
        private Searcher _searcher;
        private Player _selectedPlayer;
        private Settings _settings;

        public MainWindow()
        {
            Settings = new Settings();

            var themeSwitcher = new ThemeSwitcher();

            themeSwitcher.OnThemeChanged += SwitchTheme;
            SwitchTheme(themeSwitcher.WatchTheme());

            InitializeComponent();
            DataContext = this;
        }

        public Settings Settings
        {
            get => _settings;
            set
            {
                if (Equals(value, _settings)) return;
                _settings = value;
                OnPropertyChanged();
            }
        }

        public string RootDir
        {
            get => _rootDir;
            set
            {
                if (value == _rootDir) return;
                _rootDir = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value == _isLoading) return;
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        public bool DisplaySettings
        {
            get => _displaySettings;
            set
            {
                if (value == _displaySettings) return;
                _displaySettings = value;
                OnPropertyChanged();
            }
        }

        public List<Player> FoundPlayers
        {
            get => _foundPlayers;
            set
            {
                if (Equals(value, _foundPlayers)) return;
                _foundPlayers = value;
                SelectedPlayer = null;
                OnPropertyChanged();
            }
        }

        public bool IsNotLoading => !IsLoading;

        public Player SelectedPlayer
        {
            get => _selectedPlayer;
            set
            {
                if (Equals(value, _selectedPlayer)) return;
                _selectedPlayer = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private static void SwitchTheme(string theme)
        {
            Application.Current.Resources.MergedDictionaries[1].Source =
                new Uri($"/Themes/{theme}.xaml", UriKind.Relative);
        }

        private void BtnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.ShowDialog();
            if (string.IsNullOrEmpty(dlg.SelectedPath)) return;

            RootDir = dlg.SelectedPath;
            _searcher = new Searcher(Settings, RootDir);

            BtnAnalyzeClick(sender, e);
        }

        private async void AnalyzeRoot()
        {
            IsLoading = true;
            FoundPlayers = await _searcher.AnalyzePlayers();
            IsLoading = false;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MainWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void BtnAnalyzeClick(object sender, RoutedEventArgs e)
        {
            if (_searcher != null) AnalyzeRoot();
        }

        private async void BtnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                IsLoading = true;
                _searcher.ReplaceWholeLine = true;
                _searcher.RemoveEmptyLine = true;
                await _searcher.ReplacePlayers(dlg.SelectedPath);
                IsLoading = false;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                IsLoading = false;
            }
        }

        private void BtnToggleDisplaySettingsClick(object sender, RoutedEventArgs e)
        {
            DisplaySettings = !DisplaySettings;
        }

        private async void BtnExportNamesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = ".csv",
                    FileName = "names"
                };

                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                IsLoading = true;

                await Task.Run(() =>
                {

                    var csv = new StringBuilder();

                    foreach (var foundPlayer in FoundPlayers)
                    {
                        csv.AppendLine($"{foundPlayer.Original};{foundPlayer.Replaced}");
                    }

                    File.WriteAllText(dlg.FileName, csv.ToString());

                    IsLoading = false;
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                IsLoading = false;
            }
        }

        private async void BtnLoadProjectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    DefaultExt = ".naproj"
                };

                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                IsLoading = true;

                await Task.Run(() =>
                {
                    var xsSubmit = new XmlSerializer(typeof(Searcher));

                    using (var sr = File.OpenText(dlg.FileName))
                    {
                        if (xsSubmit.Deserialize(sr) is Searcher searcher)
                        {
                            SelectedPlayer = null;
                            RootDir = searcher.RootDir;
                            _searcher = searcher;
                            Settings = searcher.Settings;
                            FoundPlayers = searcher.AnalyzedPlayers;
                        }
                    }

                    IsLoading = false;
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                IsLoading = false;
            }
        }

        private async void BtnSaveProjectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = ".naproj",
                    FileName = "project"
                };

                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                IsLoading = true;

                await Task.Run(() =>
                {
                    var xsSubmit = new XmlSerializer(typeof(Searcher));
                    string xml;

                    using (var sw = new StringWriter())
                    using (var writer = XmlWriter.Create(sw))
                    {
                        xsSubmit.Serialize(writer, _searcher);
                        xml = sw.ToString();
                    }

                    File.WriteAllText(dlg.FileName, xml);

                    IsLoading = false;
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                IsLoading = false;
            }
        }
    }
}