using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rumpel.Models;

public class Verifier
{
    private static HttpClient _httpClient = new HttpClient();
    private Contract _contract;
    private List<string> _ignoreFlags;


    public Verifier(Contract contract, List<string> ignoreFlags, string bearerToken, string url)
    {
        _contract = contract;
        _ignoreFlags = ignoreFlags;

        if (!String.IsNullOrEmpty(bearerToken))
        {
            Printer.PrintInfo($"adding bearer token to all requests (token: {bearerToken}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
        if (!String.IsNullOrEmpty(url))
        {
            Printer.PrintInfo($"overriding recorded url {contract.URL} with {url}");
            contract.URL = url;
        }
    }
    public async Task<bool> Verify()
    {
        var verificationSucceeded = true;
        Printer.PrintInfo($"verifying contract {_contract.Name}");
        foreach (var trans in _contract.Transactions)
        {

            using var outboundRequest = InitiateOutboundRequest(trans.Request);
            CopyHeadersAndContentFromRequest(trans.Request, outboundRequest);
            using var responseMessage = await _httpClient.SendAsync(outboundRequest);
            try
            {
                var (isValid, errorMessages) = await ValidateResponse(responseMessage, trans, _ignoreFlags);
                if (isValid)
                    Printer.PrintOK($"✅ {trans.Request.Method} {trans.Request.Path}");
                else
                {
                    verificationSucceeded = false;
                    errorMessages.ForEach(error => Printer.PrintErr($"❌ {trans.Request.Method} {trans.Request.Path} failed with error: {error}"));
                }
            }
            catch (Exception e)
            {
                verificationSucceeded = false;
                Printer.PrintErr($"Failed to handle {trans.Request.Method} {trans.Request.Path}: {e.Message}");
            }
        }
        return verificationSucceeded;
    }
    private async Task<(bool, List<string>)> ValidateResponse(HttpResponseMessage responseMessage, Transaction trans, List<string> ignoreFlags)
    {
        var isValid = true;
        var errorMessages = new List<string>();
        var jsonString = await responseMessage.Content.ReadAsStringAsync();
        var responseStatusCode = (int)responseMessage.StatusCode;
        if (trans.Response.StatusCode != responseStatusCode && !ignoreFlags.Contains(IgnoreFlags.IgnoreAssertStatusCode))
        {
            isValid = false;
            errorMessages.Add($@"request {trans.Request.Method} {trans.Request.Path} 
            received status code {responseStatusCode}
            but expected status code { trans.Response.StatusCode}
            ");
        }
        var (passesSchemaValidation, schemaErrorMessages) = Interpreter.InferSchemaAndValidate(jsonString, trans.Response.RawBody, ignoreFlags, trans.Customizations);
        if (!passesSchemaValidation)
        {
            isValid = false;
            errorMessages.AddRange(schemaErrorMessages);
        }
        return (isValid, errorMessages);

    }
    private HttpRequestMessage InitiateOutboundRequest(Request rumpelReq)
    {
        var outboundRequest = new HttpRequestMessage();
        outboundRequest.RequestUri = new Uri(_contract.URL + rumpelReq.Path);
        outboundRequest.Method = new HttpMethod(rumpelReq.Method);
        return outboundRequest;
    }
    private void CopyHeadersAndContentFromRequest(Request rumpelReq, HttpRequestMessage requestMessage)
    {

        var requestMethod = rumpelReq.Method;
        if (HttpMethods.IsPost(requestMethod) ||
                HttpMethods.IsPut(requestMethod) ||
                HttpMethods.IsPatch(requestMethod))
        {
            var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(rumpelReq.RawBody)));
            requestMessage.Content = streamContent;
        }
        foreach (var header in rumpelReq.Headers)
        {
            if (header.Key.ToLower() == "host")
                continue;

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray<string>()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray<string>());
            }
        }
        // finally set our target host
        requestMessage.Headers.Host = new Uri(_contract.URL + rumpelReq.Path).Host;
    }


}