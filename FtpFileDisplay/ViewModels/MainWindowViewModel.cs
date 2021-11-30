using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Config.Net;
using FluentFTP;
using FtpFileDisplay.Events;
using FtpFileDisplay.Interface;
using FtpFileDisplay.Models;
using FtpFileDisplay.Service;
using GongSolutions.Wpf.DragDrop;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace FtpFileDisplay.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDropTarget
    {
        log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(MainWindowViewModel));
        const string MainWindowDlg = "MainWindowDlg";
        FtpService FtpDownService;
        IEventAggregator eventAggregator;
        public static IAppConfig AppConfig;
        MyAlert myAlert = new MyAlert();
        MyYesNo myYesNo = new MyYesNo();
        MetroWindow metro;

        MetroDialogSettings metroDialog = new MetroDialogSettings
        {
            DialogTitleFontSize = 40,
            DialogMessageFontSize = 25,
            AffirmativeButtonText = "확인",
            NegativeButtonText = "취소",
        };

        #region [Prop] PathHist
        // 파일경로 목록
        public ObservableCollection<DiretoryPathModel> PathHist { get; set; }
        #endregion

        #region [Prop] FtpWorkingProgress
        // FTP 작업 진행상태
        private FtpProgress ftpWorkingProgress;
        public FtpProgress FtpWorkingProgress
        {
            get { return ftpWorkingProgress; }
            set { SetProperty(ref ftpWorkingProgress, value); }
        }
        #endregion

        #region [Prop] SelectMouseClickItem
        // 파일목록에서 클릭한 아이템
        private FtpFileModel selectMouseClickItem;
        public FtpFileModel SelectMouseClickItem
        {
            get { return selectMouseClickItem; }
            set { SetProperty(ref selectMouseClickItem, value); }
        } 
        #endregion

        #region [Prop] SelectedFile
        // 파일목록에서 선택된 아이템(SingleSelect)
        private FtpFileModel selectedFile;
        public FtpFileModel SelectedFile
        {
            get { return selectedFile; }
            set { SetProperty(ref selectedFile, value);}
        }
        #endregion

        #region [Prop] SelectedFiles
        // 파일목록에서 선택된 아이템 리스트(MultiSelect)
        private IList<object> selectedFiles;
        public IList<object> SelectedFiles
        {
            get { return selectedFiles; }
            set { SetProperty(ref selectedFiles, value); }
        } 
        #endregion

        #region [Prop] FileList
        // 현재 Path에 있는 파일 목록
        private List<FtpFileModel> fileList;
        public List<FtpFileModel> FileList
        {
            get { return fileList; }
            set { SetProperty(ref fileList, value); }
        }
        #endregion

        #region [Prop] IsFtpWorking
        // Ftp 작업 상태
        private bool isFtpWorking;
        public bool IsFtpWorking
        {
            get { return isFtpWorking; }
            set { SetProperty(ref isFtpWorking, value); }
        }
        #endregion


        #region [Creator] MainWindowViewModel
        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            // Log4Net 설정
            log4net.Config.XmlConfigurator.Configure();
            AppConfig = new ConfigurationBuilder<IAppConfig>()
                .UseIniFile("FtpSetting.ini")
                .Build();


            FileList = new List<FtpFileModel>();
            this.eventAggregator = eventAggregator;
            FtpDownService = new FtpService(eventAggregator);
        }
        #endregion

        #region [Loaded] LoadedCommand
        private DelegateCommand loadedCommand;
        public DelegateCommand LoadedCommand =>
            loadedCommand ?? (loadedCommand = new DelegateCommand(ExecuteLoadedCommand));

        void ExecuteLoadedCommand()
        {
            metro = (Application.Current.MainWindow as MetroWindow);
            PathHist = new ObservableCollection<DiretoryPathModel>();
            PathHistInit();

            eventAggregator.GetEvent<FtpWorkingEvent>().Subscribe(progress => 
            {
                FtpWorkingProgress = progress;
            }, ThreadOption.UIThread);

            eventAggregator.GetEvent<FtpDownloadCompleteEvent>().Subscribe(downInfo =>
            {
                IsFtpWorking = false;
                FtpWorkingProgress = null;

                // Ftp Download 작업이 완료된 파일의 타입 구분
                if (downInfo.type == FtpFileSystemObjectType.Directory)
                {
                    // 폴더인 경우
                    if (Directory.Exists(downInfo.path))
                        Process.Start(downInfo.path);
                }
                else
                {
                    // 파일인 경우
                    if (File.Exists(downInfo.path))
                    {
                        var extension = Path.GetExtension(downInfo.path);
                        // Excel이면 실행
                        if (extension == ".xlsx")
                            Process.Start(downInfo.path);
                        else
                            Process.Start(Path.GetDirectoryName(downInfo.path));
                    }
                }
            });


            eventAggregator.GetEvent<FtpUploadCompleteEvent>().Subscribe(() => 
            {
                IsFtpWorking = false;
                FtpWorkingProgress = null;

                ExecuteCmdRefresh();
            });

            eventAggregator.GetEvent<FtpDeleteCompleteEvent>().Subscribe(() => 
            {
                IsFtpWorking = false;

                ExecuteCmdRefresh();
            });
        }
        #endregion

        #region [Cmd] ContentRenderedCommand
        private DelegateCommand contentRenderedCommand;
        public DelegateCommand ContentRenderedCommand =>
            contentRenderedCommand ?? (contentRenderedCommand = new DelegateCommand(ExecuteContentRenderedCommand));

        void ExecuteContentRenderedCommand()
        {
            FileListRefresh();
        } 
        #endregion

        #region [Cmd] CmdPathOpen
        private DelegateCommand<DiretoryPathModel> cmdPathOpen;
        public DelegateCommand<DiretoryPathModel> CmdPathOpen =>
            cmdPathOpen ?? (cmdPathOpen = new DelegateCommand<DiretoryPathModel>(ExecuteCmdFolderOpen));

        void ExecuteCmdFolderOpen(DiretoryPathModel directory)
        {
            for (int i = PathHist.Count; i > directory.Index; i--)
            {
                PathHist.RemoveAt(i - 1);
            }
            FileListRefresh(directory.FullName, false);
            
        }
        #endregion

        #region [Cmd] CmdEditCommand
        private DelegateCommand cmdEditCommand;
        public DelegateCommand CmdEditCommand =>
            cmdEditCommand ?? (cmdEditCommand = new DelegateCommand(ExecuteCmdEditCommand));

        async void ExecuteCmdEditCommand()
        {
            if (SelectMouseClickItem == null) return;

            metroDialog.DefaultText = SelectMouseClickItem.Name;
            var newName = await metro.ShowInputAsync($"이름 바꾸기", "새로운 이름을 입력해주세요.", metroDialog);

            // null은 취소를 누름
            if (!string.IsNullOrEmpty(newName))
            {
                newName = $@"{Path.GetDirectoryName(SelectMouseClickItem.FullName)}\{ReplaceFileName(newName)}";

                await FtpDownService.FtpRenameFile(SelectMouseClickItem.FullName, newName);
                var curPath = PathHist.LastOrDefault().FullName;
                FileListRefresh(curPath);
            
            }
        } 
        #endregion

        #region [Cmd] CmdSelectFile
        private DelegateCommand cmdSelectfile;
        public DelegateCommand CmdSelectFile =>
            cmdSelectfile ?? (cmdSelectfile = new DelegateCommand(ExecuteCmdSelectfile));

        void ExecuteCmdSelectfile()
        {
            if (SelectedFile == null) return;

            if (SelectedFile.Type == FtpFileSystemObjectType.Directory)
            {
                FileListRefresh(SelectedFile.FullName, false);
            }
            else if (SelectedFile.Type == FtpFileSystemObjectType.File)
            {
                IsFtpWorking = true;
                
                FtpDownService.FtpDownloadWork(new List<FtpFileWorkInfo> 
                {
                    new FtpFileWorkInfo{FullPath = SelectedFile.FullName, Type = FtpFileSystemObjectType.File }
                });
            }
        }
        #endregion

        #region [Cmd] CmdFileDownload
        private DelegateCommand cmdFileDownload;
        public DelegateCommand CmdFileDownload =>
            cmdFileDownload ?? (cmdFileDownload = new DelegateCommand(ExecuteCmdFileDownload));

        void ExecuteCmdFileDownload()
        {
            if (SelectedFiles == null || SelectedFiles.Count == 0) return;

            IsFtpWorking = true;

            var workInfos = SelectedFiles.Select(g =>
            {
                var fileModel = g as FtpFileModel;
                return new FtpFileWorkInfo { FullPath = fileModel.FullName, Type = fileModel.Type };
            }).ToList();

            FtpDownService.FtpDownloadWork(workInfos);
        }
        #endregion

        #region [Cmd] CmdFileUpload
        private DelegateCommand cmdFileUpload;
        public DelegateCommand CmdFileUpload =>
            cmdFileUpload ?? (cmdFileUpload = new DelegateCommand(ExecuteCmdFileUpload));

        void ExecuteCmdFileUpload()
        {
            CommonOpenFileDialog fileDialog = new CommonOpenFileDialog();
            fileDialog.Title = "FTP 업로드";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                IsFtpWorking = true;
                var curPath = PathHist.LastOrDefault().FullName;

                var ftpFileWorkList = fileDialog.FileNames.Select(g =>
                    new FtpFileWorkInfo { FullPath = g, RemotePath = curPath, Type = FtpFileSystemObjectType.File }
                ).ToList();
                
                FtpDownService.FtpUploadWork(ftpFileWorkList);
            }
        } 
        #endregion

        #region [Cmd] CmdDelete
        private DelegateCommand cmdDelete;
        public DelegateCommand CmdDelete =>
            cmdDelete ?? (cmdDelete = new DelegateCommand(ExecuteCmdDelete));

        async void ExecuteCmdDelete()
        {
            if (SelectedFiles != null)
            {
                myYesNo.Text = "정말 삭제하시겠습니까?";
                var result = (bool)await DialogHost.Show(myYesNo, MainWindowDlg);
                if (result)
                {
                    IsFtpWorking = true;
                    var workInfos = SelectedFiles.Select(g =>
                    {
                        var fileModel = g as FtpFileModel;
                        return new FtpFileWorkInfo { FullPath = fileModel.FullName, Type = fileModel.Type };
                    }).ToList();

                    FtpDownService.FtpDeleteWork(workInfos);
                }
            }
        }
        #endregion

        #region [Cmd] CmdRefresh
        private DelegateCommand cmdRefresh;
        public DelegateCommand CmdRefresh =>
            cmdRefresh ?? (cmdRefresh = new DelegateCommand(ExecuteCmdRefresh));

        /// <summary>
        /// 파일목록 새로고침 Command
        /// </summary>
        void ExecuteCmdRefresh()
        {
            var curPath = PathHist.LastOrDefault().FullName;
            FileListRefresh(curPath);
        }
        #endregion

        #region [Cmd] CmdSettingDlg
        private DelegateCommand cmdSettingDlg;
        public DelegateCommand CmdSettingDlg =>
            cmdSettingDlg ?? (cmdSettingDlg = new DelegateCommand(ExecuteCmdSettingDlg));

        async void ExecuteCmdSettingDlg()
        {
            var result = (bool)await DialogHost.Show(new FtpSettingDialogViewModel(eventAggregator), MainWindowDlg);
            if (result)
            {
                PathHistInit();
                FileListRefresh();
            }
        }
        #endregion

        #region [Cmd] CmdNewFolder
        private DelegateCommand cmdNewFolder;
        public DelegateCommand CmdNewFolder =>
            cmdNewFolder ?? (cmdNewFolder = new DelegateCommand(ExecuteCmdNewFolder));

        async void ExecuteCmdNewFolder()
        {
            var newName = await metro.ShowInputAsync($"새 폴더", "새로운 이름을 입력해주세요.", metroDialog);

            // null은 취소를 누름
            if (!string.IsNullOrEmpty(newName))
            {
                newName = ReplaceFileName(newName);

                if (FileList.Count(g => g.Type == FtpFileSystemObjectType.Directory && g.Name.ToUpper() == newName.ToUpper()) != 0)
                {
                    myAlert.Icon = PackIconKind.Error;
                    myAlert.Text = $"'{newName}' 폴더명이 이미 존재합니다.";
                    await DialogHost.Show(myAlert, MainWindowDlg);
                    return;
                }

                var curPath = PathHist.LastOrDefault().FullName;
                if (!string.IsNullOrEmpty(curPath))
                    newName = $@"{curPath}\{newName}";

                await FtpDownService.NewFolder(newName);
                FileListRefresh(curPath);
            }
        }
        #endregion

        
        #region [Method] FileListRefresh
        /// <summary>
        /// 파일 목록 새로고침
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="isRefresh"></param>
        public async void FileListRefresh(string dirPath = "", bool isRefresh = true)
        {
            try
            {                
                var result = await FtpDownService.GetFiles(dirPath);
                if (result != null)
                {
                    FileList = result.Select(g => new FtpFileModel
                    {
                        Type = g.Type,
                        Name = g.Name,
                        FullName = g.FullName,
                        Size = g.Type != FtpFileSystemObjectType.Directory ? FormatBytes(g.Size) : string.Empty,
                        ModifiedDate = g.Modified,
                        Extension = g.Type != FtpFileSystemObjectType.Directory ? Path.GetExtension(g.Name) : string.Empty,
                    }).ToList();

                    if (!isRefresh)
                    {
                        var pathName = dirPath.Split('/').LastOrDefault();
                        PathHist.Add(new DiretoryPathModel { FullName = dirPath, Name = pathName, Index = PathHist.Count });
                    }

                    SelectedFile = null;
                    SelectedFiles = null;
                    SelectMouseClickItem = null;
                    RaisePropertyChanged("PathHist");
                }
                else
                {
                    myAlert.Icon = PackIconKind.Error;
                    myAlert.Text = "FTP 연결 실패.";
                    await DialogHost.Show(myAlert, MainWindowDlg);
                }
            }
            catch (Exception ex)
            {
                myAlert.Icon = PackIconKind.Error;
                myAlert.Text = ex.Message;
                await DialogHost.Show(myAlert, MainWindowDlg);
            }
           
        }
        #endregion

        #region [Method] PathHistInit
        /// <summary>
        /// 파일경로 목록 초기화
        /// </summary>
        private void PathHistInit()
        {
            PathHist.Clear();
            PathHist.Add(new DiretoryPathModel { Name = "", FullName = "", Index = 0 });
        }
        #endregion

        #region [Method] ReplaceFileName
        /// <summary>
        /// 이름변경시 이름에 사용할 수 없는 문자 제외
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ReplaceFileName(string s)
        {
            Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()))));
            s = regex.Replace(s, "");

            return s;
        }
        #endregion


        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            // 파일을 Over 했을때 파일목록
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();

            // DragEffect 변경
            dropInfo.Effects = dragFileList.Any(item =>
            {
                return item != Path.GetPathRoot(item);
            }) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            // 파일을 Drop 했을때 파일목록
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();

            // DragEffect 변경
            dropInfo.Effects = dragFileList.Any(item =>
            {
                return item != Path.GetPathRoot(item);
            }) ? DragDropEffects.Copy : DragDropEffects.None;

            var curPath = PathHist.LastOrDefault().FullName;

            IsFtpWorking = true;

            // Drop 파일 List 
            var ftpFileWorkList = dragFileList.Select(g =>
            {
                FileAttributes chkAtt = File.GetAttributes(g);
                var type = (chkAtt & FileAttributes.Directory) == FileAttributes.Directory ? FtpFileSystemObjectType.Directory : FtpFileSystemObjectType.File;
                return new FtpFileWorkInfo { FullPath = g, RemotePath = curPath, Type = type };
            }).ToList();

            // Ftp Upload
            FtpDownService.FtpUploadWork(ftpFileWorkList);
        }

        #region [Method] FormatBytes
        public string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return string.Empty;
        }
        #endregion
    }
}
