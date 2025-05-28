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
        #region Gestione PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nomeProprieta)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nomeProprieta));
        }
        #endregion

        private string _cartellaSelezionata = string.Empty;
        public string CartellaSelezionata
        {
            get => _cartellaSelezionata;
            set
            {
                _cartellaSelezionata = value;
                OnPropertyChanged(nameof(CartellaSelezionata));
            }
        }

        private string _parolaChiave = string.Empty;
        public string ParolaChiave
        {
            get => _parolaChiave;
            set
            {
                _parolaChiave = value;
                OnPropertyChanged(nameof(ParolaChiave));
            }
        }

        private int _fileAnalizzati;
        public int FileAnalizzati
        {
            get => _fileAnalizzati;
            set { _fileAnalizzati = value; OnPropertyChanged(nameof(FileAnalizzati)); }
        }

        private int _numeroFile;
        public int NumeroFile
        {
            get => _numeroFile;
            set
            {
                _numeroFile = value;
                OnPropertyChanged(nameof(NumeroFile));
            }
        }

        private bool _caricamento;
        public bool Caricamento
        {
            get => _caricamento;
            set
            {
                _caricamento = value;
                OnPropertyChanged(nameof(Caricamento));
            }
        }

        public ObservableCollection<FileDettaglio> ListaFile { get; set; } = new ObservableCollection<FileDettaglio>();
        public ObservableCollection<FileDettaglio> FileScansionati { get; set; } = new ObservableCollection<FileDettaglio>();

        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void TbCartellaRicerca_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Apro il selettore della cartella da scegliere
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                CartellaSelezionata = dialog.SelectedPath;

            dialog.Dispose();
        }

        private void BtnControllaCartella_Click(object sender, RoutedEventArgs e)
        {
            CercaFileNellaCartellaSelezionata();
        }

        private bool CercaFileNellaCartellaSelezionata()
        {
            if (!Directory.Exists(CartellaSelezionata))
            {
                System.Windows.Forms.MessageBox.Show("Cartella non trovata", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            List<FileInfo> files;
            files = GetFileCartella(CartellaSelezionata);

            if (ChkAncheSottoCartelle.IsChecked.Value)
            {
                files.AddRange(CercaFileNelleSottoCartelle(CartellaSelezionata));
            }

            ListaFile.Clear();

            foreach (FileInfo file in files)
            {
                ListaFile.Add(new FileDettaglio
                {
                    Nome = file.Name,
                    Estensione = file.Extension,
                    DimensioneBytes = file.Length,
                    DataCreazione = file.CreationTime,
                    PercorsoCompleto = file.FullName,
                });
            }

            NumeroFile = ListaFile.Count;

            return true;
        }

        private List<FileInfo> CercaFileNelleSottoCartelle(string cartellaMadre)
        {
            List<FileInfo> AnalizzaCartelle(string[] cartelle)
            {
                List<FileInfo> files = [];
                foreach (var cartella in cartelle)
                {
                    files.AddRange(GetFileCartella(cartella));
                    var cartelleDellaCartella = Directory.GetDirectories(cartella);
                    files.AddRange(AnalizzaCartelle(cartelleDellaCartella));
                }

                return files;
            }

            if (!Directory.Exists(cartellaMadre)) return [];
            string[] cartelleInterne = Directory.GetDirectories(cartellaMadre);
            return AnalizzaCartelle(cartelleInterne);
        }

        private List<FileInfo> GetFileCartella(string cartellaMadre)
        {
            if (!Directory.Exists(cartellaMadre)) return [];

            DirectoryInfo dirInfo = new DirectoryInfo(cartellaMadre);
            return dirInfo.GetFiles().ToList();
        }

        private async void BtnCerca_Click(object sender, RoutedEventArgs e)
        {
            if (ListaFile.Count == 0)
            {
                if (!CercaFileNellaCartellaSelezionata()) return;
            }

            _cts = new CancellationTokenSource();
            CancellationToken cancToken = _cts.Token;

            Caricamento = true;
            FileAnalizzati = 0;

            FileScansionati.Clear();

            try
            {
                await Parallel.ForEachAsync(ListaFile, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = cancToken
                },
                async (file, ct) =>
                {
                    // Controllo centrale annullamento o interruzione logica
                    if (ct.IsCancellationRequested)
                        return;

                    if (file.Estensione == ".txt")
                    {
                        try
                        {
                            string contenuto = await File.ReadAllTextAsync(file.PercorsoCompleto, ct);

                            if (contenuto.Contains(ParolaChiave, StringComparison.OrdinalIgnoreCase))
                            {
                                // Dispatcher necessario per modifiche alla UI-bound collection
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    FileScansionati.Add(file);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Opzionalmente logga l'errore
                        }
                    }

                    // Aggiorna il progresso su thread UI
                    await Dispatcher.InvokeAsync(() =>
                    {
                        FileAnalizzati++;
                    });
                });
            }
            catch (OperationCanceledException)
            {
                // Annullamento esplicito
            }
            finally
            {
                Caricamento = false;
            }
        }

        private void GrigliaRisultati_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(GrigliaRisultati.SelectedItem is FileDettaglio fileSelezionato)) return;

            if (File.Exists(fileSelezionato.PercorsoCompleto))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileSelezionato.PercorsoCompleto,
                    UseShellExecute = true // 👉 Necessario per aprire con l'app di default
                });
            }
        }

        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }
    }
}