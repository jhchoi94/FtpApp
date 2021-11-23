using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FtpFileDisplay.Models;
using MaterialDesignThemes.Wpf;

namespace FtpFileDisplay.Views
{
    /// <summary>
    /// Interaction logic for LoadingDialog
    /// </summary>
    public partial class LoadingDialog : UserControl
    {
        log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(LoadingDialog));
        MyAlert myAlert = new MyAlert();
        public LoadingDialog(Action act)
        {
            InitializeComponent();

            this.Loaded += (s, e) => 
            {
                Task.Run(() => 
                {
                    var result = true;
                    try
                    {
                        act();   
                    }
                    catch 
                    {
                        result = false;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DialogHost.CloseDialogCommand.Execute(result, e.Source as UserControl);
                    });
                });
            };
        }
    }
}
