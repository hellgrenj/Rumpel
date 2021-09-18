using System;
using Rumpel.Models;

public static class Printer
{
    public static void PrintInfo(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void PrintOK(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
    public static void PrintErr(string err)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(err);
        Console.ResetColor();
    }
    public static void PrintWarning(string warning)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(warning);
        Console.ResetColor();
    }

    public static void PrintHelp()
    {
        var header = ".::Rumpel::.";
        var subHeader = "Simple, opinionated and automated consumer-driven contract testing for your JSON API's";
        var info = @"
HELP:

--help (or the shorthand -h): Displays this information.

--version: Prints the version

--record-contract (or the shorthand -r): This starts a new recording of a new contract.
The expected arguments are: --record-contract|-r --contract-name=<name> --target-api=<url>

--validate-contract (or the shorthand -v): This validates (tests) a contract.
The expected arguments are: --validate-contract|-v --contract=<path> (ignore-flags) (--bearer-token=<token>) (--base-url=<url>)
(ignore flags, bearer token and base url are optional)  

--mock-provider (or the shorthand -m): This mocks a provider based on a contract.
The expected arguments are: --mock-provider|m --contract=<path>
";
        var ignoreFlagsInfo = @"ignoreFlags = 

The validator can be told to ignore specific assertions. 
Example with ignore flags:
--validate-contract|-v --contract=<path> --ignore-assert-status-code

These are the available ignoreFlags:";



        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(header);
        Console.WriteLine(subHeader);
        Console.ResetColor();
        Console.WriteLine(info);
        Console.WriteLine(ignoreFlagsInfo);
        IgnoreFlags.ToList().ForEach(f => Console.WriteLine(f));
    }


}
