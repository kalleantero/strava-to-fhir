using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace StravaActivityToFhir.Models.Strava.Webhook
{
    public class SubscriptionValidationResponse
    {
        [JsonProperty(PropertyName = "hub.challenge")]
        public string HubChallenge { get; set; }
    }
}
