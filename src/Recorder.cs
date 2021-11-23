using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rumpel.Models;

public class Recorder
{
    private static HttpClient _httpClient = new HttpClient();
    private Contract _contract;
    public Recorder(Contract contract) => _contract = contract;

    public async Task Record()
    {
        Printer.PrintInfo($"recording a new contract with name {_contract.Name}, target url {_contract.URL}");
        await Server.Start(ProxyHandler);
    }
    async Task ProxyHandler(HttpContext context)
    {
        
        var trans = await InitiateNewTransaction(context);

        using var outboundRequest = InitiateOutboundRequest(trans);
        CopyHeadersAndContentFromRequest(context, outboundRequest, trans);

        using var responseMessage = await _httpClient.SendAsync(outboundRequest);
        await ReadResponse(trans, responseMessage);
        await SaveTransaction(trans);
        await CopyHeadersAndContentFromResponse(context, responseMessage);

        await context.Response.CompleteAsync();
    }

    private async Task<Transaction> InitiateNewTransaction(HttpContext context)
    {

        var path = context.Request.Path.ToString();
        if (context.Request.QueryString.HasValue)
            path += context.Request.QueryString.Value;

        return new()
        {
            Request = new()
            {
                Path = path,
                Method = context.Request.Method.ToString(),
                RawBody = await new StreamContent(context.Request.Body).ReadAsStringAsync()
            }
        };
    }
    private HttpRequestMessage InitiateOutboundRequest(Transaction trans)
    {
        var outboundRequest = new HttpRequestMessage();
        trans.Request.AddHeaders(outboundRequest);
        outboundRequest.RequestUri = new Uri(_contract.URL + trans.Request.Path);
        outboundRequest.Method = new HttpMethod(trans.Request.Method);
        return outboundRequest;
    }
    private void CopyHeadersAndContentFromRequest(HttpContext context, HttpRequestMessage requestMessage, Transaction trans)
    {
        var requestMethod = context.Request.Method;

        if (HttpMethods.IsPost(requestMethod) ||
               HttpMethods.IsPut(requestMethod) ||
               HttpMethods.IsPatch(requestMethod))
        {
            context.Request.Body.Position = 0;
            var streamContent = new StreamContent(context.Request.Body);
            requestMessage.Content = streamContent;
        }

        foreach (var header in context.Request.Headers)
        {

            if (header.Key.ToLower() == "host")
                continue;

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray<string>()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray<string>());
            }
            if (header.Key == "Authorization") // we are not recording bearer tokens, we pass them in when verifying..
                continue;

            trans.Request.Headers.Add(header.Key, header.Value);

        }
        // finally set our target host
        requestMessage.Headers.Host = new Uri(_contract.URL + trans.Request.Path).Host;
    }

    private async Task ReadResponse(Transaction trans, HttpResponseMessage responseMessage)
    {
        var body = await responseMessage.Content.ReadAsStringAsync();
        trans.Response = new Response() { StatusCode = (int)responseMessage.StatusCode, RawBody = body };
        trans.Response.AddHeaders(responseMessage);
    }

    private async Task SaveTransaction(Transaction trans)
    {
        _contract.Transactions.Add(trans);
        string jsonString = JsonSerializer.Serialize(_contract, new JsonSerializerOptions() { WriteIndented = true });
        var filePath = $"./contracts/{_contract.Name}.rumpel.contract.json";
        var file = new System.IO.FileInfo(filePath);
        file.Directory.Create(); // if it doesnt exist.. else this will be a NoOp
        await File.WriteAllTextAsync(filePath, jsonString, Encoding.UTF8);
        Printer.PrintInfo($"saved transaction for request {trans.Request.Method} to {trans.Request.Path}");

    }
    private async Task CopyHeadersAndContentFromResponse(HttpContext context, HttpResponseMessage responseMessage)
    {
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        // removing hop-by-hop-headers https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#hbh
        context.Response.Headers.Remove("transfer-encoding");


        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }


}
