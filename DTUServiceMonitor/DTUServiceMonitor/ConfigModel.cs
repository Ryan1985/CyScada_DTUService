using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTUServiceMonitor
{
    public class ConfigModel
    {
        public string DTUId { get; set; }
        public string PhoneNo { get; set; }
        public int ServerFunctionCode { get; set; }
        public int ServerAddressStart { get; set; }
        public int ServerAddressLength { get; set; }
        public int DTUDeviceId { get; set; }
        public int DTUFunctionCode { get; set; }
        public int DTUAddressStart { get; set; }
        public int DTUAddressLength { get; set; }

        public static string[] PropertyNameIndex
        {
            get
            {
                return new []
                {
                    "DTUId","PhoneNo","ServerFunctionCode","ServerAddressStart","ServerAddressLength","DTUDeviceId","DTUFunctionCode","DTUAddressStart","DTUAddressLength"
                };
            }
        }
    }

}
