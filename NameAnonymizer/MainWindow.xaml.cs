using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using NameAnonymizer.Annotations;
using SourceChord.FluentWPF;
using Application = System.Windows.Application;

namespace NameAnonymizer
{
    /// <inheritdoc cref="AcrylicWindow" />
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private List<Player> _foundPlayers;
        private bool _isLoading;
        private string _rootDir;

        private Searcher _searcher;
        private Player _selectedPlayer;

        public MainWindow()
        {
            var themeSwitcher = new ThemeSwitcher();

            themeSwitcher.OnThemeChanged += SwitchTheme;
            SwitchTheme(themeSwitcher.WatchTheme());

            InitializeComponent();
            DataContext = this;
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

        public string SearchPattern
        {
            get => Searcher.RegEx;
            set
            {
                if (value == Searcher.RegEx) return;
                Searcher.RegEx = value;
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
            _searcher = new Searcher(RootDir);
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
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            IsLoading = true;
            _searcher.ReplaceWholeLine = true;
            _searcher.RemoveEmptyLine = true;
            await _searcher.ReplacePlayers(dlg.SelectedPath);
            IsLoading = false;
        }
    }
}