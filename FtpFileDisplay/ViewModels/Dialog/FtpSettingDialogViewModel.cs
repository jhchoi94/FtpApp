using System;
using System.Threading.Tasks;
using FluentFTP;
using FtpFileDisplay.Events;
using FtpFileDisplay.Models;
using FtpFileDisplay.Views;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace FtpFileDisplay.ViewModels
{
    public class FtpSettingDialogViewModel : BindableBase
    {
        log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(FtpSettingDialogViewModel));
        const string FtpSettingDlg = "FtpSettingDlg";
        IEventAggregator eventAggregator;
        MyYesNo myYesNo = new MyYesNo();
        MyAlert myAlert = new MyAlert();

        #region [Prop] FtpServerIp
        private string ftpServerIp;
        public string FtpServerIp
        {
            get { return ftpServerIp; }
            set { SetProperty(ref ftpServerIp, value); }
        }
        #endregion

        #region [Prop] FtpUserId
        private string ftpUserId;
        public string FtpUserId
        {
            get { return ftpUserId; }
            set { SetProperty(ref ftpUserId, value); }
        }
        #endregion

        #region [Prop] FtpPassword
        private string ftpPassword;
        public string FtpPassword
        {
            get { return ftpPassword; }
            set { SetProperty(ref ftpPassword, value); }
        }
        #endregion

        #region [Prop] FtpRootPath
        private string ftpRootPath;
        public string FtpRootPath
        {
            get { return ftpRootPath; }
            set { SetProperty(ref ftpRootPath, value); }
        }
        #endregion

        #region [Prop] LocalRootPath
        private string localRootPath;
        public string LocalRootPath
        {
            get { return localRootPath; }
            set { SetProperty(ref localRootPath, value); }
        }
        #endregion

        #region [Prop] DeleteOldDay
        private int deleteOldDay;
        public int DeleteOldDay
        {
            get { return deleteOldDay; }
            set { SetProperty(ref deleteOldDay, value); }
        }
        #endregion

        #region [Creator] FtpSettingDialogViewModel
        public FtpSettingDialogViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }
        #endregion

        #region [Cmd] LoadedCommand
        private DelegateCommand loadedCommand;
        public DelegateCommand LoadedCommand =>
            loadedCommand ?? (loadedCommand = new DelegateCommand(ExecuteLoadedCommand));

        void ExecuteLoadedCommand()
        {
            var appConfig = MainWindowViewModel.AppConfig;
            FtpServerIp = appConfig.FtpServerIp;
            FtpUserId = appConfig.FtpUserId;
            FtpPassword = appConfig.FtpPassword;
            FtpRootPath = appConfig.FtpRootPath;
            LocalRootPath = appConfig.LocalRootPath;
            DeleteOldDay = appConfig.DeleteOldDay;
        }
        #endregion

        #region [Cmd] CmdSave
        private DelegateCommand cmdSave;
        public DelegateCommand CmdSave =>
            cmdSave ?? (cmdSave = new DelegateCommand(ExecuteCmdSave));

        async void ExecuteCmdSave()
        {
            myYesNo.Text = "APP 설정 정보를 저장하시겠습니까?";
            var result = (bool)await DialogHost.Show(myYesNo, FtpSettingDlg);
            if (result)
            {
                // 접속 테스트
                try
                {
                    FtpClient client = new FtpClient(FtpServerIp, FtpUserId, FtpPassword);
                    client.ConnectTimeout = 1000;

                    
                    var loadingResult = (bool)await DialogHost.Show(new LoadingDialog(() =>
                    {
                        client.Connect();
                        client.Disconnect();
                    }), FtpSettingDlg);

                    if (loadingResult)
                    {
                        MainWindowViewModel.AppConfig.FtpServerIp = FtpServerIp;
                        MainWindowViewModel.AppConfig.FtpUserId = FtpUserId;
                        MainWindowViewModel.AppConfig.FtpPassword = FtpPassword;
                        MainWindowViewModel.AppConfig.FtpRootPath = FtpRootPath;
                        MainWindowViewModel.AppConfig.LocalRootPath = LocalRootPath;
                        MainWindowViewModel.AppConfig.DeleteOldDay = DeleteOldDay;
                        eventAggregator.GetEvent<FtpSettingChangeEvent>().Publish();

                        myAlert.Icon = PackIconKind.Check;
                        myAlert.Text = "APP 설정 정보를 저장하였습니다.";
                        await DialogHost.Show(myAlert, FtpSettingDlg);

                        DialogHost.CloseDialogCommand.Execute(true, null);
                    }
                    else
                    {
                        myAlert.Icon = PackIconKind.Error;
                        myAlert.Text = $"FTP 연결 실패";
                        await DialogHost.Show(myAlert, FtpSettingDlg);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.StackTrace);
                    myAlert.Icon = PackIconKind.Error;
                    myAlert.Text = $"설정 정보 저장 실패.{Environment.NewLine}{ex.Message}";
                    await DialogHost.Show(myAlert, FtpSettingDlg);
                }
            }
        }
        #endregion
    }
}
