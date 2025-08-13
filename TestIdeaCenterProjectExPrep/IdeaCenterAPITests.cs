using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using TestIdeaCenterProjectExPrep.Models;



namespace TestIdeaCenterProjectExPrep

{

    [TestFixture]
    public class IdeaCenterProjectTests
    {

        private RestClient client;
        private static string LastCreatedIdeaId;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJjYzBhNzdkNi05ZWViLTQ1ZDktYjhmMy03NmI0ZGEwZDU2MTciLCJpYXQiOiIwOC8xMy8yMDI1IDEwOjE5OjIxIiwiVXNlcklkIjoiYWZlYTFjODYtN2U3My00ZGY3LWQyOWYtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJhYWFhMUBhYWFhLmNvbSIsIlVzZXJOYW1lIjoiYWFhYTEiLCJleHAiOjE3NTUxMDE5NjEsImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.lDK4XBD89dsrn3V1_4NXAcE6u8iPhxEWbjBNvFKVeh4";
        private const string LoginEmail = "aaaa1@aaaa.com";
        private const string LoginPassword = "12345678";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = "";

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { email, password });
            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();


                if (string.IsNullOrWhiteSpace(token))
                {
                   throw new InvalidOperationException("Failed to retrieve JWT token.");
                }

                return token;

            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }

        }

        //TESTS

        [Order(1)]
        [Test]
        public void TestCreateIdea()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea.",
                Url = ""
            };



            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsed.Message, Is.EqualTo("Successfully created!"));
           
        }

        [Order(2)]
        [Test]
        public void TestGettingAllIdeas()
        {
            var request = new RestRequest("/api/Idea/All");
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Null.Or.Empty);

            var ideas = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);
            string lastIdeaNM = ideas.LastOrDefault()?.Id;

            LastCreatedIdeaId = lastIdeaNM;
        }

        [Order(3)]
        [Test]
        public void TestingEditingLastCreatedIdea()
        {
            var request = new RestRequest($"/api/Idea/Edit?ideaId={LastCreatedIdeaId}", Method.Put);
            var ideaRequest = new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "This is an updated test idea.",
                Url = ""
            };
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsed.Message, Is.EqualTo("Edited successfully"));

        }   

        [Order(4)]
        [Test]
        public void TestingDeletingLastCreatedIdea()
        {
            var request = new RestRequest($"/api/Idea/Delete?ideaId={LastCreatedIdeaId}", Method.Delete);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<string>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsed, Is.EqualTo("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void TestingCreatedIdeaWithoutRequiredFields()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
           
        }

        [Order(6)]
        [Test]
        public void TestingEditingNonExistingIdea()
        {
            
            var nonExistingIdeaId = "non-existing-idea-id";
            var request = new RestRequest($"/api/Idea/Edit?ideaId={nonExistingIdeaId}", Method.Put);
            var ideaRequest = new IdeaDTO
            {
                Title = "Edited non existing Idea",
                Description = "This idea is wrong edited",
                Url = ""
            };
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<string>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responsed, Is.EqualTo("There is no such idea!"));
        }

        [Order(7)]
        [Test]
        public void TestingDeletingNonExistingIdea()
        {
            var nonExistingIdeaId = "non-existing-idea-id";
            var request = new RestRequest($"/api/Idea/Delete?ideaId={nonExistingIdeaId}", Method.Delete);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<string>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responsed, Is.EqualTo("There is no such idea!"));
        }


        [OneTimeTearDown]
        public void TearDown() {
            this.client?.Dispose();
           
        }
    }
        
}

    