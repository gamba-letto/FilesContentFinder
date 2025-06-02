using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesContentFinder.Models
{
    public class FileDetail
    {
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long DimensioneBytes { get; set; } = 0;
        public string DimensioneMB => $"{(DimensioneBytes / 1024d / 1024d):0.00} MB";
        public DateTime CreationDate { get; set; }
        public string FullPath { get; set; } = string.Empty;
    }
}
