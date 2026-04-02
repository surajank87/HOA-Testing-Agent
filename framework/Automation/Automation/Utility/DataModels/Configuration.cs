using Utility.Enum;
using Environment = Utility.Enum.Environment;

namespace Utility.DataModels
{
    public class Configuration
    {
        public Tenant Tenant { get; set; }
        public string LOB { get; set; }
        public string Carrier { get; set; }
        public string PersonalLineOrCommercialLine { get; set; }
        public Environment Environment { get; set; }
        public SubTenant SubTenant { get; set; }
        public string CarrierRequestFormat { get; set; }
        public string State { get; set; }

    }
}
