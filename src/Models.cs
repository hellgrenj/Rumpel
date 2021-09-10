using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

namespace Rumpel.Models
{
    public static class IgnoreFlags
    {
        public static string IgnoreAssertStatusCode = "--ignore-assert-status-code";
        public static string IgnoreAssertArrayLength = "--ignore-assert-array-length";

        public static List<string> ToList()
        {
            var list = new List<string>();
            Type t = typeof(IgnoreFlags);
            var fields = t.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var f in fields)
            {
                list.Add((f.GetValue(null).ToString())); // null because no instance.. static..
            }
            return list;
        }
        public static bool IsValid(string flag)
        {
            return ToList().Contains(flag);
        }
    }
    public class Contract
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public List<Transaction> Transactions { get; set; } = new();

    }
    public class Transaction
    {
        public Request Request { get; set; }
        public Response Response { get; set; }
        public List<Customization> Customizations { get; set; } = new();
    }
    public class Request
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public string RawBody { get; set; }
        public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new();

        public void AddHeaders(HttpRequestMessage httpReq)
        {
            foreach (var reqHeader in httpReq.Headers)
            {
                this.Headers.Add(reqHeader.Key, reqHeader.Value);
            }
        }

    }
    public class Response
    {
        public int StatusCode { get; set; }
        public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new();
        public string RawBody { get; set; }

        public void AddHeaders(HttpResponseMessage httpResp)
        {
            foreach (var respHeader in httpResp.Headers)
            {
                this.Headers.Add(respHeader.Key, respHeader.Value);
            }
        }
    }
    public class Customization
    {
        public string PropertyName { get; set; }
        public string ParentType { get; set; }
        public int Depth { get; set; }
        public string Action { get; set; } 
    }
    public class Actions  
    {
        public static string IgnoreObjectProperty = "IgnoreObjectProperty";
        public static string CompareObjectPropertyValues = "CompareObjectPropertyValues";
    }

}
