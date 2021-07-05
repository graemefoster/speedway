using System.CommandLine;

namespace Speedway.Cli
{
    public static class CommandFluentEx
    {
        public static Command AddArgumentEx(this Command cmd, string arg)
        {
            cmd.AddArgument(new Argument(arg));
            return cmd;
        }
    }
}