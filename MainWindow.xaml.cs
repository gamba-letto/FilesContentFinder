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
using static System.Net.WebRequestMethods;

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


        public ObservableCollection<FileDettaglio> ListaFile { get; set; } = new ObservableCollection<FileDettaglio>();

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

        private void BtnRicerca_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(CartellaSelezionata))
            {
                System.Windows.Forms.MessageBox.Show("Cartella non trovata", "Error",  MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
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
    }
}