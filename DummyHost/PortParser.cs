using System;
using PinFun.Core.Utils.CmdArgument;

namespace DummyHost
{
    public class PortParser : IArgumentParser
    {
        public object Parser(string input, Type targetType)
        {
            if (int.TryParse(input, out var p)) return p;
            return 8080;
        }
    }
}