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
{   //门禁系统中的一个事件报表参数选择与生成模块，功能类似于 ParamInformeEntradaSalida.cs，但它针对的是原始事件数据（如刷卡、人脸识别、门禁动作等）表 access_logs，而非整理后的进出时间段。
    /*提供 UI 选择查询条件（员工、日期、时间等）；
    构建 SQL 查询语句；
    查询 access_logs 表与 employees 联表数据；
    显示在界面 ListView 中；
    打开报表窗口 Informe 显示 InformeEventos 报表。
     */
    public partial class ParamInformeEventos : Form, IRefreshableForm
    {
        private DataChangeNotifier _notifier;
        
        public bool IsFormVisible => this.Visible;
        //构造函数，初始化窗体控件
        public ParamInformeEventos()
        {
            InitializeComponent();
        }

        //构建最终 SQL 查询语句字符串；根据用户勾选/填写的条件筛选,查询关联表 employees 和 access_logs,按 log_date, log_time 升序排序。
        private string GetQueryExpression()
        {
            
            string retval = null;
            retval = "SELECT access_logs.log_number, employees.employee_id, employees.first_name, employees.last_name, access_logs.log_date, access_logs.log_time, devices.device_name as dispositivo FROM employees, access_logs LEFT JOIN devices ON access_logs.device_id = devices.device_id WHERE employees.employee_id = access_logs.employee_id ";

            // 如果不是"所有员工"，则按员工工号筛选
            if (this.radioButtonTodosEmpleados.Checked == false)
            {
                // 如果姓名查询框为空，但工号查询框不为空，则使用工号查询
                if (string.IsNullOrEmpty(this.textBoxNombreEmpleado.Text) && !string.IsNullOrEmpty(this.textBoxDocumentoEmpleado.Text))
                {
                    retval += String.Format("AND employees.employee_id = '{0}' ", this.textBoxDocumentoEmpleado.Text);
                }
            }

            // 设备筛选
            ComboboxItem selectedDispositivo = (ComboboxItem)this.cmbDispositivos.SelectedItem;
            if (selectedDispositivo != null && Convert.ToInt32(selectedDispositivo.Value) != 0)
            {
                retval += String.Format("AND access_logs.device_id = {0} ", selectedDispositivo.Value);
            }
            
            // 日期范围筛选
            if (this.radioButtonTodasFechas.Checked == false)
            {
                string fechainicial = dateTimePickerFechaInicial.Value.ToString("yyyy-MM-dd");
                string fechafinal = dateTimePickerFechaFinal.Value.ToString("yyyy-MM-dd");
                retval += String.Format("AND access_logs.log_date BETWEEN CAST('{0}' AS DATE) AND CAST('{1}' AS DATE) ", fechainicial, fechafinal);
                
            }
            
            // 时间范围筛选（只有在指定时间段且时间段有效时才应用）
            if (this.radioButtonRangoHoras.Checked)
            {
                string horainicial = dateTimePickerHoraInicial.Value.ToString("HH:mm:ss");
                string horafinal = dateTimePickerHoraFinal.Value.ToString("HH:mm:ss");
                retval += String.Format("AND access_logs.log_time BETWEEN CAST('{0}' AS TIME) AND CAST('{1}' AS TIME) ", horainicial, horafinal);
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
            
            retval += "ORDER BY log_date, log_time, employee_id ASC";

            return retval;
        }
        //执行 SQL 查询；解析每一行记录为 InformeEventos 实例，并添加到列表中；同时将结果添加到 listView 控件中进行显示；如果没有记录，弹出"没有可显示的记录"。
        private bool ExecuteQuery(string sql, out List<InformeEventos> eventList)
        {
            eventList = new List<InformeEventos>();
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
                            DateTime time;
                            string strDate;
                            string strTime;
                            try
                            {
                                date = DateTime.Parse(rdr["log_date"].ToString());
                                strDate = date.ToString("yyyy-MM-dd");
                            }
                            catch
                            {
                                strDate = "";
                            }
                            try
                            {
                                time = DateTime.Parse(rdr["log_time"].ToString());
                                strTime = time.ToString("HH:mm:ss");
                            }
                            catch
                            {
                                strTime = "";
                            }

                            InformeEventos ie = new InformeEventos();
                            ie.num = rdr["log_number"].ToString();
                            ie.documento = rdr["employee_id"].ToString();
                            ie.nombres = rdr["first_name"].ToString();
                            ie.apellidos = rdr["last_name"].ToString();
                            ie.fecha = strDate;
                            ie.hora = strTime;
                            ie.dispositivo = rdr["dispositivo"].ToString();
                            eventList.Add(ie);
                            ie = null;

                            ListViewItem lvi = new ListViewItem(rdr["log_number"].ToString());//编号
                            lvi.SubItems.Add(rdr["employee_id"].ToString());//文档号
                            lvi.SubItems.Add(rdr["first_name"].ToString());//名字
                            lvi.SubItems.Add(rdr["last_name"].ToString());//姓氏
                            lvi.SubItems.Add(strDate);
                            lvi.SubItems.Add(strTime);
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
        //点击"查看报表"按钮的事件处理,调用 GetQueryExpression() 获取 SQL 查询字符串；清空当前 listView；调用 ExecuteQuery(...) 执行 SQL 并填充界面；创建 Informe 报表窗体并传入数据，展示报表。
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
            
            string result = GetQueryExpression().Trim();
            if (this.listView.Items.Count > 0)
                this.listView.Items.Clear();
            List<InformeEventos> objinf = null;
            bool res = ExecuteQuery(result, out objinf);
            if (res && objinf != null)
            {
                Informe frmInforme = new Informe();
                frmInforme.listads = objinf;//数据源
                frmInforme.embeddedresource = "ControlEntradaSalida.InformeEventos.rdlc";//使用的报表模板
                frmInforme.nombredatasource = "InformeEventos";//数据集名称
                frmInforme.Show();
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
        //窗体加载事件；设置默认勾选"所有员工"
        private void ParamInformeEventos_Load(object sender, EventArgs e)
        {
            this.radioButtonTodosEmpleados.Checked = true;//默认勾选"所有员工"
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
        //用户手动填写文档号时，自动取消"所有员工"选项。
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
        //当用户选择"所有员工"时，清空文档号、姓名、姓氏文本框。
        private void radioButtonTodosEmpleados_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButtonTodosEmpleados.Checked)
            {
                this.textBoxDocumentoEmpleado.Text = "";
                this.textBoxNombreEmpleado.Text = "";
                this.textBoxApellidosEmpleado.Text = "";
            }
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

    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}