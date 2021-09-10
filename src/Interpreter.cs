using System;
using System.Collections.Generic;
using System.Text.Json;
using Rumpel.Models;

public static class Interpreter
{
    public static (bool, List<string>) InferSchemaAndValidate(string jsonString, string expectedJsonString, List<string> ignoreFlags, List<Customization> customizations)
    {
        var isValid = true;
        var errorMessages = new List<string>();

        var emptyJsonExpectedAndReceived = String.IsNullOrEmpty(jsonString) && String.IsNullOrEmpty(expectedJsonString);
        if (emptyJsonExpectedAndReceived)
        {
            return (isValid, errorMessages);
        }
        var json = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var expectedJson = JsonSerializer.Deserialize<JsonElement>(expectedJsonString);
        switch (expectedJson.ValueKind)
        {
            case JsonValueKind.Object:
                var (objectOk, objectPropertiesErrors) = AssertObjectProperties(expectedJson.GetRawText(), json.GetRawText(), ignoreFlags, customizations);
                if (!objectOk)
                {
                    isValid = false;
                    errorMessages.AddRange(objectPropertiesErrors);
                }
                break;
            case JsonValueKind.Array:
                var (arrayOk, arrayErrors) = AssertArray(expectedJson, json, ignoreFlags, customizations);
                if (!arrayOk)
                {
                    isValid = false;
                    errorMessages.AddRange(arrayErrors);
                }
                break;
            default:
                var (singleValueOk, singleValueErrors) = AssertSingleValue(expectedJson, json);
                if (!singleValueOk)
                {
                    isValid = false;
                    errorMessages.AddRange(singleValueErrors);
                }
                break;
        }

        return (isValid, errorMessages);

    }

    private static (bool, List<string>) AssertArray(JsonElement expectedJson, JsonElement json, List<string> ignoreFlags, List<Customization> customizations, int nestedDepth = 0, string nestedInParentType = null)
    {
        var isValid = true;
        var errorMessages = new List<string>();
        var (arrayLengthOk, arrayLengthErrors) = AssertJsonArrayLength(expectedJson, json, ignoreFlags);
        if (!arrayLengthOk)
        {
            isValid = false;
            errorMessages.AddRange(arrayLengthErrors);
        }
        if (json.GetArrayLength() <= 0 && expectedJson.GetArrayLength() <= 0)
            return (isValid, errorMessages);

        for (var i = 0; i < expectedJson.GetArrayLength(); i++)
        {

            if (expectedJson[i].ValueKind.ToString() == JsonValueKind.Array.ToString())
            {
                var nextLevel = nestedDepth + 1;
                var (nestedArrayOk, nestedArrayErrors) = AssertArray(expectedJson[i], json[i], ignoreFlags, customizations, nextLevel, "array");
                if (!nestedArrayOk)
                {
                    isValid = false;
                    errorMessages.AddRange(nestedArrayErrors);
                }
            }
            else if (expectedJson[i].ValueKind.ToString() == JsonValueKind.Object.ToString())
            {
                var nextLevel = nestedDepth + 1;
                var (objectPropertiesOk, objectPropertiesErrors) = AssertObjectProperties(expectedJson[i].GetRawText(), json[i].GetRawText(), ignoreFlags, customizations, nextLevel, "array");
                if (!objectPropertiesOk)
                {
                    isValid = false;
                    errorMessages.AddRange(objectPropertiesErrors);
                }
            }
            else
            {
                var (singleValueOk, singleValueErrors) = AssertSingleValue(expectedJson[i], json[i], nestedDepth, "array");
                if (!singleValueOk)
                {
                    isValid = false;
                    errorMessages.AddRange(singleValueErrors);
                }
            }
        }

        return (isValid, errorMessages);
    }

    private static (bool, List<string>) AssertJsonArrayLength(JsonElement expectedJson, JsonElement json, List<string> ignoreFlags)
    {
        var isValid = true;
        var errorMessages = new List<string>();
        if (expectedJson.GetArrayLength() != json.GetArrayLength() && !ignoreFlags.Contains(IgnoreFlags.IgnoreAssertArrayLength))
        {
            isValid = false;
            errorMessages.Add($"Expected array to have length {expectedJson.GetArrayLength()} but it was {json.GetArrayLength()}");
        }
        return (isValid, errorMessages);
    }

    private static (bool, List<string>) AssertObjectProperties(string expectedJsonString, string jsonString, List<string> ignoreFlags, List<Customization> customizations, int nestedDepth = 0,
    string nestedInParentType = null)
    {
        var isValid = true;
        var errorMessages = new List<string>();
        var expectedJsonObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(expectedJsonString);
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        foreach (var key in expectedJsonObj.Keys)
        {
            if (jsonObj.ContainsKey(key) == false)
            {
                if (!CustomizedTo(Actions.IgnoreObjectProperty, customizations, key, nestedDepth, nestedInParentType))
                {
                    isValid = false;
                    var errorMessage = $"Object missing property {key} of type {expectedJsonObj[key].ValueKind.ToString()}";
                    errorMessage = AddNestedInfoIfNested(nestedDepth, nestedInParentType, errorMessage);
                    errorMessages.Add(errorMessage);
                }
            }
            else if (jsonObj[key].ValueKind == JsonValueKind.Object)
            {
                var nextLevel = nestedDepth + 1;
                var (nestedObjectPropertiesOk, nestedObjectPropertiesErrors) = AssertObjectProperties(expectedJsonObj[key].GetRawText(), jsonObj[key].GetRawText(), ignoreFlags, customizations, nextLevel, "object");
                if (!nestedObjectPropertiesOk)
                {
                    isValid = false;
                    errorMessages.AddRange(nestedObjectPropertiesErrors);
                }

            }
            else if (jsonObj[key].ValueKind == JsonValueKind.Array)
            {
                var nextLevel = nestedDepth + 1;
                var (arrayOk, arrayErrors) = AssertArray(expectedJsonObj[key], jsonObj[key], ignoreFlags, customizations, nextLevel, "object");
                if (!arrayOk)
                {
                    isValid = false;
                    errorMessages.AddRange(arrayErrors);
                }
            }
            else if (jsonObj[key].ValueKind.ToString() != expectedJsonObj[key].ValueKind.ToString())
            {
                isValid = false;
                var errorMessage = $"property with name {key} is {jsonObj[key].ValueKind.ToString()} and expected type is {expectedJsonObj[key].ValueKind.ToString()}";
                errorMessage = AddNestedInfoIfNested(nestedDepth, nestedInParentType, errorMessage);
                errorMessages.Add(errorMessage);
            }
            else if (jsonObj[key].ValueKind.ToString() == expectedJsonObj[key].ValueKind.ToString()
            && CustomizedTo(Actions.CompareObjectPropertyValues, customizations, key, nestedDepth, nestedInParentType))
            {
                var (singleValueOk, singleValueErrors) = AssertPropertyValue(key, expectedJsonObj, jsonObj, nestedDepth, nestedInParentType);
                if (!singleValueOk)
                {
                    isValid = false;
                    errorMessages.AddRange(singleValueErrors);
                }
            }

        }

        return (isValid, errorMessages);
    }
    private static (bool, List<string>) AssertSingleValue(JsonElement expectedJson, JsonElement json, int nestedDepth = 0,
    string nestedInParentType = null)
    {
        var isValid = true;
        var errorMessages = new List<string>();
        if (json.ValueKind.ToString() != expectedJson.ValueKind.ToString())
        {
            isValid = false;
            var errorMessage = $"expected single value of type {expectedJson.ValueKind.ToString()} but it was {json.ValueKind.ToString()}";
            errorMessage = AddNestedInfoIfNested(nestedDepth, nestedInParentType, errorMessage);
            errorMessages.Add(errorMessage);
        }
        return (isValid, errorMessages);
    }
    private static (bool, List<string>) AssertPropertyValue(string key, Dictionary<string, JsonElement> expectedJsonObj,
    Dictionary<string, JsonElement> jsonObj, int nestedDepth = 0, string nestedInParentType = null)
    {
        var isValid = true;
        var errorMessages = new List<string>();
        if (jsonObj[key].GetRawText() != expectedJsonObj[key].GetRawText())
        {
            isValid = false;
            var errorMessage = $"property with name {key} has the value {jsonObj[key].GetRawText()} and the expected value is {expectedJsonObj[key].GetRawText()}";
            errorMessage = AddNestedInfoIfNested(nestedDepth, nestedInParentType, errorMessage);
            errorMessages.Add(errorMessage);
        }
        return (isValid, errorMessages);
    }
    private static string AddNestedInfoIfNested(int nestedDepth, string nestedInParentType, string errorMessage)
    {
        if (nestedDepth > 0 && nestedInParentType != null)
        {
            errorMessage += $" in a nested {nestedInParentType} (depth {nestedDepth})";
        }
        return errorMessage;
    }
    private static bool CustomizedTo(string action, List<Customization> customizations, string propName, int nestedDepth, string nestedInParentType)
    {
        return customizations.Exists(c => c.PropertyName == propName && c.Action == action && c.Depth == nestedDepth && c.ParentType == nestedInParentType);
    }


}
