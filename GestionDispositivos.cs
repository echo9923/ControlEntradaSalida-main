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
    public partial class GestionDispositivos : Form
    {
        public GestionDispositivos()
        {
            InitializeComponent();
        }
        //查询指定设备 ID 的密码,从数据库读取
        private string ConsultarContrasenaDispositivo(string id)
        {
            string contrasena = null;
            Common cmn = new Common();
            String connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();//连接数据库
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "SELECT contrasena FROM dispositivos WHERE id = @id";//数据库操作语句
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {

                        while (rdr.Read())
                        {
                            contrasena = rdr["contrasena"].ToString();//查询密码
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
            return contrasena;
        }
        //删除指定设备 ID 的记录,从数据库中删除
        private bool EliminarDispositivo(string id)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "DELETE FROM dispositivos WHERE id = @id";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@id", id);                    
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
        private void ConsultarDispositivos()
        {
            Common cmn = new Common();
            String connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);//连接数据库
            if (bd.conn != null)
            {
                string sql = "SELECT * FROM dispositivos";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        listView.Items.Clear();
                        while (rdr.Read())
                        {
                            ListViewItem lvi = new ListViewItem(rdr["id"].ToString());//设备id
                            lvi.SubItems.Add(rdr["nombre"].ToString());//设备名称
                            lvi.SubItems.Add(rdr["descripcion"].ToString());//设备描述
                            lvi.SubItems.Add(rdr["direccionip"].ToString());//设备ip
                            lvi.SubItems.Add(rdr["puerto"].ToString());//设备端口
                            lvi.SubItems.Add(rdr["usuario"].ToString());//设备用户
                            // 检查当前设备是否是已连接的设备
                            string currentIp = rdr["direccionip"].ToString();
                            string currentPort = rdr["puerto"].ToString();
                            bool isConnected = false;
                            
                            // 如果有设备连接，检查是否是当前设备
                            if (Common.m_UserID >= 0 && !string.IsNullOrEmpty(Common.ip) && !string.IsNullOrEmpty(Common.puerto))
                            {
                                isConnected = (Common.ip == currentIp && Common.puerto == currentPort);
                            }
                            
                            if (isConnected)
                            {
                                lvi.SubItems.Add("Conectado");
                            } else
                            {
                                lvi.SubItems.Add("Desconectado");
                            }
                            lvi.SubItems.Add(rdr["estado"].ToString());//状态
                            lvi.SubItems.Add(rdr["predeterminado"].ToString());//默认状态
                            lvi.SubItems.Add(rdr["lastimeused"].ToString());//最后一次登录时间
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
            ConsultarDispositivos();
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
                    string descripcion = this.listView.Items[index].SubItems[2].Text;
                    string ip = this.listView.Items[index].SubItems[3].Text;
                    string puerto = this.listView.Items[index].SubItems[4].Text;
                    string usuario = this.listView.Items[index].SubItems[5].Text;
                    string contrasena = ConsultarContrasenaDispositivo(this.listView.Items[index].Text);
                    string activo = this.listView.Items[index].SubItems[7].Text;
                    string predeterminado = this.listView.Items[index].SubItems[8].Text;
                    string ultimavez = this.listView.Items[index].SubItems[9].Text;

                    LoginDevice frmLoginDevice = new LoginDevice();
                    frmLoginDevice.nuevo = false;
                    frmLoginDevice.id = id;
                    frmLoginDevice.nombre = nombre;
                    frmLoginDevice.descripcion = descripcion;
                    frmLoginDevice.ip = ip;
                    frmLoginDevice.puerto = puerto;
                    frmLoginDevice.usuario = usuario;
                    frmLoginDevice.contrasena = contrasena;
                    frmLoginDevice.activo = activo;
                    frmLoginDevice.predeterminado = predeterminado;
                    frmLoginDevice.ultimavez = ultimavez;
                    frmLoginDevice.ShowDialog();
                    // 设备连接状态已在 ConsultarDispositivos 中正确处理
                    ConsultarDispositivos();

                }
            }
        }
        //添加新设备
        private void buttonNuevo_Click(object sender, EventArgs e)
        {
            LoginDevice frmLoginDevice = new LoginDevice();// LoginDevice窗口登录设备
            frmLoginDevice.nuevo = true;
            frmLoginDevice.ShowDialog();
            ConsultarDispositivos();
            // 新设备连接状态已在 ConsultarDispositivos 中正确处理
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
                DialogResult res = MessageBox.Show("您确定要删除所选设备吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    ListView.SelectedIndexCollection indexes = this.listView.SelectedIndices;
                    string id = this.listView.Items[indexes[0]].Text;
                    if (EliminarDispositivo(id))
                    {
                        ConsultarDispositivos();//刷新列表
                    }
                }
            }
        }
    }
}

