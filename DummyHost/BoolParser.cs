using System;
using PinFun.Core.Utils.CmdArgument;

namespace DummyHost;

public class BoolParser : IArgumentParser
{
    public object Parser(string input, Type targetType)
    {
        return string.Equals(input, "y", StringComparison.OrdinalIgnoreCase);
    }
}