using Config.Net;

namespace FtpFileDisplay.Interface
{
    public interface IAppConfig
    {

        [Option(Alias = "FtpServer.FtpServerIp")]
        string FtpServerIp { get; set; }

        [Option(Alias = "FtpServer.FtpUserId")]
        string FtpUserId { get; set; }

        [Option(Alias = "FtpServer.FtpPassword")]
        string FtpPassword { get; set; }
        [Option(Alias = "FtpServer.FtpRootPath")]
        string FtpRootPath { get; set; }





        [Option(Alias = "Local.LocalRootPath")]
        string LocalRootPath { get; set; }
        [Option(Alias = "Local.DeleteOldDay")]
        int DeleteOldDay { get; set; }
    }
}
