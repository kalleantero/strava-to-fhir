using System;
using System.Collections.Generic;
using System.Text;

namespace StravaActivityToFhir.Models.Strava
{
    public class Map
    {
        public string id { get; set; }
        public string summary_polyline { get; set; }
        public int resource_state { get; set; }
    }
}
