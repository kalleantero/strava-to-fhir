using System;
using System.Collections.Generic;
using System.Text;

namespace StravaActivityToFhir.Models.Strava.Webhook
{
    public class Event
    {
        public string aspect_type { get; set; }
        public int event_time { get; set; }
        public long object_id { get; set; }
        public string object_type { get; set; }
        public long owner_id { get; set; }
        public long subscription_id { get; set; }
        public Updates updates { get; set; }
    }

}
