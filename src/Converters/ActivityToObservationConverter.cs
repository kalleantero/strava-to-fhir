using Hl7.Fhir.Model;
using StravaActivityToFhir.Interfaces;
using StravaActivityToFhir.Models.Strava;
using System;
using System.Collections.Generic;
using System.Text;

namespace StravaActivityToFhir.Converters
{
public class ActivityToObservationConverter : IConverter<Activity, Observation>
{
    public Observation Convert(Activity activity)
    {
        return new Observation()
        {
            Meta = new Meta()
            {
                Profile = new string[] { "http://phr.kanta.fi/StructureDefinition/fiphr-sd-exercisetracking-stu3" }
            },
            Language = "en",
            Text = new Narrative()
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div>Time: {activity?.start_date_local} Result: {activity.elapsed_time / 60} min</div>"
            },
            Identifier = new List<Identifier>()
            {
                new Identifier()
                {
                    Use = Identifier.IdentifierUse.Usual,
                    System = "urn:ietf:rfc:3986",
                    Value = "urn:uuid:6288f477-90ef-424a-b6e3-da4ff18a058e"
                },
                new Identifier()
                {
                    Use = Identifier.IdentifierUse.Usual,
                    System = "urn:ietf:rfc:3986",
                    Value = "urn:uuid:00000000-5cb1-fef8-16b6-835409677fb6"
                }
            },
            Status = ObservationStatus.Final,
            Category = new List<CodeableConcept>()
            {
                new CodeableConcept()
                {
                        Coding = new List<Coding>()
                        {
                            new Coding()
                            {
                                System = "http://phr.kanta.fi/fiphr-cs-fitnesscategory",
                                Code = "fitness",
                                Display = "Fitness"
                            }
                        }
                }
            },
            Code = new CodeableConcept()
            {
                Coding = new List<Coding>()
                {
                    new Coding()
                    {
                        System = "http://loinc.org",
                        Code = "55411-3",
                        Display = "Exercise duration"
                    }
                }
            },
            Subject = new ResourceReference()
            {
                Reference = "Patient/" + activity?.athlete?.id,
            },
            Issued = activity?.start_date_local,
            Performer = new List<ResourceReference>()
            {
                new ResourceReference()
                {
                    Reference = "Patient/"+ activity?.athlete?.id
                }
            },
            Value = new Quantity()
            {
                Value = activity.elapsed_time / 60,
                Unit = "min",
                System = "http://unitsofmeasure.org",
                Code = "min"
            }
        };
    }
}
}
