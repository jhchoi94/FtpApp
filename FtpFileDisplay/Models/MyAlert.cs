using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Prism.Mvvm;

namespace FtpFileDisplay.Models
{
    public class MyAlert : BindableBase
    {
        private PackIconKind icon;
        public PackIconKind Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }
        private string title;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }
        private string test;
        public string Text
        {
            get => test;
            set => SetProperty(ref test, value);
        }

        private string closeText;
        public string CloseText
        {
            get => closeText;
            set => SetProperty(ref closeText, value);
        }
        public MyAlert()
        {
            Icon = PackIconKind.Error;
            CloseText = "닫기";
        }
    }
}
