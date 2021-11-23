using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Prism.Mvvm;

namespace FtpFileDisplay.Models
{
    public class MyYesNo : BindableBase
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


        public MyYesNo()
        {
            Icon = PackIconKind.QuestionMark;
        }
    }
}
