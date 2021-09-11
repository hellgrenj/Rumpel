using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        var trans = _contract.Transactions.Find(t => t.Request.Method == method && t.Request.Path == path);
        if (trans == null)
        {
            Printer.PrintInfo($"no response found for {method} {path}");
            context.Response.StatusCode = 404;
            await context.Response.CompleteAsync();
        }
        else
        {

            context.Response.StatusCode = (int)trans.Response.StatusCode;
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
                    await Respond(context, trans, JsonSerializer.Serialize(requestBodyErrors));
                    return;
                }

            }
            Printer.PrintInfo($"returning pre-recorded response for {trans.Request.Method} {trans.Request.Path}");
            await Respond(context, trans, trans.Response.RawBody);
        }

    }
    private void AddHeaders(HttpContext context, Transaction trans)
    {
        foreach (var header in trans.Response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray<string>();
        }
        // removing hop-by-hop-headers https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#hbh
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
    private async Task Respond(HttpContext context, Transaction trans, string responseString)
    {
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseString));
        await memoryStream.CopyToAsync(context.Response.Body);
        await context.Response.CompleteAsync();
    }
}