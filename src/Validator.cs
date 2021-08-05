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

public class Validator
{
    private static HttpClient _httpClient = new HttpClient();
    private Contract _contract;
    private List<string> _ignoreFlags;


    public Validator(Contract contract, List<string> ignoreFlags, string bearerToken)
    {
        _contract = contract;
        _ignoreFlags = ignoreFlags;

        if (!String.IsNullOrEmpty(bearerToken))
        {
            Printer.PrintInfo($"adding bearer token to all requests (token: {bearerToken}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
    public async Task<bool> Validate()
    {
        var validationSucceeded = true;
        Printer.PrintInfo($"validating contract {_contract.Name}");
        foreach (var trans in _contract.Transactions)
        {

            using var outboundRequest = InitiateOutboundRequest(trans.Request);
            CopyHeadersAndContentFromRequest(trans.Request, outboundRequest);

            using var responseMessage = await _httpClient.SendAsync(outboundRequest);
            var jsonString = await responseMessage.Content.ReadAsStringAsync();
            try
            {
                var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, trans, (int)responseMessage.StatusCode, _ignoreFlags);
                if (isValid)
                    Printer.PrintOK($"✅ {trans.Request.Method} {trans.Request.Path}");
                else
                {
                    validationSucceeded = false;
                    errorMessages.ForEach(error => Printer.PrintErr($"❌ {trans.Request.Method} {trans.Request.Path} failed with error: {error}"));
                }
            }
            catch (Exception e)
            {
                validationSucceeded = false;
                Printer.PrintErr($"Failed to handle {trans.Request.Method} {trans.Request.Path}: {e.Message}");
            }
        }
        return validationSucceeded;
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