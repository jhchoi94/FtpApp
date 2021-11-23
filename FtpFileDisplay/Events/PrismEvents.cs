using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using Prism.Events;

namespace FtpFileDisplay.Events
{
    /// <summary>
    /// FTP로 파일이 Down, Upload 되는 상황을 처리
    /// </summary>
    public class FtpWorkingEvent : PubSubEvent<FtpProgress> { }


    /// <summary>
    /// Application에서 발생하는 메제지 처리
    /// </summary>
    public class AppMsgEvent : PubSubEvent<string> { }


    /// <summary>
    /// FTP로 파일 DownLoad 완료 되었을때 처리
    /// </summary>
    public class FtpDownloadCompleteEvent : PubSubEvent<(string path, FtpFileSystemObjectType type)> { }


    /// <summary>
    /// FTP로 파일 Upload 완료 되었을때 처리
    /// </summary>
    public class FtpUploadCompleteEvent : PubSubEvent { }


    /// <summary>
    /// FTP로 파일 Delete 완료 되었을때 처리
    /// </summary>
    public class FtpDeleteCompleteEvent : PubSubEvent { }



    /// <summary>
    /// FTP로 설정변경
    /// </summary>
    public class FtpSettingChangeEvent : PubSubEvent { }
}
