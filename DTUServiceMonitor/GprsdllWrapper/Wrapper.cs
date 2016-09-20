using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTUService.Interface.Models;
using DTUService.Interface.Wrappers;

namespace GprsdllWrapper
{
    public class Wrapper:IDtuWrapper
    {

        public int StartService(ushort uiListenPort)
        {
            return GPRSDTUWrapper.DSStartService(uiListenPort);
        }

        public int StopService()
        {
            return GPRSDTUWrapper.DSStopService();
        }

        public int GetNextData(ref IDataStruct pDataStruct, ushort waitseconds)
        {
            var datastruct = new DtuDataStruct();
            var result = GPRSDTUWrapper.DSGetNextData(ref datastruct, waitseconds);

            if (pDataStruct == null)
                pDataStruct = new DataStruct();

            pDataStruct.Id = datastruct.m_dtuId;
            pDataStruct.DataBuff = datastruct.m_data_buf;
            pDataStruct.DataLength = datastruct.m_data_len;
            pDataStruct.DataType = datastruct.m_data_type;
            pDataStruct.ReceiveTime = datastruct.m_recv_time;

            return result;
        }

        public int SendData(uint DtuId, ushort len, byte[] buf)
        {
            return GPRSDTUWrapper.DSSendData(DtuId, len, buf);
        }

        public uint GetDtuCount()
        {
            return GPRSDTUWrapper.DSGetModemCount();
        }

        public int GetDtuByPosition(uint pos, ref IInfoStruct pDtuInfo)
        {
            var infostruct = new DtuInfoStruct();
            var result = GPRSDTUWrapper.DSGetModemByPosition(pos, ref infostruct);
            if (pDtuInfo == null)
                pDtuInfo = new InfoStruct();

            pDtuInfo.ConnectionTime = infostruct.m_conn_time;
            pDtuInfo.DynamicIp = infostruct.m_dynip;
            pDtuInfo.Id = infostruct.m_dtuId;
            pDtuInfo.PhoneNo = infostruct.m_phoneno;
            pDtuInfo.RefreshTime = infostruct.m_refresh_time;

            return result;
        }

        public void GetLastError(IntPtr str, int nMaxStrSize)
        {
            GPRSDTUWrapper.DSGetLastError(str, nMaxStrSize);
        }
    }
}
