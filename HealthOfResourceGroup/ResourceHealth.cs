using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthOfResourceGroup
{
    public class ResourceHealth
    {
        public string nextLink { get; set; }
        public AvailabilityStatus[] value { get; set; }
    }

    public class AvailabilityStatus
    {
        public string id { get; set; }
        public string location { get; set; }
        public string name { get; set; }
        public Properties properties { get; set; }
        public string type { get; set; }
    }

    public class Properties
    {
        public AvailabilityStateValue availabilityStateValue { get; set; }
        public string detailedStatus { get; set; }
        public string occuredTime { get; set; }
        public ReasonChronicityType reasonChronicity { get; set; }
        public string reasonType { get; set; }
        public string reportedTime { get; set; }
        public string resolutionETA { get; set; }
        public string rootCauseAttributionTime { get; set; }
        //serviceImpactingEvents 
        public string summary { get; set; }
    }

    public enum AvailabilityStateValue
    {
        Available,
        Unavailable,
        Unknown
    }

    public enum ReasonChronicityType
    {
        Persistent,
        Transient
    }
}
