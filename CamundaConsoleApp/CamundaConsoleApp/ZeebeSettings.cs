using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaConsoleApp
{
    public class ZeebeSettings
    {
        public string GatewayAddress { get; set; } = "";
        public string AuthServer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
    }
}
