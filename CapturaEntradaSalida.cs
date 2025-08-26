using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ControlEntradaSalida
{
    //实时接收、显示和记录出入事件
    public partial class CapturaEntradaSalida : Form
    {
        private HCNetSDK.MSGCallBack m_falarmData = null;
        private string path = null;
        private int m_lLogNum = 0;
        private Dictionary<int, int> lAlarmHandles = new Dictionary<int, int>(); // 设备ID到报警句柄的映射
        private string cardnumber = null;
        private DeviceConnectionInfo selectedDevice = null; // 当前选中的设备

        
        BaseDatosMySQL bd;

        //初始化窗体
        public CapturaEntradaSalida()
        {
            InitializeComponent();
        }

        //插入一条出入事件到数据库 entradas_salidas 表,参数：事件编号、日期、时间、人员编号（卡号）、事件类型
                private bool InsertarEntradaSalida(string num, string fecha, string hora, string documento, string evento, int dispositivo_id)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "INSERT INTO access_logs " +
                    "(log_number, log_date, log_time, employee_id, device_id, event_type, created_at) " +
                    "VALUES (@log_number, @log_date, @log_time, @employee_id, @device_id, @event_type, @created_at)";
                try
                {

                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@log_number", num);//事件编号
                    cmd.Parameters.AddWithValue("@log_date", fecha);//日期
                    cmd.Parameters.AddWithValue("@log_time", hora);//时间
                    cmd.Parameters.AddWithValue("@employee_id", documento);//人员编号（卡号）
                    cmd.Parameters.AddWithValue("@device_id", dispositivo_id);
                    cmd.Parameters.AddWithValue("@event_type", evento);//事件类型                
                    cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//时间
                    cmd.ExecuteNonQuery();
                    bd.desconectarMySQL();

                    retval = true;

                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(bd.errormsg);
            }
            return retval;
        }


        //登录设备,设置报警监听参数,实现与设备建立"监听"连接
        private void Deploy()
        {
            // 清除之前的报警连接
            UnDeploy();
            
            // 获取所有已连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
                
            if (connectedDevices.Count == 0)
            {
                MessageBox.Show("没有已连接的设备", "设备连接", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            m_falarmData = new HCNetSDK.MSGCallBack(MsgCallback);//注册回调函数
            if (!HCNetSDK.NET_DVR_SetDVRMessageCallBack_V50(0, m_falarmData, IntPtr.Zero))
            {
                MessageBox.Show("NET_DVR_SetDVRMessageCallBack_V50 fail", "operation fail", MessageBoxButtons.OK);
                return;
            }
            
            // 为每个已连接的设备设置报警监听
            foreach (var device in connectedDevices)
            {
                HCNetSDK.NET_DVR_SETUPALARM_PARAM struSetupAlarmParam = new HCNetSDK.NET_DVR_SETUPALARM_PARAM();
                struSetupAlarmParam.dwSize = (uint)Marshal.SizeOf(struSetupAlarmParam);
                struSetupAlarmParam.byLevel = 1;
                struSetupAlarmParam.byAlarmInfoType = 1;
                struSetupAlarmParam.byDeployType = (byte)1;

                int alarmHandle = HCNetSDK.NET_DVR_SetupAlarmChan_V41(device.UserID, ref struSetupAlarmParam);
                if (alarmHandle < 0)
                {
                    MessageBox.Show($"设备 {device.Name} 设置报警监听失败，错误代码: " + HCNetSDK.NET_DVR_GetLastError(), "Setup alarm chan failed");
                }
                else
                {
                    lAlarmHandles[device.Id] = alarmHandle;
                    //MessageBox.Show($"设备 {device.Name} 设置报警监听成功");
                }
            }
        }
        
        //断开所有设备的报警监听
        private void UnDeploy()
        {
            foreach (var kvp in lAlarmHandles)
            {
                HCNetSDK.NET_DVR_CloseAlarmChan_V30(kvp.Value);
            }
            lAlarmHandles.Clear();
        }

        //设备每次发送事件时会调用此方法,识别事件类型
        private void MsgCallback(int lCommand, ref HCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            switch (lCommand)
            {
                case HCNetSDK.COMM_ALARM_ACS://只处理门禁事件
                    ProcessCommAlarmACS(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);//分发到 ProcessCommAlarmACS()
                    break;
                default:
                    break;
            }
        }
        /*处理具体门禁事件信息
        将 SDK 返回的 NET_DVR_ACS_ALARM_INFO 转为结构体；
        判断事件主类型（MAJOR_）与次类型（MINOR_），用 TypeMap 转换为字符串；
        提取详细信息（时间、卡号、用户信息、读卡器号、门号、验证号等）；
        如果事件是 MINOR_FACE_VERIFY_PASS（人脸验证通过）：
            查询人员姓名：调用 ObtenerNombreEmpleado(...)；
            在界面 listViewEventos 显示；
            插入数据库记录
         */
                private void ProcessCommAlarmACS(ref HCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            HCNetSDK.NET_DVR_ACS_ALARM_INFO struAcsAlarmInfo = new HCNetSDK.NET_DVR_ACS_ALARM_INFO();
            struAcsAlarmInfo = (HCNetSDK.NET_DVR_ACS_ALARM_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(HCNetSDK.NET_DVR_ACS_ALARM_INFO));// 将 SDK 返回的 NET_DVR_ACS_ALARM_INFO 转为结构体
            HCNetSDK.NET_DVR_LOG_V30 struFileInfo = new HCNetSDK.NET_DVR_LOG_V30();
            struFileInfo.dwMajorType = struAcsAlarmInfo.dwMajor;
            struFileInfo.dwMinorType = struAcsAlarmInfo.dwMinor;
            char[] csTmp = new char[256];

            if (HCNetSDK.MAJOR_ALARM == struFileInfo.dwMajorType)
            {
                TypeMap.AlarmMinorTypeMap(struFileInfo, csTmp);
            }
            else if (HCNetSDK.MAJOR_OPERATION == struFileInfo.dwMajorType)
            {
                TypeMap.OperationMinorTypeMap(struFileInfo, csTmp);
            }
            else if (HCNetSDK.MAJOR_EXCEPTION == struFileInfo.dwMajorType)
            {
                TypeMap.ExceptionMinorTypeMap(struFileInfo, csTmp);
            }
            else if (HCNetSDK.MAJOR_EVENT == struFileInfo.dwMajorType)
            {
                TypeMap.EventMinorTypeMap(struFileInfo, csTmp);
            }

            String szInfo = new String(csTmp).TrimEnd('\0');
            String szInfoBuf = null;
            szInfoBuf = szInfo;
            /**************************************************/
            String name = System.Text.Encoding.UTF8.GetString(struAcsAlarmInfo.sNetUser).TrimEnd('\0');
            for (int i = 0; i < struAcsAlarmInfo.sNetUser.Length; i++)
            {
                if (struAcsAlarmInfo.sNetUser[i] == 0)
                {
                    name = name.Substring(0, i);
                    break;
                }
            }
            /**************************************************/

            string tipoevento = string.Format("{0}", szInfo);
            string fecha = string.Format("{0,4}-{1:D2}-{2}", struAcsAlarmInfo.struTime.dwYear, struAcsAlarmInfo.struTime.dwMonth, struAcsAlarmInfo.struTime.dwDay);
            string hora = string.Format("{0:D2}:{1:D2}:{2:D2}", struAcsAlarmInfo.struTime.dwHour, struAcsAlarmInfo.struTime.dwMinute, struAcsAlarmInfo.struTime.dwSecond);
            string numerotarjeta = null;

            szInfoBuf = string.Format("{0} time:{1,4}-{2:D2}-{3} {4:D2}:{5:D2}:{6:D2}, [{7}]({8})", szInfo, struAcsAlarmInfo.struTime.dwYear, struAcsAlarmInfo.struTime.dwMonth,
                struAcsAlarmInfo.struTime.dwDay, struAcsAlarmInfo.struTime.dwHour, struAcsAlarmInfo.struTime.dwMinute, struAcsAlarmInfo.struTime.dwSecond,
                struAcsAlarmInfo.struRemoteHostAddr.sIpV4, name);

            if (struAcsAlarmInfo.struAcsEventInfo.byCardNo[0] != 0)
            {
                this.cardnumber = System.Text.Encoding.UTF8.GetString(struAcsAlarmInfo.struAcsEventInfo.byCardNo).TrimEnd('\0');
                szInfoBuf = szInfoBuf + "+卡号:" + cardnumber;
                numerotarjeta = cardnumber;
                
            }
            String[] szCardType = { "普通卡", "禁用卡", "黑名单卡", "夜班卡", "压力卡", "超级卡", "访客卡" };
            byte byCardType = struAcsAlarmInfo.struAcsEventInfo.byCardType;

            if (byCardType != 0 && byCardType <= szCardType.Length)
            {
                szInfoBuf = szInfoBuf + "+卡类型:" + szCardType[byCardType - 1];
            }

            if (struAcsAlarmInfo.struAcsEventInfo.dwCardReaderNo != 0)
            {
                szInfoBuf = szInfoBuf + "+读卡器编号:" + struAcsAlarmInfo.struAcsEventInfo.dwCardReaderNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwDoorNo != 0)
            {
                szInfoBuf = szInfoBuf + "+门号:" + struAcsAlarmInfo.struAcsEventInfo.dwDoorNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwVerifyNo != 0)
            {
                szInfoBuf = szInfoBuf + "+多卡认证序列号:" + struAcsAlarmInfo.struAcsEventInfo.dwVerifyNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwAlarmInNo != 0)
            {
                szInfoBuf = szInfoBuf + "+报警输入编号:" + struAcsAlarmInfo.struAcsEventInfo.dwAlarmInNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwAlarmOutNo != 0)
            {
                szInfoBuf = szInfoBuf + "+报警输出编号:" + struAcsAlarmInfo.struAcsEventInfo.dwAlarmOutNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwCaseSensorNo != 0)
            {
                szInfoBuf = szInfoBuf + "+事件触发编号:" + struAcsAlarmInfo.struAcsEventInfo.dwCaseSensorNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwRs485No != 0)
            {
                szInfoBuf = szInfoBuf + "+RS485通道编号:" + struAcsAlarmInfo.struAcsEventInfo.dwRs485No;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwMultiCardGroupNo != 0)
            {
                szInfoBuf = szInfoBuf + "+多重组合认证ID:" + struAcsAlarmInfo.struAcsEventInfo.dwMultiCardGroupNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.byCardReaderKind != 0)
            {
                szInfoBuf = szInfoBuf + "+读卡器类型:" + struAcsAlarmInfo.struAcsEventInfo.byCardReaderKind.ToString();
            }
            if (struAcsAlarmInfo.struAcsEventInfo.wAccessChannel >= 0)
            {
                szInfoBuf = szInfoBuf + "+访问通道:" + struAcsAlarmInfo.struAcsEventInfo.wAccessChannel;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.dwEmployeeNo != 0)
            {
                //employeenum = struAcsAlarmInfo.struAcsEventInfo.dwEmployeeNo.ToString();
                szInfoBuf = szInfoBuf + "+员工编号:" + struAcsAlarmInfo.struAcsEventInfo.dwEmployeeNo;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.byDeviceNo != 0)
            {
                szInfoBuf = szInfoBuf + "+设备编号:" + struAcsAlarmInfo.struAcsEventInfo.byDeviceNo.ToString();
            }
            if (struAcsAlarmInfo.struAcsEventInfo.wLocalControllerID >= 0)
            {
                szInfoBuf = szInfoBuf + "+本地控制器ID:" + struAcsAlarmInfo.struAcsEventInfo.wLocalControllerID;
            }
            if (struAcsAlarmInfo.struAcsEventInfo.byInternetAccess >= 0)
            {
                szInfoBuf = szInfoBuf + "+网络访问:" + struAcsAlarmInfo.struAcsEventInfo.byInternetAccess.ToString();
            }
            if (struAcsAlarmInfo.struAcsEventInfo.byType >= 0)
            {
                szInfoBuf = szInfoBuf + "+类型:" + struAcsAlarmInfo.struAcsEventInfo.byType.ToString();
            }
            if (struAcsAlarmInfo.struAcsEventInfo.bySwipeCardType != 0)
            {
                szInfoBuf = szInfoBuf + "+刷卡类型:" + struAcsAlarmInfo.struAcsEventInfo.bySwipeCardType.ToString();
            }
            //其它消息先不罗列了......
            /*
            if (struAcsAlarmInfo.dwPicDataLen > 0)
            {
                path = null;
                Random rand = new Random(unchecked((int)DateTime.Now.Ticks));
                path = string.Format(@"C:/Picture/ACS_LocalTime{0}_{1}.bmp", szInfo, rand.Next());
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    int iLen = (int)struAcsAlarmInfo.dwPicDataLen;
                    byte[] by = new byte[iLen];
                    Marshal.Copy(struAcsAlarmInfo.pPicData, by, 0, iLen);
                    fs.Write(by, 0, iLen);
                    fs.Close();
                }
                szInfoBuf = szInfoBuf + "SavePath:" + path;
            }*/

            //如果事件是 MINOR_FACE_VERIFY_PASS（人脸验证通过）：
            // 在lambda表达式外部获取pAlarmer的值，避免在lambda中使用ref参数
            int userId = pAlarmer.lUserID;
            this.listViewEventos.BeginInvoke(new Action(() =>
            {
                if (tipoevento == "MINOR_FACE_VERIFY_PASS")
                {
                    string nombreempleado = null;
                    if (numerotarjeta != null)
                        nombreempleado = ObtenerNombreEmpleado(numerotarjeta);
                    ListViewItem Item = new ListViewItem();
                    m_lLogNum += 1;
                    Item.Text = m_lLogNum.ToString();
                    //Item.SubItems.Add(DateTime.Now.ToString());
                    //Item.SubItems.Add(szInfoBuf);
                    Item.SubItems.Add(fecha);
                    Item.SubItems.Add(hora);
                    Item.SubItems.Add(tipoevento);
                    Item.SubItems.Add(numerotarjeta);
                    Item.SubItems.Add(nombreempleado);
                    this.listViewEventos.Items.Add(Item);

                    var device = DeviceConnectionManager.Instance.GetAllDevices().FirstOrDefault(d => d.UserID == userId);
                    if (device != null)
                    {
                        InsertarEntradaSalida(m_lLogNum.ToString(), fecha, hora, numerotarjeta, tipoevento, device.Id);
                    }
                }
                
            })); 
        }
        //根据卡号（文档号）从 empleados 表中查找员工姓名；用于显示在事件列表中。
        private string ObtenerNombreEmpleado(string documento)
        {
            string retval = null;

           
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);

            if (bd.conn != null)
            {
                string sql = "SELECT * FROM employees WHERE employee_id = @employee_id";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@employee_id", documento);                    
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        
                        while (rdr.Read())
                        {
                            retval = rdr["first_name"].ToString() + " " + rdr["last_name"].ToString();
                        }
                    }
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(bd.errormsg);
            }

            return retval;
        }
        //窗体加载时调用 Deploy() 开始监听
        private void GestionEventos_Load(object sender, EventArgs e)
        {
            // 订阅设备状态变化事件
            DeviceConnectionManager.Instance.DeviceStatusChanged += OnDeviceStatusChanged;
            
            Deploy();
        }
        
        //设备状态变化事件处理
        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            // 当设备状态发生变化时，重新部署监听
            this.Invoke(new Action(() =>
            {
                Deploy();
            }));
        }
        
        //窗体关闭前关闭报警通道 NET_DVR_CloseAlarmChan()
        private void GestionEventos_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 取消订阅设备状态变化事件
            DeviceConnectionManager.Instance.DeviceStatusChanged -= OnDeviceStatusChanged;
            
            UnDeploy();
        }
        
        //
        private void listViewEventos_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
        
    
}
