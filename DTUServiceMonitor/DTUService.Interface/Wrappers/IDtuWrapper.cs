using System;
using DTUService.Interface.Models;

namespace DTUService.Interface.Wrappers
{
    
    public interface IDtuWrapper
    {
        int StartService(ushort uiListenPort);
        int StopService();
        int GetNextData(ref IDataStruct pDataStruct, ushort waitseconds);
        int SendData(uint DtuId, ushort len, byte[] buf);
        uint GetDtuCount();
        int GetDtuByPosition(uint pos, ref IInfoStruct pDtuInfo);
        void GetLastError(IntPtr str, int nMaxStrSize);

    }
}
