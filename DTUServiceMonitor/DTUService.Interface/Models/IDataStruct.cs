using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTUService.Interface.Models
{
    public interface IDataStruct
    {
        byte[] Id { get; set; }
        byte[] PhoneNo { get; set; }
        byte[] ReceiveTime { get; set; }
        byte[] DataBuff { get; set; }
        byte[] DataLength { get; set; }
        byte DataType { get; set; }
        IDataStruct CreateNewInstance();



    }
}
