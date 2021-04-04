using System;
using System.IO;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using StravaActivityToFhir.Converters;
using StravaActivityToFhir.Models.Strava;
using StravaActivityToFhir.Models.Strava.Webhook;

namespace StravaActivityToFhir
{
    public class StravaActivityToFhir
    {
        [FunctionName("StravaActivityToFhir")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            if (req.Method == "GET")
            {
                return Verify(req, log);
            }
            else if (req.Method == "POST")
            {
                return await ReceiveAsync(req, log);
            }

            log.LogInformation("Unknown operation");

            return new BadRequestResult();
        }

        /// <summary>
        /// Method handles incoming Strava event
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private async Task<IActionResult> ReceiveAsync(HttpRequest req, ILogger log)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            log.LogInformation("Incoming Strava event" + requestBody);

            var data = JsonConvert.DeserializeObject<Event>(requestBody);

            if (data != null)
            {
                if (data.aspect_type == "create")
                {
                    var activityData = await GetActivity(data.object_id);
                    if (activityData != null)
                    {
                        var response = await SendDataToFhirRepository(activityData);

                        if (response != null)
                        {
                            var serializer = new FhirJsonSerializer(new SerializerSettings()
                            {
                                Pretty = true
                            });

                            var resource = serializer.SerializeToString(response);

                            log.LogInformation("Data succesfully sent to FHIR repository: " + resource);
                        }
                    }
                }
                else if (data.aspect_type == "delete")
                {
                    throw new NotImplementedException();
                }

            }
            return new OkObjectResult("OK");
        }

        private IActionResult Verify(HttpRequest req, ILogger log)
        {
            string hubChallenge = req.Query["hub.challenge"];
            string verifyToken = req.Query["hub.verify_token"];

            log.LogInformation($"Incoming Strava subcription validation request. Challenge was ${hubChallenge} and verify token was ${verifyToken}.");

            if (string.IsNullOrEmpty(hubChallenge) || string.IsNullOrEmpty(verifyToken))
            {
                return new BadRequestResult();
            }
            var stravaWebhookVerificationToken = Environment.GetEnvironmentVariable("StravaWebhookVerificationToken");

            if (verifyToken != stravaWebhookVerificationToken)
            {
                return new BadRequestResult();
            }

            var response = new SubscriptionValidationResponse()
            {
                HubChallenge = hubChallenge
            };

            return new OkObjectResult(response);
        }

        /// <summary>
        /// Converts Strava activity to FHIR observation and sends it to FHIR repository
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private async Task<Observation> SendDataToFhirRepository(Activity activity)
        {
            var fhirRepositoryUrl = Environment.GetEnvironmentVariable("FhirRepositoryUrl");

            if (string.IsNullOrEmpty(fhirRepositoryUrl))
            {
                throw new ArgumentException("Fhir repository Url is missing from the configuration");
            }

            var converter = new ActivityToObservationConverter();
            var observation = converter.Convert(activity);
            var accessToken = await GetAadAccessToken();

            var settings = new FhirClientSettings
            {
                PreferredFormat = ResourceFormat.Json,
                PreferredReturn = Prefer.ReturnRepresentation,
            };

            var client = new FhirClient(fhirRepositoryUrl, settings);
            client.RequestHeaders.Add("Authorization", "Bearer " + accessToken);
            return await client.CreateAsync(observation);
        }

        /// <summary>
        /// Retrieves access token from Azure Ad using app credentials
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetAadAccessToken()
        {
            var fhirResource = Environment.GetEnvironmentVariable("FhirResource");
            var azureAdTenant = Environment.GetEnvironmentVariable("AzureAdTenant");
            var azureAdClientId = Environment.GetEnvironmentVariable("AzureAdClientId");
            var azureAdClientSecret = Environment.GetEnvironmentVariable("AzureAdClientSecret");

            if(string.IsNullOrEmpty(fhirResource) || string.IsNullOrEmpty(azureAdTenant) ||
                string.IsNullOrEmpty(azureAdClientId) || string.IsNullOrEmpty(azureAdClientSecret))
            {
                throw new ArgumentException("Azure Ad configuration values are invalid");
            }
            // Authentication using app credentials
            var authenticationContext = new AuthenticationContext(azureAdTenant);
            var credential = new ClientCredential(azureAdClientId, azureAdClientSecret);
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(fhirResource, credential);
            return authenticationResult.AccessToken;
        }

        /// <summary>
        /// Retrieves Strava exercise activity by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<Activity> GetActivity(long id)
        {      
            var stravaBaseUrl = Environment.GetEnvironmentVariable("StravaBaseUrl");
            var stravaActivityEndpoint = Environment.GetEnvironmentVariable("StravaActivityEndpoint");

            //This version uses hard coded access token. TODO: Automate access token persistance and implement refresh token usage
            var accessToken = Environment.GetEnvironmentVariable("StravaToken:AccessToken");

            if (string.IsNullOrEmpty(stravaBaseUrl) || string.IsNullOrEmpty(stravaActivityEndpoint) || string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Strava configuration values are missing from the configuration");
            }

            var client = new RestClient(stravaBaseUrl)
            {
                Authenticator = new JwtAuthenticator(accessToken)
            };

            var request = new RestRequest(string.Format(stravaActivityEndpoint, id), DataFormat.Json);

            return await client.GetAsync<Activity>(request);
        }
    }
}

