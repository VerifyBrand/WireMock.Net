﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using NFluent;
using WireMock.Models;
using WireMock.ResponseBuilders;
using WireMock.Settings;
using WireMock.Util;
using Xunit;
#if NET452
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace WireMock.Net.Tests.ResponseBuilders
{
    public class ResponseWithHandlebarsTests
    {
        private readonly Mock<IFluentMockServerSettings> _settingsMock = new Mock<IFluentMockServerSettings>();
        private const string ClientIp = "::1";

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_WithBodyAsJson_ResultAsObject()
        {
            // Assign
            string jsonString = "{ \"things\": [ { \"name\": \"RequiredThing\" }, { \"name\": \"Wiremock\" } ] }";
            var bodyData = new BodyData
            {
                BodyAsJson = JsonConvert.DeserializeObject(jsonString),
                DetectedBodyType = BodyType.Json,
                Encoding = Encoding.UTF8
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo_object"), "POST", ClientIp, bodyData);

            var response = Response.Create()
                .WithBodyAsJson(new { x = "test {{request.path}}" })
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(JsonConvert.SerializeObject(responseMessage.BodyData.BodyAsJson)).Equals("{\"x\":\"test /foo_object\"}");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_UrlPathVerb()
        {
            // Assign
            var body = new BodyData
            {
                BodyAsString = "whatever",
                DetectedBodyType = BodyType.String
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo"), "POSt", ClientIp, body);

            var response = Response.Create()
                .WithBody("test {{request.url}} {{request.path}} {{request.method}}")
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("test http://localhost/foo /foo POSt");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_UrlPath()
        {
            // Assign
            var urlDetails = UrlUtils.Parse(new Uri("http://localhost/wiremock/a/b"), new PathString("/wiremock"));
            var request = new RequestMessage(urlDetails, "POST", ClientIp);

            var response = Response.Create()
                .WithBody("{{request.url}} {{request.absoluteurl}} {{request.path}} {{request.absolutepath}}")
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("http://localhost/a/b http://localhost/wiremock/a/b /a/b /wiremock/a/b");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_PathSegments()
        {
            // Assign
            var urlDetails = UrlUtils.Parse(new Uri("http://localhost/wiremock/a/b"), new PathString("/wiremock"));
            var request = new RequestMessage(urlDetails, "POST", ClientIp);

            var response = Response.Create()
                .WithBody("{{request.pathsegments.[0]}} {{request.absolutepathsegments.[0]}}")
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("a wiremock");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_Query()
        {
            // Assign
            var body = new BodyData
            {
                BodyAsString = "abc",
                DetectedBodyType = BodyType.String
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo?a=1&a=2&b=5"), "POST", ClientIp, body);

            var response = Response.Create()
                .WithBody("test keya={{request.query.a}} idx={{request.query.a.[0]}} idx={{request.query.a.[1]}} keyb={{request.query.b}}")
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("test keya=1 idx=1 idx=2 keyb=5");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_Header()
        {
            // Assign
            var body = new BodyData
            {
                BodyAsString = "abc",
                DetectedBodyType = BodyType.String
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo"), "POST", ClientIp, body, new Dictionary<string, string[]> { { "Content-Type", new[] { "text/plain" } } });

            var response = Response.Create().WithHeader("x", "{{request.headers.Content-Type}}").WithBody("test").WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("test");
            Check.That(responseMessage.Headers).ContainsKey("x");
            Check.That(responseMessage.Headers["x"]).ContainsExactly("text/plain");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_Headers()
        {
            // Assign
            var body = new BodyData
            {
                BodyAsString = "abc",
                DetectedBodyType = BodyType.String
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo"), "POST", ClientIp, body, new Dictionary<string, string[]> { { "Content-Type", new[] { "text/plain" } } });

            var response = Response.Create().WithHeader("x", "{{request.headers.Content-Type}}", "{{request.url}}").WithBody("test").WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("test");
            Check.That(responseMessage.Headers).ContainsKey("x");
            Check.That(responseMessage.Headers["x"]).Contains("text/plain");
            Check.That(responseMessage.Headers["x"]).Contains("http://localhost/foo");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_Origin_Port_Protocol_Host()
        {
            // Assign
            var body = new BodyData
            {
                BodyAsString = "abc",
                DetectedBodyType = BodyType.String
            };
            var request = new RequestMessage(new UrlDetails("http://localhost:1234"), "POST", ClientIp, body);

            var response = Response.Create()
                .WithBody("test {{request.origin}} {{request.port}} {{request.protocol}} {{request.host}}")
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsString).Equals("test http://localhost:1234 1234 http localhost");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_WithBodyAsJson_ResultAsArray()
        {
            // Assign
            string jsonString = "{ \"a\": \"test 1\", \"b\": \"test 2\" }";
            var bodyData = new BodyData
            {
                BodyAsJson = JsonConvert.DeserializeObject(jsonString),
                DetectedBodyType = BodyType.Json,
                Encoding = Encoding.UTF8
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo_array"), "POST", ClientIp, bodyData);

            var response = Response.Create()
                .WithBodyAsJson(new[] { "first", "{{request.path}}", "{{request.bodyAsJson.a}}", "{{request.bodyAsJson.b}}", "last" })
                .WithTransformer();

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(JsonConvert.SerializeObject(responseMessage.BodyData.BodyAsJson)).Equals("[\"first\",\"/foo_array\",\"test 1\",\"test 2\",\"last\"]");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_WithBodyAsFile()
        {
            // Assign
            var request = new RequestMessage(new UrlDetails("http://localhost/foo?MyUniqueNumber=1"), "GET", ClientIp);

            var response = Response.Create()
                .WithTransformer()
                .WithBodyFromFile(@"c:\\{{request.query.MyUniqueNumber}}\test.xml"); // why use a \\ here ?

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsFile).Equals(@"c:\1\test.xml");
        }

        [Fact]
        public async Task Response_ProvideResponse_Handlebars_WithBodyAsFile_JsonPath()
        {
            // Assign
            string jsonString = "{ \"MyUniqueNumber\": \"1\" }";
            var bodyData = new BodyData
            {
                BodyAsString = jsonString,
                BodyAsJson = JsonConvert.DeserializeObject(jsonString),
                DetectedBodyType = BodyType.Json,
                DetectedBodyTypeFromContentType = BodyType.Json,
                Encoding = Encoding.UTF8
            };
            var request = new RequestMessage(new UrlDetails("http://localhost/foo"), "POST", ClientIp, bodyData);

            string jsonPath = "\"$.MyUniqueNumber\"";
            var response = Response.Create()
                .WithTransformer()
                .WithBodyFromFile(@"c:\\{{JsonPath.SelectToken request.body " + jsonPath + "}}\\test.json"); // why use a \\ here ?

            // Act
            var responseMessage = await response.ProvideResponseAsync(request, _settingsMock.Object);

            // Assert
            Check.That(responseMessage.BodyData.BodyAsFile).Equals(@"c:\1\test.json");
        }
    }
}