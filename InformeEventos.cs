using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    public class InformeEventos
    {
        // 英文属性（原有）
        public string SequenceNumber { get; set; }
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string DeviceNumber { get; set; }
        public string DeviceName { get; set; }
        public string EventType { get; set; }
        public string EventTime { get; set; }
        public string RemoteHostAddress { get; set; }

        // 西班牙语属性（供报表与现有代码使用）
        // 注意：这些属性与上面的英文属性并无强绑定，仅为兼容现有代码与 RDLC 字段命名
        public string num { get; set; }             // 日志编号
        public string documento { get; set; }       // 员工证件号/员工ID
        public string nombres { get; set; }         // 名
        public string apellidos { get; set; }       // 姓
        public string fecha { get; set; }           // 日期字符串
        public string hora { get; set; }            // 时间字符串
        public string dispositivo { get; set; }     // 设备名称/编号

        public InformeEventos()
        {
        }
    }
}

