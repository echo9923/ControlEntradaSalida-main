using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEntradaSalida
{   //生成员工出入记录报表的窗体类。其核心功能是根据筛选条件查询entradas_salidas数据，生成报表数据展示，并调用报表窗体进行图形化展示
    public partial class ParamInformeEntradaSalida : Form, IRefreshableForm
    {
        private DataChangeNotifier _notifier;
        
        public bool IsFormVisible => this.Visible;
        //构造函数，初始化窗体控件。
        public ParamInformeEntradaSalida()
        {
            InitializeComponent();
        }
        //调用数据库中存储过程 generate_attendance_report()；该过程负责整理原始进出数据为报表格式，临时存入temp_attendance_report表。
        private bool GenerateAttendanceReport()
        {
            bool retval = false;

            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                try
                {
                    string sql = "CALL generate_attendance_report()";//数据库中存储过程 generate_attendance_report()
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.ExecuteNonQuery();
                    bd.desconectarMySQL();
                    retval = true;
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

        //构建最终 SQL 查询语句字符串；根据用户勾选/填写的条件筛选,查询关联表 employees 和 temp_attendance_report,按 report_date, employee_id 升序排序。
        private string GetQueryExpression()
        {
            
            string retval = null;
            retval = "SELECT temp_attendance_report.id, temp_attendance_report.employee_id, first_name, employees.last_name, temp_attendance_report.report_date, temp_attendance_report.check_in_time, temp_attendance_report.check_out_time, devices.device_name as dispositivo FROM employees, temp_attendance_report LEFT JOIN devices ON temp_attendance_report.device_id = devices.device_id WHERE employees.employee_id = temp_attendance_report.employee_id ";

            // 如果不是"所有员工"，则按员工工号筛选
            if (this.radioButtonTodosEmpleados.Checked == false)
            {
                // 如果姓名查询框为空，但工号查询框不为空，则使用工号查询
                if (string.IsNullOrEmpty(this.textBoxNombreEmpleado.Text) && !string.IsNullOrEmpty(this.textBoxDocumentoEmpleado.Text))
                {
                    retval += String.Format("AND temp_attendance_report.employee_id = '{0}' ", this.textBoxDocumentoEmpleado.Text);
                }
            }

            // 设备筛选
            ComboboxItem selectedDispositivo = (ComboboxItem)this.cmbDispositivos.SelectedItem;
            if (selectedDispositivo != null && Convert.ToInt32(selectedDispositivo.Value) != 0)
            {
                retval += String.Format("AND temp_attendance_report.device_id = {0} ", selectedDispositivo.Value);
            }
            
            // 日期范围筛选
            if (this.radioButtonTodasFechas.Checked == false)
            {
                string fechainicial = dateTimePickerFechaInicial.Value.ToString("yyyy-MM-dd");
                string fechafinal = dateTimePickerFechaFinal.Value.ToString("yyyy-MM-dd");
                retval += String.Format("AND temp_attendance_report.report_date BETWEEN CAST('{0}' AS DATE) AND CAST('{1}' AS DATE) ", fechainicial, fechafinal);
                
            }
            
            // 时间范围筛选（只有在指定时间段且时间段有效时才应用）
            if (this.radioButtonRangoHoras.Checked)
            {
                string horainicial = dateTimePickerHoraInicial.Value.ToString("HH:mm:ss");
                string horafinal = dateTimePickerHoraFinal.Value.ToString("HH:mm:ss");
                retval += String.Format("AND (temp_attendance_report.check_in_time BETWEEN CAST('{0}' AS TIME) AND CAST('{1}' AS TIME) OR temp_attendance_report.check_out_time BETWEEN CAST('{0}' AS TIME) AND CAST('{1}' AS TIME)) ", horainicial, horafinal);
            }
            
            // 姓名/工号/部门模糊查询（支持回车搜索）
            if (!string.IsNullOrEmpty(this.textBoxNombreEmpleado.Text))
            {
                retval += String.Format("AND (employees.first_name LIKE '%{0}%' OR employees.employee_id LIKE '%{0}%' OR employees.last_name LIKE '%{0}%') ", this.textBoxNombreEmpleado.Text);
            }
            
            // 部门模糊查询（支持回车搜索）
            if (!string.IsNullOrEmpty(this.textBoxApellidosEmpleado.Text))
            {
                retval += String.Format("AND employees.last_name LIKE '%{0}%' ", this.textBoxApellidosEmpleado.Text);
            }
            
            retval += "ORDER BY report_date, employee_id ASC";

            return retval;
        }
        //执行 SQL 查询；解析每一行记录为 InformeEntradaSalida 实例，并添加到列表中；同时将结果添加到 listView 控件中进行显示；如果没有记录，弹出“没有可显示的记录”。
        private bool ExecuteQuery(string sql, out List<InformeEntradaSalida> attendanceList)
        {
            attendanceList = new List<InformeEntradaSalida>();
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            DateTime date;
                            DateTime checkInTime;
                            DateTime checkOutTime;
                            string strDate;
                            string strCheckInTime;
                            string strCheckOutTime;
                            try
                            {
                                date = DateTime.Parse(rdr["report_date"].ToString());
                                strDate = date.ToString("yyyy-MM-dd");
                            }
                            catch
                            {
                                strDate = "";
                            }
                            try
                            {
                                checkInTime = DateTime.Parse(rdr["check_in_time"].ToString());
                                strCheckInTime = checkInTime.ToString("HH:mm:ss");
                            }
                            catch
                            {
                                strCheckInTime = "";
                            }
                            try
                            {
                                checkOutTime = DateTime.Parse(rdr["check_out_time"].ToString());
                                strCheckOutTime = checkOutTime.ToString("HH:mm:ss");
                            }
                            catch
                            {
                                strCheckOutTime = "";
                            }
                            

                            InformeEntradaSalida ies = new InformeEntradaSalida();
                            ies.num = rdr["id"].ToString();
                            ies.documento = rdr["employee_id"].ToString();
                            ies.nombres = rdr["first_name"].ToString();
                            ies.apellidos = rdr["last_name"].ToString();
                            ies.fecha = strDate;
                            ies.horaa = strCheckInTime;
                            ies.horab = strCheckOutTime;
                            ies.dispositivo = rdr["dispositivo"].ToString();
                            attendanceList.Add(ies);
                            ies = null;

                            ListViewItem lvi = new ListViewItem(rdr["id"].ToString());//id
                            lvi.SubItems.Add(rdr["employee_id"].ToString());//文档号
                            lvi.SubItems.Add(rdr["first_name"].ToString());//名字
                            lvi.SubItems.Add(rdr["last_name"].ToString());//姓氏
                            lvi.SubItems.Add(strDate);
                            lvi.SubItems.Add(strCheckInTime);
                            lvi.SubItems.Add(strCheckOutTime);
                            lvi.SubItems.Add(rdr["dispositivo"].ToString());
                            listView.Items.Add(lvi);
                            lvi = null;
                        }
                        rdr.Close();
                        bd.desconectarMySQL();
                        retval = true;
                    } else
                    {
                        MessageBox.Show("没有可显示的记录", "没有可显示的记录", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                    }
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
        //点击"查看报表"按钮的事件处理,调用 CrearTablaInformeES() 生成临时表；调用 ObtenerExpresionQuery() 获取 SQL 查询字符串；清空当前 listView；调用 GenerarConsulta(...) 执行 SQL 并填充界面；创建 Informe 报表窗体并传入数据，展示报表。
        private void buttonVerInforme_Click(object sender, EventArgs e)
        {
            // 检查日期范围是否合理
            if (this.dateTimePickerFechaInicial.Value > this.dateTimePickerFechaFinal.Value)
            {
                MessageBox.Show("结束日期不能早于开始日期", "日期范围错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // 检查时间范围是否合理
            if (this.radioButtonRangoHoras.Checked && 
                this.dateTimePickerHoraInicial.Value > this.dateTimePickerHoraFinal.Value)
            {
                MessageBox.Show("结束时间不能早于开始时间", "时间范围错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (GenerateAttendanceReport())
            {
                string result = GetQueryExpression().Trim();
                if (this.listView.Items.Count > 0)
                    this.listView.Items.Clear();
                List<InformeEntradaSalida> objinf = null;
                bool res = ExecuteQuery(result, out objinf);
                if (res && objinf != null)
                {
                    Informe frmInforme = new Informe();
                    frmInforme.listads = objinf;//数据源
                    frmInforme.embeddedresource = "ControlEntradaSalida.InformeEntradaSalida.rdlc";//使用的报表模板
                    frmInforme.nombredatasource = "InformeEntradaSalida";//数据集名称
                    frmInforme.Show();
                }
            }
        }
        //启用日期选择器：启用起止日期控件。
        private void radioButtonRangoFechas_Click(object sender, EventArgs e)
        {
            this.dateTimePickerFechaInicial.Enabled = true;
            this.dateTimePickerFechaFinal.Enabled = true;
        }
        //禁用日期选择器。
        private void radioButtonTodasFechas_Click(object sender, EventArgs e)
        {
            this.dateTimePickerFechaInicial.Enabled = false;
            this.dateTimePickerFechaFinal.Enabled = false;
        }
        //窗体加载事件；设置默认勾选“所有员工”
        private void ParamInformeConsumos_Load(object sender, EventArgs e)
        {
            this.radioButtonTodosEmpleados.Checked = true;//默认勾选“所有员工”
            CargarDispositivos();
            
            // 设置默认日期范围为最近7天
            this.dateTimePickerFechaInicial.Value = DateTime.Now.AddDays(-7);
            this.dateTimePickerFechaFinal.Value = DateTime.Now;
            
            // 设置时间选择器的默认值
            this.dateTimePickerHoraInicial.Value = DateTime.Today.AddHours(8); // 默认开始时间 08:00
            this.dateTimePickerHoraFinal.Value = DateTime.Today.AddHours(18);  // 默认结束时间 18:00
            
            // 初始化数据变更通知器
            _notifier = DataChangeNotifier.Instance;
            _notifier.DeviceDataChanged += OnDeviceDataChanged;
            _notifier.EmployeeDataChanged += OnEmployeeDataChanged;
        }

        private void CargarDispositivos()
        {
            this.cmbDispositivos.Items.Clear();

            ComboboxItem itemTodos = new ComboboxItem();
            itemTodos.Text = "全部";
            itemTodos.Value = 0;
            this.cmbDispositivos.Items.Add(itemTodos);

            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);

            if (bd.conn != null)
            {
                string sql = "SELECT device_id, device_name FROM devices ORDER BY device_name";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        ComboboxItem item = new ComboboxItem();
                        item.Text = rdr["device_name"].ToString();
                        item.Value = Convert.ToInt32(rdr["device_id"]);
                        this.cmbDispositivos.Items.Add(item);
                    }
                    rdr.Close();
                    this.cmbDispositivos.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    bd.desconectarMySQL();
                }
            }
        }

        //启用时间选择器（起始与结束时间）
        private void radioButtonRangoHoras_CheckedChanged(object sender, EventArgs e)
        {
            this.dateTimePickerHoraInicial.Enabled = this.radioButtonRangoHoras.Checked;
            this.dateTimePickerHoraFinal.Enabled = this.radioButtonRangoHoras.Checked;
        }
        //禁用时间选择器。
        private void radioButtonTodasHoras_CheckedChanged(object sender, EventArgs e)
        {
            this.dateTimePickerHoraInicial.Enabled = false;
            this.dateTimePickerHoraFinal.Enabled = false;
        }
        //用户手动填写文档号时，自动取消“所有员工”选项。
        private void textBoxDocumentoEmpleado_Click(object sender, EventArgs e)
        {
            this.radioButtonTodosEmpleados.Checked = false;
        }
        
        //员工工号文本框按下回车键时触发查询
        private void textBoxDocumentoEmpleado_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                buttonVerInforme_Click(sender, e);
            }
        }
        
        //姓名查询文本框按下回车键时触发查询
        private void textBoxNombreEmpleado_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                buttonVerInforme_Click(sender, e);
            }
        }
        //当用户选择“所有员工”时，清空文档号、姓名、姓氏文本框。
        private void radioButtonTodosEmpleados_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButtonTodosEmpleados.Checked)
            {
                this.textBoxDocumentoEmpleado.Text = "";
                this.textBoxNombreEmpleado.Text = "";
                this.textBoxApellidosEmpleado.Text = "";
            }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void labelDispositivo_Click(object sender, EventArgs e)
        {

        }
        
        #region IRefreshableForm 实现
        
        /// <summary>
        /// 刷新设备数据
        /// </summary>
        public void RefreshDeviceData()
        {
            SafeUIUpdater.UpdateUI(this, () => 
            {
                string selectedDeviceValue = GetSelectedDeviceValue();
                CargarDispositivos();
                RestoreSelectedDevice(selectedDeviceValue);
            });
        }
        
        /// <summary>
        /// 刷新员工数据
        /// </summary>
        public void RefreshEmployeeData()
        {
            // 报表参数界面不需要刷新员工数据（没有员工下拉列表）
        }
        
        /// <summary>
        /// 刷新门状态（报表参数界面不需要）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">门状态</param>
        public void RefreshDoorStatus(string deviceId, DoorStatus status)
        {
            // 报表参数界面不需要刷新门状态
        }
        
        #endregion
        
        #region 数据变更事件处理
        
        /// <summary>
        /// 处理设备数据变更事件
        /// </summary>
        private void OnDeviceDataChanged(object sender, DeviceDataChangedEventArgs e)
        {
            if (this.IsFormVisible)
            {
                RefreshDeviceData();
            }
        }
        
        /// <summary>
        /// 处理员工数据变更事件
        /// </summary>
        private void OnEmployeeDataChanged(object sender, EmployeeDataChangedEventArgs e)
        {
            // 报表参数界面不需要处理员工数据变更
        }
        
        /// <summary>
        /// 获取当前选中的设备值
        /// </summary>
        /// <returns>设备值</returns>
        private string GetSelectedDeviceValue()
        {
            if (cmbDispositivos.SelectedItem is ComboboxItem selectedItem)
            {
                return selectedItem.Value?.ToString();
            }
            return "0";
        }
        
        /// <summary>
        /// 恢复之前选中的设备
        /// </summary>
        /// <param name="deviceValue">设备值</param>
        private void RestoreSelectedDevice(string deviceValue)
        {
            try
            {
                foreach (ComboboxItem item in cmbDispositivos.Items)
                {
                    if (item.Value?.ToString() == deviceValue)
                    {
                        cmbDispositivos.SelectedItem = item;
                        break;
                    }
                }
            }
            catch
            {
                // 如果恢复失败，选择默认的第一个选项
                if (cmbDispositivos.Items.Count > 0)
                {
                    cmbDispositivos.SelectedIndex = 0;
                }
            }
        }
        
        #endregion
        
        #region 窗体关闭处理
        
        /// <summary>
        /// 窗体关闭时取消事件订阅
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 取消事件订阅，防止内存泄漏
            if (_notifier != null)
            {
                _notifier.DeviceDataChanged -= OnDeviceDataChanged;
                _notifier.EmployeeDataChanged -= OnEmployeeDataChanged;
            }
            
            base.OnFormClosed(e);
        }

        #endregion

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }
    }
}
