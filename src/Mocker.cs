using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rumpel.Models;

public class Mocker
{
    private Contract _contract;
    public Mocker(Contract contract) => _contract = contract;

    public async Task Run()
    {
        Printer.PrintInfo($"mocking provider for contract {_contract.Name}");
        await Server.Start(MockRequestHandler);
    }

    async Task MockRequestHandler(HttpContext context)
    {
        var method = context.Request.Method.ToString();
        var path = context.Request.Path.ToString();
        if (context.Request.QueryString.HasValue)
            path += context.Request.QueryString.Value;

        var trans = _contract.Transactions.Find(t => t.Request.Method == method && t.Request.Path == path);
        if (trans == null)
        {
            Printer.PrintInfo($"no response found for {method} {path}");
            context.Response.StatusCode = 404;
            await context.Response.CompleteAsync();
        }
        else
        {
            context.Response.StatusCode = GetStatusCode(trans);
            if (context.Response.StatusCode == 500)
                await Respond(context, String.Empty, trans.SimulatedConditions);

            AddHeaders(context, trans);
            if (HttpMethods.IsPost(trans.Request.Method) ||
                HttpMethods.IsPut(trans.Request.Method) ||
                HttpMethods.IsPatch(trans.Request.Method))
            {
                var (requestBodyOk, requestBodyErrors) = await ValidateRequestBody(context, trans);
                if (!requestBodyOk)
                {
                    context.Response.StatusCode = 400;
                    Printer.PrintInfo($"returning bad request with validation errors for {trans.Request.Method} {trans.Request.Path}");
                    await Respond(context, JsonSerializer.Serialize(requestBodyErrors), trans.SimulatedConditions);
                    return;
                }
            }
            Printer.PrintInfo($"returning pre-recorded response for {trans.Request.Method} {trans.Request.Path}");
            await Respond(context, trans.Response.RawBody, trans.SimulatedConditions);
        }

    }
    private int GetStatusCode(Transaction trans)
    {
        var defaultStatusCode = trans.Response.StatusCode;
        if (trans.SimulatedConditions is null)
            return defaultStatusCode;

        var sometimes500 = trans.SimulatedConditions.Find(sc => sc.Type == SimulatedConditionTypes.Sometimes500);
        if (sometimes500 is not null)
        {
    
            try
            {
                var percentage = Int32.Parse(sometimes500.Value);
                Printer.PrintInfo($"{percentage}% chance the recorded status code will be replaced with 500");
                var random = new Random();
                var randomNumber = random.Next(101);
                if (randomNumber <= percentage)
                {
                    Printer.PrintInfo("simulating a 500");
                    return 500;
                }
            }
            catch
            {
                Printer.PrintErr("could not parse percentage from Value for Sometimes500");
            }
        }
        return defaultStatusCode;

    }
    private void AddHeaders(HttpContext context, Transaction trans)
    {
        foreach (var header in trans.Response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray<string>();
        }
        context.Response.Headers.Remove("transfer-encoding");
    }
    private async Task<(bool, List<string>)> ValidateRequestBody(HttpContext context, Transaction trans)
    {
        var isValid = true;
        var errorMessages = new List<string>();

        context.Request.Body.Position = 0;
        var streamContent = new StreamContent(context.Request.Body);
        var requestBodyAsString = await streamContent.ReadAsStringAsync();
        var (requestOk, requestErrors) = Interpreter.InferSchemaAndValidate(requestBodyAsString, trans.Request.RawBody, new List<string>() { IgnoreFlags.IgnoreAssertArrayLength }, new());
        if (!requestOk)
        {
            isValid = false;
            errorMessages.AddRange(requestErrors);
        }

        return (isValid, errorMessages);
    }
    private async Task Respond(HttpContext context, string responseString, List<SimulatedCondition> simulatedConditions)
    {
        RunAnySimulatedDelays(simulatedConditions);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseString));
        await memoryStream.CopyToAsync(context.Response.Body);
        await context.Response.CompleteAsync();
    }
    private void RunAnySimulatedDelays(List<SimulatedCondition> simulatedConditions)
    {
        if (simulatedConditions is null)
            return;

        var fixedDelay = simulatedConditions.Find(sc => sc.Type == SimulatedConditionTypes.FixedDelay);
        if (fixedDelay is not null)
        {
            try
            {
                var delayInMilliseconds = Int32.Parse(fixedDelay.Value);
                Printer.PrintInfo($"simulating a fixed delay of {delayInMilliseconds} milliseconds");
                Thread.Sleep(delayInMilliseconds);
            }
            catch
            {
                Printer.PrintErr($"could not parse delay in Value for FixedDelay");
            }
        }
        var randomDelay = simulatedConditions.Find(sc => sc.Type == SimulatedConditionTypes.RandomDelay);
        if (randomDelay is not null)
        {
            try
            {
                var split = randomDelay.Value.Split("-");
                var minDelay = Int32.Parse(split[0]);
                var maxDelay = Int32.Parse(split[1]);
                var random = new Random();
                var delayInMilliseconds = random.Next(minDelay, maxDelay);
                Printer.PrintInfo($"simulating a random delay of {delayInMilliseconds} milliseconds");
                Thread.Sleep(delayInMilliseconds);
            }
            catch
            {
                Printer.PrintErr($"could not parse delay min-max range in Value for RandomDelay. Expecting min-max in milliseconds, e.g 100-2000");
            }

        }
    }
}