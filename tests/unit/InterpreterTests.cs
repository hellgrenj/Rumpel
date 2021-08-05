using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Rumpel.Models;
using Xunit;

namespace unit
{
    public class InterpreterTests
    {
        [Fact]
        public void InferSchemaAndValidate_returns_true_and_empty_errors_if_json_ok()
        {
            var jsonString = @"
            { 
                ""id"": 1, 
                ""name"": ""test"" 
            }";
            var expectedJsonString = @"
            { 
                ""id"": 37, 
                ""name"": ""testarido..."" 
            }";
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.True(isValid);
            Assert.Empty(errorMessages);
        }

        [Fact]
        public void InferSchemaAndValidate_validates_object_properties()
        {
            var jsonString = @"
            { 
                ""id"": 1, 
                ""name"": ""test"" 
            }";
            var expectedJsonString = @"
            { 
                ""id"": 18, 
                ""name"": ""does not matter"" 
            }";

            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.True(isValid);
            Assert.Empty(errorMessages);
        }



        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessages_if_wrong_property_type()
        {
            var jsonString = @"
            { 
                ""id"": 1, 
                ""name"": ""test"" 
            }";
            var expectedJsonString = @"
            { 
                ""id"": ""expecting a string id"", 
                ""name"": ""test"" 
            }";

            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.False(isValid);
            Assert.Contains("property with name id is Number and expected type is String", errorMessages[0]);
        }

        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessages_if_missing_property()
        {
            var jsonString = @"
            { 
                ""id"": 1, 
                ""name"": ""test"" 
            }";
            var expectedJsonString = @"
            { 
                ""id"": 1, 
                ""name"": ""test"", 
                ""age"": 39
            }";


            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());
            Assert.False(isValid);
            Assert.Contains("Object missing property age", errorMessages[0]);
        }



        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessages_if_wrong_single_type()
        {
            var jsonString = "1";
            var expectedJsonString = @"""Expecting string""";

            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());
            Assert.False(isValid);
            Assert.Contains("expected single value of type String but it was Number", errorMessages[0]);
        }

        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessagess_if_wrong_singleValue_type_in_array()
        {

            var jsonString = @"[""test"",""test2"",""test3""]";
            var expectedJsonString = @"[1,2,3]";
       
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.False(isValid);
            Assert.Contains("expected single value of type Number but it was String", errorMessages[0]);
        }
        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessages_If_unexpected_object_in_array()
        {
            var jsonString = @"[{""id"":1, ""name"":32},{""id"":2, ""name"":""maja""}]"; // name should be string not 32 as in first object in list..
            var expectedJsonString = @"[{""id"":1, ""name"":""should be string""},{""id"":2, ""name"":""maja""}]";
        
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString,new List<string>());

            Assert.False(isValid);
            Assert.Contains("property with name name is Number and expected type is String", errorMessages[0]);
        }


        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessagess_if_unexpected_array_length()
        {

            var jsonString = @"[""test"",""test2"",""test3""]";
            var expectedJsonString = @"[""test"",""test2""]"; // expected array length == 2
          
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.False(isValid);
            Assert.Contains("Expected array to have length 2 but it was 3", errorMessages[0]);
        }
        [Fact]
        public void InferSchemaAndValidate_returns_true_if_unexpected_array_length_but_ignore_flag()
        {

            var jsonString = @"[""test"",""test2"",""test3""]";
            var expectedJsonString = @"[""test"",""test2""]"; // expected array length == 2
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>() { "--ignore-assert-array-length" });

            Assert.True(isValid);
            Assert.Empty(errorMessages);

        }

        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessage_if_error_in_nested_array()
        {

            var jsonString = @"[[1,2,3],[1,2,3]]"; // passing in an array of number arrays.. expecting array of string arrays
            var expectedJsonString = @"[[""string"",""string"",""string""],[""string"",""string"",""string""]]";
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.False(isValid);
            Assert.Contains("expected single value of type String but it was Number in a nested array (depth 1)", errorMessages[0]);

        }
        [Fact]
        public void InferSchemaAndValidate_returns_false_and_errorMessages_If_error_in_nested_object()
        {
            var jsonString = @"{""id"":1, ""name"":""should be string"", ""child"": {""id"":1, ""name"":2}}"; // name in nested object should be string but is number
            var expectedJsonString = @"{""id"":1, ""name"":""should be string"", ""child"": {""id"":1, ""name"":""should be string""}}";
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());

            Assert.False(isValid);
            Assert.Contains("property with name name is Number and expected type is String in a nested object (depth 1)", errorMessages[0]);
        }

        [Fact]
        public void InferSchemaAndValidate_can_infer_schema_and_validate_complex_json()
        {

            var complexObjectExpected = new
            {
                prop1 = new
                {
                    prop11 = 1,
                    prop12 = 2,
                    prop13 = "hej"
                },
                propx = new { propx1 = "hellu" },
                propy = new { propy1 = "hellu" },
                prop2 = new ArrayList(){new {
                   prop21 = new ArrayList(){"hej", "hopp"}
                }, new {
                   prop22 = new ArrayList(){"hej2", "hopp2"}
                }}
            };
            var complexObject = new
            {
                prop1 = new
                {
                    prop11 = 1,
                    prop12 = 2,
                    prop13 = "hej"
                },
                propx = new { propx1 = "hellu" }, // we are missing property propy here...
                prop2 = new ArrayList(){new {
                   prop21 = new ArrayList(){"1", "2"}
                }, new {
                   prop22 = new ArrayList(){"1", 2} // expecting string here... 3 levels deep..
                }}
            };
            var expectedJsonString = JsonSerializer.Serialize(complexObjectExpected);
            var jsonString = JsonSerializer.Serialize(complexObject);
         
            var (isValid, errorMessages) = Interpreter.InferSchemaAndValidate(jsonString, expectedJsonString, new List<string>());


            Assert.False(isValid);
            Assert.Equal(errorMessages.Count, 2);
            Assert.Contains("Object missing property propy of type Object", errorMessages[0]);
            Assert.Contains("expected single value of type String but it was Number in a nested array (depth 3)", errorMessages[1]);

        }

    }
}
