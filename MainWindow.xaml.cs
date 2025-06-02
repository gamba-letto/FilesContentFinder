using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using FilesContentFinder.Models;
using System.Diagnostics;

namespace FilesContentFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region PropertyChanged Manager
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nomeProprieta)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nomeProprieta));
        }
        #endregion

        private string _startingFolder = string.Empty;
        public string StartingFolder
        {
            get => _startingFolder;
            set
            {
                _startingFolder = value;
                OnPropertyChanged(nameof(StartingFolder));
            }
        }

        private string _destinationFolder = string.Empty;
        public string DestinationFolder
        {
            get => _destinationFolder;
            set
            {
                _destinationFolder = value;
                OnPropertyChanged(nameof(DestinationFolder));
            }
        }

        private string _keyWord = string.Empty;
        public string KeyWord
        {
            get => _keyWord;
            set
            {
                _keyWord = value;
                OnPropertyChanged(nameof(KeyWord));
            }
        }

        private int _numberOfScannedFiles;
        public int NumberOfScannedFiles
        {
            get => _numberOfScannedFiles;
            set { _numberOfScannedFiles = value; OnPropertyChanged(nameof(NumberOfScannedFiles)); }
        }

        private int _numberOfFIles;
        public int NumberOfFiles
        {
            get => _numberOfFIles;
            set
            {
                _numberOfFIles = value;
                OnPropertyChanged(nameof(NumberOfFiles));
            }
        }

        private bool _loading;
        public bool Loading
        {
            get => _loading;
            set
            {
                _loading = value;
                OnPropertyChanged(nameof(Loading));
            }
        }

        public ObservableCollection<FileDetail> FileList { get; set; } = new ObservableCollection<FileDetail>();
        public ObservableCollection<FileDetail> ScannedFiles { get; set; } = new ObservableCollection<FileDetail>();
        public List<string> FileTypes { get; set; } = new List<string>() 
        {
            ".txt",
            ".png",
        };

        private string _selectedFileType = ".txt";
        public string SelectedFileType
        {
            get => _selectedFileType;
            set
            {
                _selectedFileType = value;
                OnPropertyChanged(nameof(SelectedFileType));
            }
        }

        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Not much here
        }

        private void TbStartingFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Opens the window to select the folder
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                StartingFolder = dialog.SelectedPath;
                FindFilesInsideSelectedFolder();
            }

            dialog.Dispose();
        }

        private void TbDestinationFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Opens the window to select the folder
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                DestinationFolder = dialog.SelectedPath;

            dialog.Dispose();
        }

        private void BtnCheckFolder_Click(object sender, RoutedEventArgs e)
        {
            FindFilesInsideSelectedFolder();
        }

        /// <summary>
        /// Find files inside <see cref="StartingFolder"/> and inserts them to <see cref="FileList"/>
        /// </summary>
        /// <returns></returns>
        private bool FindFilesInsideSelectedFolder()
        {
            StartingFolder = StartingFolder.Trim();

            # region Checks
            if (string.IsNullOrEmpty(StartingFolder))
            {
                System.Windows.Forms.MessageBox.Show("Select a folder by double tapping on the \"Starting Folder\" box", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!Directory.Exists(StartingFolder))
            {
                System.Windows.Forms.MessageBox.Show("Folder not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            #endregion

            List<FileInfo> files;
            files = GetFilesInsideFolder(StartingFolder);

            if (ChkScanSubFoldersToo.IsChecked.Value)
            {
                files.AddRange(SearchFilesInsideSubFolders(StartingFolder));
            }

            FileList.Clear();

            foreach (FileInfo file in files)
            {
                FileList.Add(new FileDetail
                {
                    Name = file.Name,
                    Extension = file.Extension,
                    DimensioneBytes = file.Length,
                    CreationDate = file.CreationTime,
                    FullPath = file.FullName,
                });
            }

            NumberOfFiles = FileList.Count;

            return true;
        }

        private List<FileInfo> SearchFilesInsideSubFolders(string mainFolder)
        {
            List<FileInfo> AnalizeFolder(string[] folders)
            {
                List<FileInfo> files = [];
                foreach (var folder in folders)
                {
                    files.AddRange(GetFilesInsideFolder(folder));
                    var subSubFolders = Directory.GetDirectories(folder);
                    files.AddRange(AnalizeFolder(subSubFolders));
                }

                return files;
            }

            if (!Directory.Exists(mainFolder)) return [];
            string[] subFolders = Directory.GetDirectories(mainFolder);
            return AnalizeFolder(subFolders);
        }

        private List<FileInfo> GetFilesInsideFolder(string mainFolder)
        {
            if (!Directory.Exists(mainFolder)) return [];

            DirectoryInfo dirInfo = new DirectoryInfo(mainFolder);
            return dirInfo.GetFiles().ToList();
        }

        private async void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            async Task AddFileToList(FileDetail file)
            {
                // Dispatcher necessary to notify UI-bound collection changes
                await Dispatcher.InvokeAsync((Action)(() =>
                {
                    this.ScannedFiles.Add(file);
                }));

                if (!string.IsNullOrEmpty(DestinationFolder))
                    File.Copy(file.FullPath, DestinationFolder + "\\" + file.Name);
            }

            if (FileList.Count == 0)
            {
                if (!FindFilesInsideSelectedFolder()) return;
            }

            _cts = new CancellationTokenSource();
            CancellationToken cancToken = _cts.Token;

            Loading = true;
            NumberOfScannedFiles = 0;

            ScannedFiles.Clear();

            try
            {
                await Parallel.ForEachAsync(FileList, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = cancToken
                },
                (Func<FileDetail, CancellationToken, ValueTask>)(async (file, ct) =>
                {
                    // General undo control
                    if (ct.IsCancellationRequested)
                        return;

                    if (file.Extension == SelectedFileType)
                    {
                        switch (SelectedFileType)
                        {
                            case ".txt":
                                try
                                {
                                    string contenuto = await File.ReadAllTextAsync(file.FullPath, ct);

                                    if (contenuto.Contains(KeyWord, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await AddFileToList(file);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Optional error log
                                }
                                break;

                            case ".png":
                                await AddFileToList(file);
                                break;
                            default:
                                break;
                        }
                    }

                    // Update loading value on UI thread
                    await Dispatcher.InvokeAsync((Action)(() =>
                    {
                        this.NumberOfScannedFiles++;
                    }));
                }));
            }
            catch (OperationCanceledException)
            {
                // Explicit undo
            }
            finally
            {
                Loading = false;
            }
        }

        private void ResultGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(ResultGrid.SelectedItem is FileDetail selectedFile)) return;

            if (File.Exists(selectedFile.FullPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedFile.FullPath,
                    UseShellExecute = true // 👉 Necessary to open using default app
                });
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }
    }
}