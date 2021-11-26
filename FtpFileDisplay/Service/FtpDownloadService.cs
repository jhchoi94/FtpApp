using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentFTP;
using FtpFileDisplay.Events;
using FtpFileDisplay.Interface;
using FtpFileDisplay.Models;
using FtpFileDisplay.ViewModels;
using Prism.Events;

namespace FtpFileDisplay.Service
{
    public class FtpDownloadService
    {
        log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(FtpDownloadService));
        Queue<FtpFileWorkInfo> FtpDownWaitQue;
        Queue<FtpFileWorkInfo> FtpUploadWaitQue;
        Queue<FtpFileWorkInfo> FtpDeleteWaitQue;
        Action<FtpProgress> progress;

        FtpClient client;
        IEventAggregator EvtAggregator;

        IAppConfig AppConfig;

        #region [Creator] FtpDownloadService
        public FtpDownloadService(IEventAggregator eventAggregator)
        {
            AppConfig = MainWindowViewModel.AppConfig;
            FtpDownWaitQue = new Queue<FtpFileWorkInfo>();
            FtpUploadWaitQue = new Queue<FtpFileWorkInfo>();
            FtpDeleteWaitQue = new Queue<FtpFileWorkInfo>();

            EvtAggregator = eventAggregator;
            InitClient();

            // 저장할 폴더 확인
            if (!Directory.Exists(AppConfig.LocalRootPath))
                Directory.CreateDirectory(AppConfig.LocalRootPath);

            eventAggregator.GetEvent<FtpSettingChangeEvent>().Subscribe(() => 
            {
                //AppConfig = MainWindowViewModel.AppConfig;
                InitClient();
            });

            progress = new Action<FtpProgress>(p => 
            {
                EvtAggregator.GetEvent<FtpWorkingEvent>().Publish(p);
            });

            #region Old Delete
            Task.Run(async () =>
            {
                var deleteDate = DateTime.Now.AddDays(-1);          // 최초 실행시 오래된 파일을 삭제하기 위해서 하루를 뺌
                while (true)
                {
                    // 날짜가 바뀌면 오래된 파일 삭제
                    if (deleteDate.Day != DateTime.Now.Day)
                    {
                        OldFileDelete();
                        deleteDate = DateTime.Now;
                    }

                    await Task.Delay(100000);
                }
            });
            #endregion
        }
        #endregion

        #region [Method] InitClient
        void InitClient()
        {
            client = new FtpClient(AppConfig.FtpServerIp, AppConfig.FtpUserId, AppConfig.FtpPassword);
            client.DataConnectionType = FtpDataConnectionType.PASV;
            client.ConnectTimeout = 1000;
        }
        #endregion

        #region [Method] UploadFile
        void UploadFile()
        {
            try
            {
                client.Connect();

                while (FtpUploadWaitQue.Count != 0)
                {
                    var curQue = FtpUploadWaitQue.Peek();
                    var remoteUploadPath = $@"{curQue.RemotePath}\{Path.GetFileName(curQue.FullPath)}";

                    if (curQue.Type == FtpFileSystemObjectType.Directory)
                        client.UploadDirectory(curQue.FullPath, remoteUploadPath, FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite, progress: progress);
                    else
                        client.UploadFile(curQue.FullPath, remoteUploadPath, FtpRemoteExists.Overwrite, progress: progress);

                    FtpUploadWaitQue.Dequeue();
                }

                client.Disconnect();
                EvtAggregator.GetEvent<FtpUploadCompleteEvent>().Publish();
            }
            catch (Exception ex)
            {
                FtpUploadWaitQue.Dequeue();
                Logger.Error(ex.StackTrace);
            }
        } 
        #endregion

        #region [Method] DownloadFile
        void DownloadFile()
        {
            try
            {
                var downloadQueCnt = FtpDownWaitQue.Count;

                client.Connect();

                var localDownPath = string.Empty;
                FtpFileSystemObjectType type = FtpDownWaitQue.Count > 1 ? FtpFileSystemObjectType.Directory : FtpDownWaitQue.Peek().Type;           // 여러 파일을 다운로드 받았다면 해당 폴더를 Open하기 위함
                
                while (FtpDownWaitQue.Count != 0)
                {
                    var curQue = FtpDownWaitQue.Peek();
                    localDownPath = $@"{AppConfig.LocalRootPath}\{DateTime.Now.ToString("yyyyMMdd")}\{curQue.FullPath}";
                    var remotePath = $@"{curQue.FullPath}";
                    if (curQue.Type == FtpFileSystemObjectType.Directory)
                         client.DownloadDirectory(localDownPath, remotePath, FtpFolderSyncMode.Update, FtpLocalExists.Overwrite, progress: progress);
                    else
                         client.DownloadFile(localDownPath, remotePath, FtpLocalExists.Overwrite, FtpVerify.None, progress);
                    FtpDownWaitQue.Dequeue();
                }

                if (downloadQueCnt > 1)
                    localDownPath = Path.GetDirectoryName(localDownPath);
                
                EvtAggregator.GetEvent<FtpDownloadCompleteEvent>().Publish((localDownPath, type));
                 client.Disconnect();
                
            }
            catch (Exception ex)
            {
                FtpDownWaitQue.Dequeue();
                Logger.Error(ex.StackTrace);
            }
        }
        #endregion

        #region [Method] DeleteFile
        void DeleteFile()
        {
            try
            {
                client.Connect();
                while (FtpDeleteWaitQue.Count != 0)
                {
                    var curQue = FtpDeleteWaitQue.Peek();
                    if (curQue.Type == FtpFileSystemObjectType.Directory)
                         client.DeleteDirectory(curQue.FullPath, FtpListOption.AllFiles);
                    else
                         client.DeleteFile(curQue.FullPath);

                    FtpDeleteWaitQue.Dequeue();
                }
                client.Disconnect();

                EvtAggregator.GetEvent<FtpDeleteCompleteEvent>().Publish();
            }
            catch (Exception ex)
            {
                FtpDeleteWaitQue.Dequeue();
                Logger.Error(ex.StackTrace);
            }
        } 
        #endregion

        #region [Method] GetFiles
        public async Task<List<FtpListItem>> GetFiles(string path = "")
        {
            var findPath = string.IsNullOrEmpty(path) ? AppConfig.FtpRootPath : path;
            client.Connect();
            var ftpListItems = await client.GetListingAsync(findPath);
            client.Disconnect();

            return ftpListItems.ToList();
        }
        #endregion

        #region [Method] FtpDownloadQueAdd
        public void FtpDownloadWork(List<FtpFileWorkInfo> workInfos)
        {
            workInfos.ForEach(g => 
            {
                FtpDownWaitQue.Enqueue(g);
            });

            Task.Run(DownloadFile);
        }
        #endregion

        #region [Method] FtpUploadQueAdd
        public void FtpUploadWork(List<FtpFileWorkInfo> workInfos)
        {
            workInfos.ForEach(g => 
            {
                FtpUploadWaitQue.Enqueue(g);
            });

            Task.Run(UploadFile);
        }
        #endregion

        #region [Method] DeleteFile
        public void FtpDeleteWork(List<FtpFileWorkInfo> workInfos)
        {
            workInfos.ForEach(g =>
            {
                FtpDeleteWaitQue.Enqueue(g);
            });

            Task.Run(DeleteFile);
        }
        #endregion

        #region [Method] FtpRenameFile
        public async Task FtpRenameFile(string oldName, string newName)
        {
            await client.RenameAsync(oldName, newName);
        } 
        #endregion

        #region [Method] NewFolder
        public async Task NewFolder(string folderName)
        {
            client.Connect();
            await client.CreateDirectoryAsync(folderName);
            client.Disconnect();
        }
        #endregion

        #region [Method] OldFileDelete
        /// <summary>
        /// 설정된 일 수 이상된 파일은 자동삭제
        /// </summary>
        public void OldFileDelete()
        {
            DirectoryInfo di = new DirectoryInfo(AppConfig.LocalRootPath);

            di.GetDirectories().ToList().ForEach(g =>
            {
                var totalDays = (DateTime.Now - g.CreationTime).TotalDays;
                if (totalDays >= AppConfig.DeleteOldDay)
                {
                    try
                    {
                        Directory.Delete(g.FullName, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.StackTrace);
                    }
                }
            });
        }
        #endregion
    }
}
