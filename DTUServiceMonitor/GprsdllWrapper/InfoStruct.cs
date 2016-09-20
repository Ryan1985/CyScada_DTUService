using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTUService.Interface.Models;

namespace GprsdllWrapper
{
    public  class InfoStruct:IInfoStruct
    {
        private DtuInfoStruct _infoStruct;

        public byte[] Id
        {
            get
            {
               return _infoStruct.m_dtuId;
            }
            set
            {
               _infoStruct.m_dtuId = value;
            }
        }

        public byte[] PhoneNo
        {
            get
            {
                return _infoStruct.m_phoneno;
            }
            set
            {
                _infoStruct.m_phoneno = value;
            }
        }

        public byte[] DynamicIp
        {
            get
            {
                return _infoStruct.m_dynip;
            }
            set
            {
                _infoStruct.m_dynip = value;
            }
        }

        public ulong ConnectionTime
        {
            get
            {
                return _infoStruct.m_conn_time;
            }
            set
            {
                _infoStruct.m_conn_time = value;
            }
        }

        public ulong RefreshTime
        {
            get
            {
                return _infoStruct.m_refresh_time;
            }
            set
            {
                _infoStruct.m_refresh_time = value;
            }
        }


        public IInfoStruct CreateNewInstance()
        {
            return new InfoStruct();
        }
    }
}
