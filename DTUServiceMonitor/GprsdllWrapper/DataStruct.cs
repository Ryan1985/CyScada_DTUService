using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTUService.Interface.Models;

namespace GprsdllWrapper
{
    public class DataStruct:IDataStruct
    {
        private DtuDataStruct _dataStruct;


        public byte[] Id
        {
            get
            {
                return _dataStruct.m_dtuId;
            }
            set
            {
                _dataStruct.m_dtuId = value;
            }
        }

        public byte[] PhoneNo
        {
            get
            {
                return null;
            }
            set
            {
                
            }
        }

        public byte[] ReceiveTime
        {
            get
            {
                return _dataStruct.m_recv_time;
            }
            set
            {
                _dataStruct.m_recv_time = value;
            }
        }

        public byte[] DataBuff
        {
            get
            {
                return _dataStruct.m_data_buf;
            }
            set
            {
                _dataStruct.m_data_buf = value;
            }
        }

        public byte[] DataLength
        {
            get
            {
                return _dataStruct.m_data_len;
            }
            set
            {
                _dataStruct.m_data_len = value;
            }
        }

        public byte DataType
        {
            get
            {
                return _dataStruct.m_data_type;
            }
            set
            {
                _dataStruct.m_data_type = value;
            }
        }


        public IDataStruct CreateNewInstance()
        {
            return new DataStruct();
        }
    }
}
