using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTUService.Interface.Models
{
    public interface IInfoStruct
    {
        byte[] Id { get; set; }
        byte[] PhoneNo { get; set; }
        byte[] DynamicIp { get; set; }
        ulong ConnectionTime { get; set; }
        ulong RefreshTime { get; set; }
        IInfoStruct CreateNewInstance();
    }
}
