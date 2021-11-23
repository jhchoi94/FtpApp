using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using Prism.Mvvm;

namespace FtpFileDisplay.Models
{
    public class FtpFileModel : BindableBase
    {

        public FtpFileSystemObjectType Type { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public string Size { get; set; }
        public DateTime ModifiedDate { get; set; }

    }

    public class DiretoryPathModel : BindableBase
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
    }

    public class FtpFileWorkInfo
    {
        public FtpFileSystemObjectType Type { get; set; }
        public string FullPath { get; set; }
        public string RemotePath { get; set; }
    }
}
