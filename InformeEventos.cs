using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    public class InformeEventos
    {
        // 事件日志报表使用的英文属性，与 access_logs 表字段一一对应
        public long SequenceNumber { get; set; }
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public int DeviceNumber { get; set; }
        public string DeviceName { get; set; }
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public string RemoteHostAddress { get; set; }

        // 便于报表展示的派生字符串字段
        public string EventDateText
        {
            get
            {
                return EventTime == DateTime.MinValue ? string.Empty : EventTime.ToString("yyyy-MM-dd");
            }
        }

        public string EventTimeText
        {
            get
            {
                return EventTime == DateTime.MinValue ? string.Empty : EventTime.ToString("HH:mm:ss");
            }
        }

        public InformeEventos()
        {
        }
    }
}

