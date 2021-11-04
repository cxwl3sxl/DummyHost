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

        public Config() : base("本地业务服务模拟器")
        {
        }
    }
}