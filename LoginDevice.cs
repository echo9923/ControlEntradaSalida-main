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
{   // 登录、添加和编辑设备信息 ,管理设备的连接配置
    public partial class LoginDevice : Form
    {
        public bool nuevo = false;//是否为新增设备
        public string id = null;//ID
        public string nombre = null;//名称
        public string descripcion = null;//描述
        public string ip = null;//ip
        public string puerto = null;//端口
        public string usuario = null;//用户名
        public string password = null;//密码
        public string ultimavez = null;//最后登录时间
        public string predeterminado = null;//是否默认
        public string activo = null;//是否启用
        //初始化窗体
        public LoginDevice()
        {
            InitializeComponent();
            
            // 确保子窗口始终显示在最上层
            this.TopMost = true;
            this.ShowInTaskbar = false; // 不在任务栏显示
        }
        //取消按钮事件，关闭窗口
        private void buttonCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //窗体加载时，如果已有设备信息（不是新增），则填充表单字段；根据状态字段显示“是否默认设备”和“是否启用”
        private void LoginDevice_Load(object sender, EventArgs e)
        {
            // 确保窗口获得焦点并显示在最前面
            this.Activate();
            this.BringToFront();
            this.Focus();
            
            if (this.id != null && this.nombre != null && this.ip != null && this.puerto != null && this.usuario != null && this.password != null )
            {
                this.textBoxID.Text = this.id.ToString();
                this.textBoxNombre.Text = this.nombre.ToString();
                if (this.descripcion != null)
                {
                    string categoria = this.descripcion.ToString().ToUpper();
                    // 尝试在下拉框中选择对应的类别
                    int index = this.comboBoxCategoria.FindStringExact(categoria);
                    if (index >= 0)
                        this.comboBoxCategoria.SelectedIndex = index;
                    else
                        this.comboBoxCategoria.SelectedIndex = 0; // 默认选择第一个
                }
                else
                {
                    this.comboBoxCategoria.SelectedIndex = 0; // 默认选择第一个
                }
                if (this.activo.ToString() == "1")
                    this.checkBoxEstado.Checked = true;
                if (this.predeterminado.ToString() == "1")
                    this.checkBoxPredeterminado.Checked = true;

                
                
                this.txtDireccionIP.Text = this.ip.ToString();
                this.txtPuerto.Text = this.puerto.ToString();
                this.txtUsuario.Text = this.usuario.ToString();
                this.txtContrasena.Text = this.password.ToString();
                this.textBoxUltimaVez.Text = this.ultimavez.ToString();

            } 
       
        }

        //添加新设备到数据库,保存设备的所有字段,添加当前时间为 created 和 lastimeused 字段
        private bool AddDevice()
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "INSERT INTO devices (device_name, " +
                    "description, ip_address, port, username, " +
                    "password, status, is_default, last_used_time, created_at)" +
                    "VALUES (@device_name, @description, @ip_address, @port, " +
                    "@username, @password, @status, @is_default, @last_used_time, @created_at)";//SQL语句，插入数据库
                try
                {
                    int status = 0;
                    int isDefault = 0;
                    if (this.checkBoxEstado.Checked)
                        status = 1;
                    if (this.checkBoxPredeterminado.Checked)
                    {
                        isDefault = 1;
                        UpdateDefaultDevice();
                    }


                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@device_name", this.textBoxNombre.Text);//名称
                    cmd.Parameters.AddWithValue("@description", this.comboBoxCategoria.Text);//类别
                    cmd.Parameters.AddWithValue("@ip_address", this.txtDireccionIP.Text);//IP
                    cmd.Parameters.AddWithValue("@port", this.txtPuerto.Text);//端口
                    cmd.Parameters.AddWithValue("@username", this.txtUsuario.Text);//用户
                    cmd.Parameters.AddWithValue("@password", this.txtContrasena.Text);//密码
                    cmd.Parameters.AddWithValue("@status", status);//状态
                    cmd.Parameters.AddWithValue("@is_default", isDefault);//是否默认
                    cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@last_used_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
        //设置默认设备,将数据库devices表中所有is_default=1的记录设置为0,保证数据库中只有一个默认设备。
        private bool UpdateDefaultDevice()
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "UPDATE devices " +
                    "SET is_default = 0 " +                    
                    "WHERE is_default = 1";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);                   
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
        //更新数据库设备记录；包括名称、描述、地址、端口、用户信息、状态、是否默认等；
        private bool UpdateDevice(string deviceId)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "UPDATE devices SET device_name = @device_name, " +
                    "description = @description, " +
                    "ip_address = @ip_address, " +
                    "port =  @port, " +
                    "username = @username, " +
                    "password = @password," +
                    "status = @status, " +
                    "is_default = @is_default, " +
                    "updated_at = @updated_at," +
                    "last_used_time = @last_used_time " +
                    "WHERE device_id = @device_id";
                try
                {
                    int status = 0;
                    int isDefault = 0;
                    if (this.checkBoxEstado.Checked)
                        status = 1;
                    if (this.checkBoxPredeterminado.Checked)
                    {
                        isDefault = 1;
                        UpdateDefaultDevice();
                    }

                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@device_name", this.textBoxNombre.Text);//名称
                    cmd.Parameters.AddWithValue("@description", this.comboBoxCategoria.Text);//类别
                    cmd.Parameters.AddWithValue("@ip_address", this.txtDireccionIP.Text);//IP
                    cmd.Parameters.AddWithValue("@port", this.txtPuerto.Text);//端口
                    cmd.Parameters.AddWithValue("@username", this.txtUsuario.Text);//用户
                    cmd.Parameters.AddWithValue("@password", this.txtContrasena.Text);//密码
                    cmd.Parameters.AddWithValue("@status", status);//状态
                    cmd.Parameters.AddWithValue("@is_default", isDefault);//是否默认
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@last_used_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//最后登录时间
                    cmd.Parameters.AddWithValue("@device_id", this.textBoxID.Text);//编号
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
        //使用当前表单中填写的 IP、端口、用户名、密码连接设备,调用 Common.Login封装方法登录设备
        private bool login(out int userID)
        {
            bool retval = false;
            userID = -1;

            Common cmn = new Common();
            string msg = null;
            int lUserID = -1;
            bool ret = false;
            ret = cmn.Login(this.txtDireccionIP.Text,
                this.txtPuerto.Text,
                this.txtUsuario.Text,
                this.txtContrasena.Text, out lUserID, out msg);
            if (!ret)
            {
                MessageBox.Show(msg,"登录错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                userID = lUserID;
                retval = true;                
            }
            return retval;
        }
        //登录按钮点击事件,成功登录后调用 InsertarDispositivo() 添加
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            int userID = -1;
            bool operationSucceeded = false;
            string deviceInfo = $"{this.textBoxNombre.Text} - {this.txtDireccionIP.Text}";
            
            if (nuevo)
            {
                if (login(out userID))
                {
                    operationSucceeded = AddDevice();
                    // 如果登录成功，断开连接（因为设备连接管理器会管理连接）
                    if (userID >= 0)
                    {
                        HCNetSDK.NET_DVR_Logout_V30(userID);
                    }
                }
                else 
                {
                    DialogResult res = MessageBox.Show("未能在设备上登录，您还是想要添加它吗？", "设备登录错误", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (res == DialogResult.Yes)
                    {
                        operationSucceeded = AddDevice();
                    }
                }
                
                // 如果成功添加设备，通知其他界面
                if (operationSucceeded)
                {
                    DataChangeNotifier.Instance.NotifyDeviceDataChanged(
                        "", // 新设备还没有ID
                        deviceInfo,
                        DeviceChangeType.Added,
                        this.GetType().Name);
                }
            } 
            else
            {
                login(out userID);
                operationSucceeded = UpdateDevice(this.textBoxID.Text);
                // 如果登录成功，断开连接（因为设备连接管理器会管理连接）
                if (userID >= 0)
                {
                    HCNetSDK.NET_DVR_Logout_V30(userID);
                }
                
                // 如果成功更新设备，通知其他界面
                if (operationSucceeded)
                {
                    DataChangeNotifier.Instance.NotifyDeviceDataChanged(
                        this.textBoxID.Text,
                        deviceInfo,
                        DeviceChangeType.Updated,
                        this.GetType().Name);
                }
            }
            
            if (operationSucceeded)
            {
                this.Close();
            }
        }

        private void txtPuerto_Enter(object sender, EventArgs e)
        {
            
        }
    }
}
