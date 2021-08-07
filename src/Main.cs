using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Rumpel.Models;

if (args.Length == 0)
{
    Printer.PrintHelp();
    Environment.Exit(0);
}

switch (args[0])
{
    case "--help":
    case "-h": Printer.PrintHelp(); break;
    case "--version": Printer.PrintInfo("Rumpel v0.1.2"); break;
    case "--record-contract":
    case "-r": await RecordContract(args); break;
    case "--validate-contract":
    case "-v": await ValidateContract(args); break;
    case "--mock-provider":
    case "-m": await MockProvider(args); break;
    default: Printer.PrintErr($"unknown argument {args[0]}"); break;

}

async Task RecordContract(string[] args)
{
    if (args.Length < 3)
    {
        Printer.PrintErr("missing arguments, expecting: --record-contract|-r --contract-name=<name> --target-api=<url>");
        Environment.Exit(1);
    }

    var contractNamePrefix = "--contract-name=";
    var (contractName, contractNameExtracted) = TryExtractSetting(args, contractNamePrefix);
    if (!contractNameExtracted)
        ExitWithArgumentMissingOrInvalid(argumentName: "contract name", contractNamePrefix, expectedInput: "name");

    var targetApiPrefix = "--target-api=";
    var (targetApi, targetApiExtracted) = TryExtractSetting(args, targetApiPrefix);
    if (!targetApiExtracted)
        ExitWithArgumentMissingOrInvalid(argumentName: "target api", targetApiPrefix, expectedInput: "url");

    var contract = new Contract() { Name = contractName, URL = targetApi };
    var recorder = new Recorder(contract);
    await recorder.Record();
}
async Task ValidateContract(string[] args)
{
    if (args.Length < 2)
    {
        Printer.PrintErr("missing arguments, expecting: --validate-contract|-v --contract=<path> (ignore-flags) (--bearer-token=<token>)");
        Environment.Exit(1);
    }

    var contractPathPrefix = "--contract-path=";
    var (contractPath, contractPathExtracted) = TryExtractSetting(args, contractPathPrefix);
    if (!contractPathExtracted)
        ExitWithArgumentMissingOrInvalid(argumentName: "contract path", contractPathPrefix, expectedInput: "path");

    var (bearerToken, _) = TryExtractSetting(args, prefix: "--bearer-token=");

    Contract contract = null;
    try
    {
        contract = JsonSerializer.Deserialize<Contract>(File.ReadAllText(contractPath, Encoding.UTF8));
    }
    catch (Exception ex)
    {
        Printer.PrintErr($"could not parse the provided contract: \n {ex.Message}");
        Environment.Exit(1);
    }
    var ignoreFlags = ExtractIgnoreFlags(args);
    var validator = new Validator(contract, ignoreFlags, bearerToken);

    var validationSucceeded = await validator.Validate();
    if (validationSucceeded)
    {
        Printer.PrintOK($"\nContract test passed! (contract: {contract.Name})".ToUpper());
    }
    else
    {
        Printer.PrintErr($"\nContract test failed! (contract: {contract.Name})".ToUpper());
        Environment.Exit(1);
    }

}
async Task MockProvider(string[] args)
{
    if (args.Length < 2)
    {
        Printer.PrintErr("missing arguments, expecting: --mock-provider|-m --contract=<path>");
        Environment.Exit(1);
    }
    var contractPathPrefix = "--contract-path=";
    var (contractPath, contractPathExtracted) = TryExtractSetting(args, contractPathPrefix);
    if (!contractPathExtracted)
        ExitWithArgumentMissingOrInvalid(argumentName: "contract path", contractPathPrefix, expectedInput: "path");

    Contract contract = null;
    try
    {
        contract = JsonSerializer.Deserialize<Contract>(File.ReadAllText(contractPath, Encoding.UTF8));
    }
    catch (Exception ex)
    {
        Printer.PrintErr($"could not parse the provided contract: \n {ex.Message}");
        Environment.Exit(1);
    }

    var mocker = new Mocker(contract);
    await mocker.Run();

}
(string, bool) TryExtractSetting(string[] args, string prefix)
{
    string setting;
    bool extracted;
    var argument = args.Where(a => a.Contains(prefix)).FirstOrDefault();
    if (String.IsNullOrEmpty(argument) || argument.Length < prefix.Length + 1)
    {
        setting = String.Empty;
        extracted = false;
    }
    else
    {
        setting = argument.Split(prefix)[1];
        extracted = true;
    }

    return (setting, extracted);
}
void ExitWithArgumentMissingOrInvalid(string argumentName, string prefix, string expectedInput)
{
    Printer.PrintErr($" {argumentName} must be provided with the correct syntax {prefix}<{expectedInput}>");
    Environment.Exit(1);
}
List<string> ExtractIgnoreFlags(string[] args)
{
    var ignoreFlags = new List<string>();
    ignoreFlags.AddRange(args.Where(a => a.Contains("--ignore-")).ToList<string>());

    if (ignoreFlags.Count > 0)
    {
        Printer.PrintWarning("Running with the following ignore flags");
        ignoreFlags.ForEach(flag =>
        {
            if (IgnoreFlags.IsValid(flag))
                Printer.PrintWarning(flag);
            else
                Printer.PrintErr($"ignore flag {flag} is not valid");
        });
    }
    return ignoreFlags;
}

