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
        public string contrasena = null;//密码
        public string ultimavez = null;//最后登录时间
        public string predeterminado = null;//是否默认
        public string activo = null;//是否启用
        //初始化窗体
        public LoginDevice()
        {
            InitializeComponent();   
        }
        //取消按钮事件，关闭窗口
        private void buttonCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //窗体加载时，如果已有设备信息（不是新增），则填充表单字段；根据状态字段显示“是否默认设备”和“是否启用”
        private void LoginDevice_Load(object sender, EventArgs e)
        {
            if (this.id != null && this.nombre != null && this.ip != null && this.puerto != null && this.usuario != null && this.contrasena != null )
            {
                this.textBoxID.Text = this.id.ToString();
                this.textBoxNombre.Text = this.nombre.ToString();
                if (this.descripcion != null)
                    this.textBoxDescripcion.Text = this.descripcion.ToString().ToUpper();
                else
                    this.textBoxDescripcion.Text = "";
                if (this.activo.ToString() == "1")
                    this.checkBoxEstado.Checked = true;
                if (this.predeterminado.ToString() == "1")
                    this.checkBoxPredeterminado.Checked = true;

                
                
                this.txtDireccionIP.Text = this.ip.ToString();
                this.txtPuerto.Text = this.puerto.ToString();
                this.txtUsuario.Text = this.usuario.ToString();
                this.txtContrasena.Text = this.contrasena.ToString();
                this.textBoxUltimaVez.Text = this.ultimavez.ToString();

            } 
       
        }

        //添加新设备到数据库,保存设备的所有字段,添加当前时间为 created 和 lastimeused 字段
        private bool InsertarDispositivo()
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "INSERT INTO dispositivos (nombre, " +
                    "descripcion, direccionip, puerto, usuario, " +
                    "contrasena, estado, predeterminado, lastimeused, created)" +
                    "VALUES (@nombre, @descripcion, @direccionip, @puerto, " +
                    "@usuario, @contrasena, @estado, @predeterminado, @lastimeused, @created)";//SQL语句，插入数据库
                try
                {
                    int estado = 0;
                    int predeterminado = 0;
                    if (this.checkBoxEstado.Checked)
                        estado = 1;
                    if (this.checkBoxPredeterminado.Checked)
                    {
                        predeterminado = 1;
                        ActualizarPredeterminado();
                    }


                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@nombre", this.textBoxNombre.Text);//名称
                    cmd.Parameters.AddWithValue("@descripcion", this.textBoxDescripcion.Text);//描述
                    cmd.Parameters.AddWithValue("@direccionip", this.txtDireccionIP.Text);//IP
                    cmd.Parameters.AddWithValue("@puerto", this.txtPuerto.Text);//端口
                    cmd.Parameters.AddWithValue("@usuario", this.txtUsuario.Text);//用户
                    cmd.Parameters.AddWithValue("@contrasena", this.txtContrasena.Text);//密码
                    cmd.Parameters.AddWithValue("@estado", estado);//状态
                    cmd.Parameters.AddWithValue("@predeterminado", predeterminado);//是否默认
                    cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@lastimeused", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
        //设置默认设备,将数据库dispositivos表中所有predeterminado=1的记录设置为0,保证数据库中只有一个默认设备。
        private bool ActualizarPredeterminado()
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "UPDATE dispositivos " +
                    "SET predeterminado = 0 " +                    
                    "WHERE predeterminado = 1";
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
        private bool ActualizarDispositivo(string id)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "UPDATE dispositivos SET nombre = @nombre, " +
                    "descripcion = @descripcion, " +
                    "direccionip = @direccionip, " +
                    "puerto =  @puerto, " +
                    "usuario = @usuario, " +
                    "contrasena = @contrasena," +
                    "estado = @estado, " +
                    "predeterminado = @predeterminado, " +
                    "modified = @modified," +
                    "lastimeused = @lasttime " +
                    "WHERE id = @id";
                try
                {
                    int estado = 0;
                    int predeterminado = 0;
                    if (this.checkBoxEstado.Checked)
                        estado = 1;
                    if (this.checkBoxPredeterminado.Checked)
                    {
                        predeterminado = 1;
                        ActualizarPredeterminado();
                    }

                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@nombre", this.textBoxNombre.Text);//名称
                    cmd.Parameters.AddWithValue("@descripcion", this.textBoxDescripcion.Text);//描述
                    cmd.Parameters.AddWithValue("@direccionip", this.txtDireccionIP.Text);//IP
                    cmd.Parameters.AddWithValue("@puerto", this.txtPuerto.Text);//端口
                    cmd.Parameters.AddWithValue("@usuario", this.txtUsuario.Text);//用户
                    cmd.Parameters.AddWithValue("@contrasena", this.txtContrasena.Text);//密码
                    cmd.Parameters.AddWithValue("@estado", estado);//状态
                    cmd.Parameters.AddWithValue("@predeterminado", predeterminado);//是否默认
                    cmd.Parameters.AddWithValue("@modified", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@lasttime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//最后登录时间
                    cmd.Parameters.AddWithValue("@id", this.textBoxID.Text);//编号
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
        //使用当前表单中填写的 IP、端口、用户名、密码连接设备,调用 Common.Login封装方法登录设备；成功后保存这些信息到 Common 类的静态字段
        private bool login()
        {
            bool retval = false;

            Common cmn = new Common();
            string msg = null;
            bool ret = false;
            ret = cmn.Login(this.txtDireccionIP.Text,
                this.txtPuerto.Text,
                this.txtUsuario.Text,
                this.txtContrasena.Text, out msg);
            if (!ret)
            {
                MessageBox.Show(msg,"登录错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Common.ip = this.txtDireccionIP.Text;
                Common.puerto = this.txtPuerto.Text;      
                Common.usuario = this.txtUsuario.Text;
                Common.contrasena = this.txtContrasena.Text;
                retval = true;                
            }
            return retval;
        }
        //登录按钮点击事件,成功登录后调用 InsertarDispositivo() 添加
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            if (nuevo)
            {
                if (login())
                {
                    InsertarDispositivo();
                }
                else 
                {
                    DialogResult res = MessageBox.Show("未能在设备上登录，您还是想要添加它吗？", "设备登录错误", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (res == DialogResult.Yes)
                    {
                        InsertarDispositivo();
                    }

                }

            } 
            else
            {
                login();
                ActualizarDispositivo(this.textBoxID.Text);
            }
            this.Close();
        }

        private void txtPuerto_Enter(object sender, EventArgs e)
        {
            
        }
    }
}
