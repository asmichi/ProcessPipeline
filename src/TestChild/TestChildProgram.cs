// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;

namespace Asmichi.Utilities
{
    internal static class TestChildProgram
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Write("TestChild");
                return 0;
            }

            var command = args[0];
            switch (command)
            {
                case "ExitCode":
                    return CommandExitCode(args);
                case "EchoOutAndError":
                    return CommandEchoOutAndError();
                case "EchoBack":
                    return CommandEchoBack();
                case "Sleep":
                    return CommandSleep(args);
                default:
                    Console.WriteLine("Unknown command: {0}", command);
                    return 1;
            }
        }

        private static int CommandExitCode(string[] args)
        {
            return int.Parse(args[1]);
        }

        private static int CommandEchoOutAndError()
        {
            Console.Write("TestChild.Out");
            Console.Error.Write("TestChild.Error");
            return 0;
        }

        private static int CommandEchoBack()
        {
            var text = Console.In.ReadToEnd();
            Console.Write(text);

            return 0;
        }

        private static int CommandSleep(string[] args)
        {
            int duration = int.Parse(args[1]);
            Thread.Sleep(duration);
            return 0;
        }
    }
}
