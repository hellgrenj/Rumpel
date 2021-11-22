using System.Reflection;

namespace Rumpel.Models;
public static class IgnoreFlags
{
    public const string IgnoreAssertStatusCode = "--ignore-assert-status-code";
    public const string IgnoreAssertArrayLength = "--ignore-assert-array-length";

    public static List<string> ToList()
    {
        var list = new List<string>();
        Type t = typeof(IgnoreFlags);
        var fields = t.GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (var f in fields)
        {
            list.Add((f.GetValue(null).ToString()));
        }
        return list;
    }
    public static bool IsValid(string flag)
    {
        return ToList().Contains(flag);
    }
}
public record Contract
{
    public string Name { get; init; }
    public string URL { get; init; }
    public List<Transaction> Transactions { get; init; } = new();
}
public class Transaction
{
    public Request Request { get; set; }
    public Response Response { get; set; }
    public List<Customization> Customizations { get; set; } = new();
    public List<SimulatedCondition> SimulatedConditions { get; set; } = new();
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
public record Customization(string PropertyName, int Depth, string Action);
public class CustomizationActions
{
    public const string IgnoreObjectProperty = "IgnoreObjectProperty";
    public const string CompareObjectPropertyValues = "CompareObjectPropertyValues";
}
public class SimulatedConditionTypes
{
    public const string FixedDelay = "FixedDelay";
    public const string RandomDelay = "RandomDelay";
    public const string Sometimes500 = "Sometimes500";
}
public record SimulatedCondition(string Type, string Value);


