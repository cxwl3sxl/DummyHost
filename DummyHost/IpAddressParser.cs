using System;
using System.Net;
using PinFun.Core.Utils.CmdArgument;

namespace DummyHost
{
    public class IpAddressParser : IArgumentParser
    {
        public object Parser(string input, Type targetType)
        {
            if (IPAddress.TryParse(input, out var ip)) return ip;
            return IPAddress.Any;
        }
    }
}