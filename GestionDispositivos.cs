using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{   //设备管理界面的功能
    public partial class GestionDispositivos : Form, IRefreshableForm
    {
        private DataChangeNotifier _notifier;
        
        public bool IsFormVisible => this.Visible;
        
        public GestionDispositivos()
        {
            InitializeComponent();
        }
        //查询指定设备 ID 的密码,从数据库读取
        private string GetDevicePassword(string deviceId)
        {
            string password = null;
            Common cmn = new Common();
            String connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();//连接数据库
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "SELECT password FROM devices WHERE device_id = @device_id";//数据库操作语句
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@device_id", deviceId);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {

                        while (rdr.Read())
                        {
                            password = rdr["password"].ToString();//查询密码
                        }
                    }
                    rdr.Close();
                    bd.desconectarMySQL();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            return password;
        }
        //删除指定设备 ID 的记录,从数据库中删除
        private bool DeleteDevice(string deviceId)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "DELETE FROM devices WHERE device_id = @device_id";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@device_id", deviceId);                    
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
        //从数据库中查询所有设备，显示列表
        private void LoadDevices()
        {
            Common cmn = new Common();
            String connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);//连接数据库
            if (bd.conn != null)
            {
                string sql = "SELECT * FROM devices";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        listView.Items.Clear();
                        while (rdr.Read())
                        {
                            ListViewItem lvi = new ListViewItem(rdr["device_id"].ToString());//设备id
                            lvi.SubItems.Add(rdr["device_name"].ToString());//设备名称
                            lvi.SubItems.Add(rdr["description"].ToString());//设备类别
                            lvi.SubItems.Add(rdr["ip_address"].ToString());//设备ip
                            lvi.SubItems.Add(rdr["port"].ToString());//设备端口
                            lvi.SubItems.Add(rdr["username"].ToString());//设备用户
                            // 检查当前设备是否是已连接的设备
                            string currentIp = rdr["ip_address"].ToString();
                            string currentPort = rdr["port"].ToString();
                            bool isConnected = false;
                            
                            // 使用设备连接管理器检查设备连接状态
                            var device = DeviceConnectionManager.Instance.GetDeviceByAddress(currentIp, currentPort);
                            if (device != null)
                            {
                                isConnected = device.IsConnected;
                            }
                            
                            if (isConnected)
                            {
                                lvi.SubItems.Add("已连接");
                            } else
                            {
                                lvi.SubItems.Add("未连接");
                            }
                            lvi.SubItems.Add(rdr["status"].ToString());//状态
                            lvi.SubItems.Add(rdr["is_default"].ToString());//默认状态
                            lvi.SubItems.Add(rdr["last_used_time"].ToString());//最后一次登录时间
                            listView.Items.Add(lvi);
                            lvi = null;
                        }
                    }
                    rdr.Close();
                    bd.desconectarMySQL();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        //窗体加载时：加载所有设备信息
        private void GestionDispositivos_Load(object sender, EventArgs e)
        {
            LoadDevices();
            
            // 订阅数据变更事件
            _notifier = DataChangeNotifier.Instance;
            _notifier.DeviceDataChanged += OnDeviceDataChanged;
            _notifier.EmployeeDataChanged += OnEmployeeDataChanged;
        }
        //双击列表中的某个设备：查看或修改设备信息
        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count != 0)
            {
                ListView.SelectedIndexCollection indexes = this.listView.SelectedIndices;
                foreach (int index in indexes)
                {
                    
                    string id = this.listView.Items[index].Text;
                    string nombre = this.listView.Items[index].SubItems[1].Text;
                    string categoria = this.listView.Items[index].SubItems[2].Text;
                    string ip = this.listView.Items[index].SubItems[3].Text;
                    string puerto = this.listView.Items[index].SubItems[4].Text;
                    string usuario = this.listView.Items[index].SubItems[5].Text;
                    string password = GetDevicePassword(this.listView.Items[index].Text);
                    string activo = this.listView.Items[index].SubItems[7].Text;
                    string predeterminado = this.listView.Items[index].SubItems[8].Text;
                    string ultimavez = this.listView.Items[index].SubItems[9].Text;

                    LoginDevice frmLoginDevice = new LoginDevice();
                    frmLoginDevice.nuevo = false;
                    frmLoginDevice.id = id;
                    frmLoginDevice.nombre = nombre;
                    frmLoginDevice.descripcion = categoria;
                    frmLoginDevice.ip = ip;
                    frmLoginDevice.puerto = puerto;
                    frmLoginDevice.usuario = usuario;
                    frmLoginDevice.password = password;
                    frmLoginDevice.activo = activo;
                    frmLoginDevice.predeterminado = predeterminado;
                    frmLoginDevice.ultimavez = ultimavez;
                    frmLoginDevice.ShowDialog(this);
                    // 设备连接状态已在 LoadDevices 中正确处理
                    LoadDevices();
                    
                    // 通知其他界面设备数据已变更
                    _notifier?.NotifyDeviceDataChanged(
                        id, 
                        $"{nombre} - {ip}",
                        DeviceChangeType.Updated,
                        this.GetType().Name);

                }
            }
        }
        //添加新设备
        private void buttonNuevo_Click(object sender, EventArgs e)
        {
            LoginDevice frmLoginDevice = new LoginDevice();// LoginDevice窗口登录设备
            frmLoginDevice.nuevo = true;
            frmLoginDevice.ShowDialog(this);
            LoadDevices();
            // 新设备连接状态已在 LoadDevices 中正确处理
            
            // 通知其他界面有新设备添加
            _notifier?.NotifyDeviceDataChanged(
                "新设备", 
                "设备列表已更新",
                DeviceChangeType.Added,
                this.GetType().Name);
        }
        //删除设备
        private void buttonEliminar_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show("您必须从列表中至少选择一个设备", "删除设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            } else
            {
                DialogResult res = MessageBox.Show("您确定要删除所选设备吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    ListView.SelectedIndexCollection indexes = this.listView.SelectedIndices;
                    string deviceId = this.listView.Items[indexes[0]].Text;
                    string deviceInfo = $"{this.listView.Items[indexes[0]].SubItems[1].Text} - {this.listView.Items[indexes[0]].SubItems[3].Text}";
                    if (DeleteDevice(deviceId))
                    {
                        LoadDevices();//刷新列表
                        
                        // 通知其他界面设备已删除
                        _notifier?.NotifyDeviceDataChanged(
                            deviceId, 
                            deviceInfo,
                            DeviceChangeType.Deleted,
                            this.GetType().Name);
                    }
                }
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
                LoadDevices();
            });
        }
        
        /// <summary>
        /// 刷新员工数据（设备管理界面不需要）
        /// </summary>
        public void RefreshEmployeeData()
        {
            // 设备管理界面不需要刷新员工数据
        }
        
        /// <summary>
        /// 刷新门状态（在设备列表中更新状态显示）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">门状态</param>
        public void RefreshDoorStatus(string deviceId, DoorStatus status)
        {
            SafeUIUpdater.UpdateUI(this, () => 
            {
                UpdateDeviceStatusInGrid(deviceId, status);
            });
        }
        
        #endregion
        
        #region 事件处理方法
        
        /// <summary>
        /// 处理设备数据变更事件
        /// </summary>
        private void OnDeviceDataChanged(object sender, DeviceDataChangedEventArgs e)
        {
            // 避免自己触发的事件导致重复刷新
            if (e.Source == this.GetType().Name) return;
            
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
            // 设备管理界面不需要处理员工数据变更
        }
        
        /// <summary>
        /// 更新设备列表中的状态显示
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">门状态</param>
        private void UpdateDeviceStatusInGrid(string deviceId, DoorStatus status)
        {
            try
            {
                foreach (ListViewItem item in listView.Items)
                {
                    if (item.Text == deviceId)
                    {
                        // 更新门状态显示（在现有状态列中添加门状态信息）
                        string doorStatusText = GetDoorStatusDisplayText(status);
                        if (item.SubItems.Count > 7)
                        {
                            string currentStatus = item.SubItems[7].Text;
                            item.SubItems[7].Text = $"{currentStatus} | {doorStatusText}";
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新设备状态显示异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取门状态显示文本
        /// </summary>
        /// <param name="status">门状态</param>
        /// <returns>显示文本</returns>
        private string GetDoorStatusDisplayText(DoorStatus status)
        {
            return status switch
            {
                DoorStatus.Opened => "门已开启",
                DoorStatus.Closed => "门已关闭",
                DoorStatus.AlwaysOpen => "门常开",
                DoorStatus.AlwaysClosed => "门常闭",
                _ => "门状态未知"
            };
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
    }
}

