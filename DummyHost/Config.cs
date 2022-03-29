using System.Net;
using PinFun.Core.Utils.CmdArgument;

namespace DummyHost
{
    public class Config : CommandArgument
    {
        [CommandArgumentElement("port", ArgumentParser = typeof(PortParser), DefaultValue = 80, Help = "该服务的工作端口",
            IsRequired = false)]
        public int Port { get; set; }

        [CommandArgumentElement("ip", ArgumentParser = typeof(IpAddressParser), Help = "该服务工作的ip地址",
            IsRequired = false)]
        public IPAddress IpAddress { get; set; }

        [CommandArgumentElement("ssl-pwd", Help = "SSL证书文件密码",
            IsRequired = false)]
        public string SslPassword { get; set; }

        [CommandArgumentElement("ssl-port", ArgumentParser = typeof(PortParser), DefaultValue = 443,
            Help = "SSL服务的工作端口",
            IsRequired = false)]
        public int SslPort { get; set; }

        public Config() : base("本地业务服务模拟器")
        {
        }
    }
}