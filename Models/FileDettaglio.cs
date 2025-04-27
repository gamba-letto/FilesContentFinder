using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesContentFinder.Models
{
    public class FileDettaglio
    {
        public string Nome { get; set; } = string.Empty;
        public string Estensione { get; set; } = string.Empty;
        public long DimensioneBytes { get; set; } = 0;
        public string DimensioneMB => $"{(DimensioneBytes / 1024d / 1024d):0.00} MB";
        public DateTime DataCreazione { get; set; }
        public string PercorsoCompleto { get; set; } = string.Empty;
    }
}
