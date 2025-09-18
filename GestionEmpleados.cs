using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEntradaSalida
{   //实现员工管理功能
    public partial class GestionEmpleados : Form, IRefreshableForm
    {
        private int m_lCapFaceCfgHandle = -1;
        private int m_lGetFaceCfgHandle = -1;
        private int m_lSetFaceCfgHandle = -1;
        private string url_imagen = null;
        private bool nuevo = true; // 是否为新增员工
        private DataChangeNotifier _notifier;
        
        public bool IsFormVisible => this.Visible;

        //初始化窗体
        public GestionEmpleados()
        {
            InitializeComponent();

            // 减少干扰性的闪烁
            if (this.errorProvider != null)
            {
                this.errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            }

            // 用户编辑后清除该控件的错误提示（不做即时必填校验）
            this.textBoxDocumento.TextChanged += InputControl_Changed;
            this.textBoxNombreCompleto.TextChanged += InputControl_Changed;
            this.cmbEstado.SelectedIndexChanged += InputControl_Changed;
            this.cmbCategoria.SelectedIndexChanged += InputControl_Changed;

            // 窗体显示后，避免自动聚焦到“员工编号”输入框
            this.Shown += GestionEmpleados_Shown;

            // 运行时移除卡号相关功能后，不再需要隐藏卡号控件
        }

        // 窗体显示后将焦点放到一个非输入控件，避免默认聚焦到员工编号
        private void GestionEmpleados_Shown(object sender, EventArgs e)
        {
            try
            {
                // 使用 BeginInvoke 确保在布局完成后设置焦点
                this.BeginInvoke((Action)(() =>
                {
                    if (this.buttonNuevo != null && this.buttonNuevo.CanSelect)
                    {
                        this.ActiveControl = this.buttonNuevo;
                    }
                    else
                    {
                        this.ActiveControl = this; // 退化处理
                    }
                }));
            }
            catch { }
        }

        // 任一输入发生变化时，清除该控件上的错误提示
        private void InputControl_Changed(object sender, EventArgs e)
        {
            if (sender is Control c && this.errorProvider != null)
            {
                this.errorProvider.SetError(c, string.Empty);
            }
        }

        // 统一的提交时必填项校验逻辑
        private bool ValidateFormOnSubmit(out List<string> errors)
        {
            errors = new List<string>();
            if (this.errorProvider != null)
            {
                this.errorProvider.Clear();
            }

            bool valid = true;

            // 员工编号（文档号）
            if (string.IsNullOrWhiteSpace(textBoxDocumento.Text))
            {
                valid = false;
                errors.Add("员工编号");
                if (this.errorProvider != null)
                    this.errorProvider.SetError(textBoxDocumento, "必填");
            }

            // 姓名（完整姓名）
            if (string.IsNullOrWhiteSpace(textBoxNombreCompleto.Text))
            {
                valid = false;
                errors.Add("姓名");
                if (this.errorProvider != null)
                    this.errorProvider.SetError(textBoxNombreCompleto, "必填");
            }

            // 状态
            if (cmbEstado.SelectedItem == null || string.IsNullOrWhiteSpace(cmbEstado.SelectedItem.ToString()))
            {
                valid = false;
                errors.Add("状态");
                if (this.errorProvider != null)
                    this.errorProvider.SetError(cmbEstado, "必选");
            }

            // 类别
            if (cmbCategoria.SelectedItem == null || string.IsNullOrWhiteSpace(cmbCategoria.SelectedItem.ToString()))
            {
                valid = false;
                errors.Add("类别");
                if (this.errorProvider != null)
                    this.errorProvider.SetError(cmbCategoria, "必选");
            }

            return valid;
        }
        
        // 获取当前连接设备的UserID
        private int GetConnectedDeviceUserID()
        {
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                // 使用第一个连接的设备
                return connectedDevices[0].UserID;
            }
            return -1;
        }


        //接收拍摄的图像,写入磁盘,显示在界面上
        private void ProcesarFotoCapturada(ref HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_CFG struFaceCfg, ref bool flag)
        {
            if (0 == struFaceCfg.dwFacePicSize)
            {
                return;
            }
            DateTime dt = DateTime.Now;
            string filePath = null;
            if (Common.datadir != null)
            {
                filePath = Common.datadir + "\\" + dt.ToString("yyyy-MM-dd_HH-mm-ss") + ".jpg"; ;
            } else
            {
                if (!Directory.Exists(Environment.CurrentDirectory + "\\imagenes\\"))
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\imagenes\\");

                filePath = Environment.CurrentDirectory + "\\imagenes\\" + dt.ToString("yyyy-MM-dd_HH-mm-ss") + ".jpg";
            }

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    int FaceLen = struFaceCfg.dwFacePicSize;
                    byte[] by = new byte[FaceLen];
                    Marshal.Copy(struFaceCfg.pFacePicBuffer, by, 0, FaceLen);
                    fs.Write(by, 0, FaceLen);
                    fs.Close();
                }

                pictureBoxUsuario.Image = Image.FromFile(filePath);
                this.url_imagen = filePath;
            }
            catch
            {
                flag = false;
                MessageBox.Show("数据获取错误", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        //调用 SDK 进行拍照；获取照片并传给 ProcesarFotoCapturada()
        private void CapturarFoto()
        {
            if (m_lCapFaceCfgHandle != -1)
            {
                HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lCapFaceCfgHandle);
                m_lCapFaceCfgHandle = -1;
            }
            if (this.pictureBoxUsuario.Image != null)
            {
                this.pictureBoxUsuario.Image.Dispose();
                this.pictureBoxUsuario.Image = null;
            }
            url_imagen = null;

            HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_COND struCond = new HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_COND();
            struCond.init();
            struCond.dwSize = Marshal.SizeOf(struCond);
            int dwInBufferSize = struCond.dwSize;
            IntPtr ptrStruCond = Marshal.AllocHGlobal(dwInBufferSize);
            Marshal.StructureToPtr(struCond, ptrStruCond, false);
            m_lCapFaceCfgHandle = HCNetSDK_Facial.NET_DVR_StartRemoteConfig(GetConnectedDeviceUserID(), HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_INFO, ptrStruCond, dwInBufferSize, null, IntPtr.Zero);
            if (-1 == m_lCapFaceCfgHandle)
            {
                Marshal.FreeHGlobal(ptrStruCond);
                MessageBox.Show("NET_DVR_CAP_FACE_FAIL, ERROR CODE" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString(), "Error", MessageBoxButtons.OK);
                return;
            }

            HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_CFG struFaceCfg = new HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_CFG();
            struFaceCfg.init();
            int dwStatus = 0;
            int dwOutBuffSize = Marshal.SizeOf(struFaceCfg);
            bool Flag = true;
            while (Flag)
            {
                dwStatus = HCNetSDK_Facial.NET_DVR_GetNextRemoteConfig(m_lCapFaceCfgHandle, ref struFaceCfg, dwOutBuffSize);
                switch (dwStatus)
                {
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_SUCCESS://成功读取到数据，处理完本次数据后需调用next
                        ProcesarFotoCapturada(ref struFaceCfg, ref Flag);
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_NEED_WAIT:
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FAILED:
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lCapFaceCfgHandle);
                        MessageBox.Show("NET_SDK_GET_NEXT_STATUS_FAILED" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString(), "Error", MessageBoxButtons.OK);
                        Flag = false;
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FINISH:
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lCapFaceCfgHandle);
                        Flag = false;
                        break;
                    default:
                        MessageBox.Show("NET_SDK_GET_STATUS_UNKOWN" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString(), "Error", MessageBoxButtons.OK);
                        Flag = false;
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lCapFaceCfgHandle);
                        break;
                }
            }
            Marshal.FreeHGlobal(ptrStruCond);
        }

        //构建人脸结构并下发设备
        private bool EnviarImagen(string numlectora, string numtarjeta)
        {
            bool retval = false;
            if (this.pictureBoxUsuario.Image != null)
            {
                this.pictureBoxUsuario.Image.Dispose();
                this.pictureBoxUsuario.Image = null;
            }

            HCNetSDK_Facial.NET_DVR_FACE_COND struCond = new HCNetSDK_Facial.NET_DVR_FACE_COND();
            struCond.init();
            struCond.dwSize = Marshal.SizeOf(struCond);
            struCond.dwFaceNum = 1;
            int.TryParse(numlectora, out struCond.dwEnableReaderNo);
            byte[] byTemp = System.Text.Encoding.UTF8.GetBytes(numtarjeta);
            for (int i = 0; i < byTemp.Length; i++)
            {
                struCond.byCardNo[i] = byTemp[i];
            }

            int dwInBufferSize = Convert.ToInt32(struCond.dwSize);
            IntPtr ptrstruCond = Marshal.AllocHGlobal(dwInBufferSize);
            Marshal.StructureToPtr(struCond, ptrstruCond, false);
            m_lSetFaceCfgHandle = HCNetSDK_Facial.NET_DVR_StartRemoteConfig(GetConnectedDeviceUserID(), HCNetSDK_Facial.NET_DVR_SET_FACE, ptrstruCond, dwInBufferSize, null, IntPtr.Zero);
            if (-1 == m_lSetFaceCfgHandle)
            {
                Marshal.FreeHGlobal(ptrstruCond);
                MessageBox.Show("NET_DVR_SET_FACE_FAIL, ERROR CODE" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString());
                return retval;
            }

            HCNetSDK_Facial.NET_DVR_FACE_RECORD struRecord = new HCNetSDK_Facial.NET_DVR_FACE_RECORD();
            struRecord.init();
            struRecord.dwSize = Marshal.SizeOf(struRecord);

            byte[] byRecordNo = System.Text.Encoding.UTF8.GetBytes(numtarjeta);
            for (int i = 0; i < byRecordNo.Length; i++)
            {
                struRecord.byCardNo[i] = byRecordNo[i];
            }
            bool result = false;
            result = LeerDatosImagen(ref struRecord);//加载图片数据
            int dwInBuffSize = Marshal.SizeOf(struRecord);
            int dwStatus = 0;
            //下发人脸数据
            HCNetSDK_Facial.NET_DVR_FACE_STATUS struStatus = new HCNetSDK_Facial.NET_DVR_FACE_STATUS();
            struStatus.init();
            struStatus.dwSize = Marshal.SizeOf(struStatus);
            int dwOutBuffSize = Convert.ToInt32(struStatus.dwSize);
            IntPtr ptrOutDataLen = Marshal.AllocHGlobal(sizeof(int));
            bool Flag = true;

            while (Flag)
            {
                dwStatus = HCNetSDK_Facial.NET_DVR_SendWithRecvRemoteConfig(m_lSetFaceCfgHandle, ref struRecord, dwInBuffSize, ref struStatus, dwOutBuffSize, ptrOutDataLen);
                switch (dwStatus)
                {
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_SUCCESS:
                        ResultadoEnviarDatosImagen(ref struStatus, ref Flag);//判断下发状态
                        if (Flag)
                            retval = true;
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_NEED_WAIT:
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FAILED:
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lSetFaceCfgHandle);
                        MessageBox.Show("NET_SDK_GET_NEXT_STATUS_FAILED" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString());
                        Flag = false;
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FINISH:
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lSetFaceCfgHandle);
                        Flag = false;
                        break;
                    default:
                        MessageBox.Show("NET_SDK_GET_NEXT_STATUS_UNKOWN" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString());
                        Flag = false;
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lSetFaceCfgHandle);
                        break;
                }
            }
            Marshal.FreeHGlobal(ptrstruCond);
            Marshal.FreeHGlobal(ptrOutDataLen);
            return retval;
        }
        //判断发送人脸数据是否成功
        private void ResultadoEnviarDatosImagen(ref HCNetSDK_Facial.NET_DVR_FACE_STATUS struStatus, ref bool flag)
        {
            switch (struStatus.byRecvStatus)
            {
                case 1:
                    //MessageBox.Show("SetFaceDataSuccessful"); 
                    break;
                default:
                    flag = false;
                    MessageBox.Show("NET_SDK_SET_Face_DATA_FAILED" + struStatus.byRecvStatus.ToString());
                    break;
            }

        }
        //检查图像是否存在；限制图片大小 ≤ 200KB；读取为字节数组并放入结构体中
        private bool LeerDatosImagen(ref HCNetSDK_Facial.NET_DVR_FACE_RECORD struRecord)
        {
            bool retval = false;
            if (this.pictureBoxUsuario.Image != null)
            {
                this.pictureBoxUsuario.Image.Dispose();
                this.pictureBoxUsuario.Image = null;
            }

            if (!File.Exists(url_imagen))
            {
                MessageBox.Show("人脸图片不存在!");
                return retval;
            }
            FileStream fs = new FileStream(url_imagen, FileMode.OpenOrCreate);
            if (0 == fs.Length)
            {
                MessageBox.Show("人脸照片完成，请输入另一张照片!");
                return retval;
            }
            if (200 * 1024 < fs.Length)
            {
                MessageBox.Show("人脸照片大于200k，请输入另一张照片!");
                return retval;
            }
            try
            {
                int.TryParse(fs.Length.ToString(), out struRecord.dwFaceLen);
                int iLen = Convert.ToInt32(struRecord.dwFaceLen);
                byte[] by = new byte[iLen];
                struRecord.pFaceBuffer = Marshal.AllocHGlobal(iLen);
                fs.Read(by, 0, iLen);
                Marshal.Copy(by, 0, struRecord.pFaceBuffer, iLen);
                fs.Close();
                retval = true;
            }
            catch
            {
                MessageBox.Show("读取人脸数据失败");
                fs.Close();
                return retval;
            }
            return retval;
        }
        //“采集照片”按钮响应，调用CapturarFoto()采集照片
        private void buttonCapturarFoto_Click(object sender, EventArgs e)
        {
            
            string msg = null;
            bool retval = false;
            Common cmn = new Common();

            /*如果之前进行了捕捉并想要丢弃，请删除磁盘上的图像 */
            if ( url_imagen != null && this.pictureBoxUsuario.Image != null && nuevo)
            {
                this.pictureBoxUsuario.Image.Dispose();
                this.pictureBoxUsuario.Image = null;

                if (System.IO.File.Exists(url_imagen))
                {    
                    bool result = EliminarImagen(url_imagen);
                    if (!result)
                    {
                        MessageBox.Show("删除预获取文件时出错: " + url_imagen, "删除之前获取的文件", MessageBoxButtons.OK);
                    }
                }
            }

            // 获取当前连接的设备信息
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                // 使用第一个连接的设备
                var device = connectedDevices[0];
                retval = cmn.Login(device.IpAddress, device.Port, device.Username, device.Password, out int userID, out msg);
            }
            else
            {
                retval = false;
                msg = "没有已连接的设备";
            }
            if (retval)
            {
                CapturarFoto();
                this.buttonCapturarFoto.Enabled = false;
            }
            cmn = null;
            
        }
        //从数据库中查询 employees 表中最大 ID 值
        private int ConsultarUltimoID()
        {
            int retval = 0;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "SELECT MAX(employee_id) AS lastid FROM employees";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        
                        while (rdr.Read())
                        {
                            retval = Convert.ToInt32(rdr["lastid"].ToString());
                        }
                        rdr.Close();
                        bd.desconectarMySQL();
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
        //向 listView 添加新用户并显示
        private bool AddUserToListView()
        {
            bool retval = false;
            //int lastid = 0;
            //lastid = ConsultarUltimoID();

            string fullName = this.textBoxNombreCompleto.Text;
            if (fullName.Length > 30)
                fullName = fullName.Substring(0, 29);

            ListViewItem lvi = new ListViewItem(this.textBoxDocumento.Text);
            lvi.SubItems.Add(this.cmbEstado.Text);
            lvi.SubItems.Add(this.textBoxDocumento.Text);           
            lvi.SubItems.Add(fullName); // 显示完整姓名
            lvi.SubItems.Add(""); // 姓氏字段留空
            lvi.SubItems.Add(this.url_imagen);
            lvi.SubItems.Add(this.cmbCategoria.Text);
            listView.Items.Add(lvi);
            lvi = null;
            LimpiarControles(false);
            url_imagen = null;
             
            retval = true;
            return retval;

        }
        //查询所有员工；显示在 listView 中
        private bool CargarDatosTablaEmpleados()
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "SELECT * FROM employees";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            ListViewItem lvi = new ListViewItem(rdr["employee_id"].ToString());//员工编号
                            lvi.SubItems.Add(rdr["status"].ToString()); //状态              
                            // 合并显示完整姓名
                            string nombreCompleto = rdr["full_name"].ToString();
                            lvi.SubItems.Add(nombreCompleto);//完整姓名
                            lvi.SubItems.Add("");//姓氏字段留空
                            lvi.SubItems.Add(rdr["photo_path"].ToString());    //照片  
                            lvi.SubItems.Add("normal"); // 默认类别              
                            listView.Items.Add(lvi);
                            lvi = null;
                        }
                        rdr.Close();
                        bd.desconectarMySQL();
                        retval = true;
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
        //插入新员工到数据库中的employees 表
        private bool InsertarUsuario()
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "INSERT INTO employees "+
                    "(employee_id, card_number, full_name, photo_path, status, created_at) " +
                    "VALUES (@employee_id, @card_number, @full_name, @photo_path, @status, @created_at)";
                try
                {
                    string nombrecompleto = this.textBoxNombreCompleto.Text;
                    if (nombrecompleto.Length > 255)
                        nombrecompleto = nombrecompleto.Substring(0, 254);

                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@employee_id", this.textBoxDocumento.Text);//员工编号
                    cmd.Parameters.AddWithValue("@card_number", this.textBoxDocumento.Text);//卡号设置为与员工编号相同，但不再用于刷卡认证
                    cmd.Parameters.AddWithValue("@full_name", nombrecompleto);//完整姓名
                    cmd.Parameters.AddWithValue("@photo_path", this.url_imagen);//照片路径
                    cmd.Parameters.AddWithValue("@status", this.cmbEstado.Text);//状态
                    cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//创建时间
                    cmd.ExecuteNonQuery();
                    bd.desconectarMySQL();
                    
                    retval = true;

                } catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            } else {
                MessageBox.Show(bd.errormsg);
            }        
            return retval;
        }
        //从数据库中的employees 表删除员工
        private bool EliminarUsuario(string documento)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "CALL delete_employee(@employee_id)";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@employee_id", documento);
                    cmd.ExecuteNonQuery();
                    bd.desconectarMySQL();
                    retval = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            } else
            {
                MessageBox.Show(bd.errormsg);
            }
            return retval;
        }

        //用于编辑后更新界面上的条目信息
        private void ActualizarItemListView()
        {
            if (listView.SelectedItems.Count == 1)
            {
                ListViewItem item = this.listView.SelectedItems[0];
                item.SubItems[1].Text = cmbEstado.Text;
                item.SubItems[2].Text = this.textBoxNombreCompleto.Text; // 显示完整姓名
                item.SubItems[3].Text = ""; // 姓氏字段留空
                item.SubItems[4].Text = "";
                item.SubItems[5].Text = cmbCategoria.Text;
            }
        }
        //更新数据库中员工信息（姓名、状态、修改时间）
        private bool ActualizarInfoUsuarioBD(string documento)
        {
            bool retval = false;
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = "UPDATE employees SET full_name = @full_name, " +
                    "status = @status, updated_at = @updated_at " +
                    "WHERE employee_id = @employee_id";
                try
                {
                    string nombrecompleto = this.textBoxNombreCompleto.Text;
                    if (nombrecompleto.Length > 255)
                        nombrecompleto = nombrecompleto.Substring(0, 254);

                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@employee_id", this.textBoxDocumento.Text);
                    cmd.Parameters.AddWithValue("@full_name", nombrecompleto);
                    cmd.Parameters.AddWithValue("@status", this.cmbEstado.Text);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                    bd.desconectarMySQL();

                    retval = true;
                    ActualizarItemListView();

                }
                catch (MySqlException ex)
                {
                    string errstr = ex.Number.ToString() + " " + ex.Message;
                    MessageBox.Show(errstr, "在ActualizarInfoUsuarioBD()中出错", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }
            else
            {
                string errstr = bd.errornum + " " + bd.errormsg + "bd = null";
                MessageBox.Show(errstr, "在ActualizarInfoUsuarioBD()中出错", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return retval;
        }
        //调用 Hikvision ISAPI 接口 PUT 命令；同步修改姓名、有效期等
        private bool ActualizarInfoUsuarioDispositivo(string documento)
        {
            bool retval = false;

            string url = "PUT /ISAPI/AccessControl/UserInfo/Modify?format=json";
            string UserInfo = "{{ \"UserInfo\" : {{\"employeeNo\": \"{0}\",\"name\": \"{1}\",\"userType\": \"normal\",   \"Valid\" : {{ \"enable\": true,\"beginTime\": \"{2}\",\"endTime\": \"{3}\", \"timeType\": \"local\"}}  }}}}";

            string nombrecompleto = this.textBoxNombreCompleto.Text;
            if (nombrecompleto.Length > 30)
                nombrecompleto = nombrecompleto.Substring(0, 29);

            string fechainicio = "2022-01-01T00:00:00";
            string fechafinal = "2031-01-01T23:59:00";
            string UserInfoValues = String.Format(UserInfo, documento, nombrecompleto, fechainicio, fechafinal);


            string outputString = null;
            string outputStatus = null;
            bool result = false;
            Common cmn = new Common();
            result = cmn.ISAPIQuery(GetConnectedDeviceUserID(), url, UserInfoValues, out outputString, out outputStatus);
            string xmlresult = "";
            dynamic DynamicData;
            if (!result)
            {
                try
                {
                    xmlresult = outputStatus;
                    DynamicData = JsonConvert.DeserializeObject(xmlresult);
                    string statusCode = DynamicData.statusCode;
                    string statusString = DynamicData.statusString;
                    string subStatusCode = DynamicData.subStatusCode;
                    string errorCode = DynamicData.errorCode;
                    string errorMsg = DynamicData.errorMsg;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("更新用户设备信息时出错", ex.Message.ToString());
                }

            }
            else
            {
                try
                {
                    xmlresult = outputString;
                    DynamicData = JsonConvert.DeserializeObject(xmlresult);
                    string statusCode = DynamicData.statusCode;
                    string statusString = DynamicData.statusString;
                    string subStatusCode = DynamicData.subStatusCode;

                    if (statusCode == "1" && statusString == "OK" && subStatusCode == "ok")
                        retval = true;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("更新用户设备信息时出错", ex.Message.ToString());
                }
            }
            return retval;
        }

        // 验证文档号
        private bool ValidarDocumento()
        {
            if (string.IsNullOrWhiteSpace(textBoxDocumento.Text))
            {
                MessageBox.Show("请输入文档号", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxDocumento.Focus();
                return false;
            }
            return true;
        }

        // 验证状态
        private bool ValidarEstado()
        {
            if (cmbEstado.SelectedItem == null || string.IsNullOrWhiteSpace(cmbEstado.SelectedItem.ToString()))
            {
                MessageBox.Show("请选择状态", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbEstado.Focus();
                return false;
            }
            return true;
        }

        // 验证姓名
        private bool ValidarNombreCompleto()
        {
            if (string.IsNullOrWhiteSpace(textBoxNombreCompleto.Text))
            {
                MessageBox.Show("请输入姓名", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxNombreCompleto.Focus();
                return false;
            }
            return true;
        }

        // 验证类别
        private bool ValidarCategoria()
        {
            if (cmbCategoria.SelectedItem == null || string.IsNullOrWhiteSpace(cmbCategoria.SelectedItem.ToString()))
            {
                MessageBox.Show("请选择类别", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbCategoria.Focus();
                return false;
            }
            return true;
        }

        // 文档号验证事件
        private void textBoxDocumento_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            // 验证逻辑已禁用，避免干扰用户输入体验
        }

        // 姓名验证事件
        private void textBoxNombreCompleto_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            // 验证逻辑已禁用，避免干扰用户输入体验
        }

        // 状态选择事件
        private void cmbEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.errorProvider != null)
                this.errorProvider.SetError(cmbEstado, string.Empty);
            // 状态选择改变时的处理逻辑
        }

        // 状态验证事件
        private void cmbEstado_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            // 验证逻辑已禁用，避免干扰用户输入体验
        }

        // 类别选择事件
        private void cmbCategoria_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.errorProvider != null)
                this.errorProvider.SetError(cmbCategoria, string.Empty);
            // 类别选择改变时的处理逻辑
        }

        // 类别验证事件
        private void cmbCategoria_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            // 验证逻辑已禁用，避免干扰用户输入体验
        }

        //"保存"按钮相应，同时在设备和数据库中更新
        private void buttonAgregar_Click(object sender, EventArgs e)
        {
            List<string> errors;
            if (!ValidateFormOnSubmit(out errors))
            {
                if (errors != null && errors.Count > 0)
                {
                    string summary = "请完善以下必填项：\n - " + string.Join("\n - ", errors);
                    MessageBox.Show(summary, "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            string msg;
            bool retval;
            Common cmn = new Common();
            // 获取当前连接的设备信息
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                // 使用第一个连接的设备
                var device = connectedDevices[0];
                retval = cmn.Login(device.IpAddress, device.Port, device.Username, device.Password, out int userID, out msg);
            }
            else
            {
                retval = false;
                msg = "没有已连接的设备";
            }
            if (retval)
            {
                bool resultenviar = false;
                string nombrecompleto = this.textBoxNombreCompleto.Text;
                if (nombrecompleto.Length > 30)
                    nombrecompleto = nombrecompleto.Substring(0, 29);

                

                if (nuevo)//如果是新增员工
                {

                    if (this.pictureBoxUsuario.Image != null)
                        resultenviar = EnviarImagen("1", this.textBoxDocumento.Text);//上传人脸图片
                    else
                        resultenviar = true;

                    if (resultenviar)
                    {
                        InsertarUsuario();// 插入数据库
                        AddUserToListView();//更新 UI界面列表
                        LimpiarControles(false);
                        url_imagen = null;
                        
                        // 通知其他界面员工数据已变更
                        _notifier?.NotifyEmployeeDataChanged(
                            this.textBoxDocumento.Text,
                            this.textBoxNombreCompleto.Text,
                            EmployeeChangeType.Added,
                            this.GetType().Name);
                    }
                }
                if (!nuevo)//更新已有员工
                {
                    bool resultactbd = false; ;
                    bool resultactdispo = ActualizarInfoUsuarioDispositivo(this.textBoxDocumento.Text);// 更新设备信息（通过 ISAPI）

                    if (this.pictureBoxUsuario.Image != null)
                        resultenviar = EnviarImagen("1", this.textBoxDocumento.Text);
                    else
                        resultenviar = true;

                    if (resultactdispo && resultenviar)
                        resultactbd = ActualizarInfoUsuarioBD(this.textBoxDocumento.Text);//更新本地数据库

                    if (!resultactdispo || !resultenviar)
                        MessageBox.Show("无法在设备上更新用户（信息和/或照片）", "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (!resultactbd)
                        MessageBox.Show("无法在本地数据库中更新用户", "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 如果更新成功，通知其他界面
                    if (resultactbd)
                    {
                        _notifier?.NotifyEmployeeDataChanged(
                            this.textBoxDocumento.Text,
                            this.textBoxNombreCompleto.Text,
                            EmployeeChangeType.Updated,
                            this.GetType().Name);
                    }
                    
                    LimpiarControles(false);
                }
            }
            cmn = null;
        }
        //窗体加载时触发,检查是否登录设备；
        private void GestionUsuarios_Load(object sender, EventArgs e)
        {
            if (GetConnectedDeviceUserID() < 0)
            {
                MessageBox.Show("您必须在设备上登录", "登录错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 不再禁用整个groupBox，而是只禁用与设备相关的按钮
                this.buttonCapturarFoto.Enabled = false;
                this.buttonAgregar.Enabled = false;
                this.buttonEliminar.Enabled = false;
            }
            else
            {
                // 启用与设备相关的按钮
                this.buttonCapturarFoto.Enabled = true;
                this.buttonAgregar.Enabled = true;
                this.buttonEliminar.Enabled = true;
            }
            LimpiarControles(true);//初始化控件
            CargarDatosTablaEmpleados();//加载所有员工数据到 listView
            
            // 确保所有控件都处于启用状态
            this.textBoxNombreCompleto.Enabled = true;
            this.textBoxDocumento.Enabled = true;
            this.cmbEstado.Enabled = true;
            this.cmbCategoria.Enabled = true;
            this.buttonNuevo.Enabled = true;
            this.buttonFiltrar.Enabled = true;
            
            // 初始化数据变更通知器
            _notifier = DataChangeNotifier.Instance;
            _notifier.DeviceDataChanged += OnDeviceDataChanged;
            _notifier.EmployeeDataChanged += OnEmployeeDataChanged;
        }
        //删除本地保存的人脸图片
        private bool EliminarImagen(string filePath)
        {
            bool retval = false;
            try
            {
                System.IO.File.Delete(filePath);
                retval = true;
            } catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                
            }
            return retval;
        }
        //清空控件内容；可选清空列表；重置界面状态
        private void LimpiarControles(bool todos)
        {
            this.cmbEstado.Text = this.cmbEstado.Items[0].ToString();
            this.cmbCategoria.Text = this.cmbCategoria.Items[0].ToString();
            this.textBoxDocumento.Enabled = true;
            this.textBoxDocumento.Text = "";            
            this.textBoxNombreCompleto.Text = "";
            this.buttonAgregar.Enabled = true;
            this.buttonNuevo.Enabled = true;
            this.buttonEliminar.Enabled = true;
            this.buttonFiltrar.Enabled = true;

            if (this.pictureBoxUsuario.Image != null)
            {
                this.pictureBoxUsuario.Image.Dispose();
                this.pictureBoxUsuario.Image = null;
            }
            if (todos == true)
            {
                if (this.listView.Items.Count > 0)
                    this.listView.Items.Clear();
                
            }
            url_imagen = null;
            nuevo = true;
            this.buttonCapturarFoto.Enabled = true;
        }
        //“删除”按钮响应，从数据库和设备上同时删除
        private void buttonEliminar_Click(object sender, EventArgs e)
        {
            if (this.listView.CheckedItems.Count == 0)
            {
                MessageBox.Show("您必须至少选择列表中的一个用户", "删除用户", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return; 
            }

            DialogResult dialogResult = MessageBox.Show("选定的用户将被删除，无法恢复。您确定要继续吗?", "确认删除操作", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                string msg;
                bool retval;
                Common cmn = new Common();
                // 获取当前连接的设备信息
                var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                    .Where(d => d.IsConnected).ToList();
                if (connectedDevices.Count > 0)
                {
                    // 使用第一个连接的设备
                    var device = connectedDevices[0];
                    retval = cmn.Login(device.IpAddress, device.Port, device.Username, device.Password, out int userID, out msg);
                }
                else
                {
                    retval = false;
                    msg = "没有已连接的设备";
                }
                if (retval)
                {
                    ListView.CheckedListViewItemCollection itemColl = this.listView.CheckedItems;
                    foreach (ListViewItem item in itemColl) 
                    {
                        string documento = item.Text;
                        bool result = EliminarUsuario(documento);//从数据库中删除
                        if (result)
                        {
                            // 通知其他界面员工数据已变更
                            string nombreCompleto = item.SubItems.Count > 3 ? item.SubItems[3].Text : "未知";
                            _notifier?.NotifyEmployeeDataChanged(
                                documento,
                                nombreCompleto,
                                EmployeeChangeType.Deleted,
                                this.GetType().Name);
                        }
                        else
                        {
                            MessageBox.Show("未能从数据库中删除员工信息，请检查", "删除用户", MessageBoxButtons.OK);
                        }                  
                    }
                    LimpiarControles(true);//重置界面状态
                    CargarDatosTablaEmpleados();//显示在 listView

                }
                    
            }
                
        }
        //从设备下载用户的人脸图像，显示在 pictureBoxUsuario
        private bool BuscarImagenUsuario(string documento)
        {
            bool retval = false;
            if (m_lGetFaceCfgHandle != -1)
            {
                HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lGetFaceCfgHandle);
                m_lGetFaceCfgHandle = -1;
            }

            if (this.pictureBoxUsuario.Image != null)
            {
                pictureBoxUsuario.Image.Dispose();
                pictureBoxUsuario.Image = null;
            }

            HCNetSDK_Facial.NET_DVR_FACE_COND struCond = new HCNetSDK_Facial.NET_DVR_FACE_COND();
            struCond.init();
            struCond.dwSize = Marshal.SizeOf(struCond);
            int dwSize = struCond.dwSize;
            struCond.dwEnableReaderNo = 1;
            struCond.dwFaceNum = 1;// The numbre of faces is 1
            byte[] byTemp = System.Text.Encoding.UTF8.GetBytes(documento);
            for (int i = 0; i < byTemp.Length; i++)
            {
                struCond.byCardNo[i] = byTemp[i];
            }

            IntPtr ptrStruCond = Marshal.AllocHGlobal(dwSize);
            Marshal.StructureToPtr(struCond, ptrStruCond, false);

            m_lGetFaceCfgHandle = HCNetSDK_Facial.NET_DVR_StartRemoteConfig(GetConnectedDeviceUserID(), HCNetSDK_Facial.NET_DVR_GET_FACE, ptrStruCond, dwSize, null, IntPtr.Zero);
            if (m_lGetFaceCfgHandle == -1)
            {
                Marshal.FreeHGlobal(ptrStruCond);
                MessageBox.Show("NET_DVR_GET_FACE_FAIL, ERROR CODE" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString(), "Error", MessageBoxButtons.OK);
                return false;
            }

            bool Flag = true;
            int dwStatus = 0;

            HCNetSDK_Facial.NET_DVR_FACE_RECORD struRecord = new HCNetSDK_Facial.NET_DVR_FACE_RECORD();
            struRecord.init();
            struRecord.dwSize = Marshal.SizeOf(struRecord);
            int dwOutBuffSize = struRecord.dwSize;
            while (Flag)
            {
                dwStatus = HCNetSDK_Facial.NET_DVR_GetNextRemoteConfig(m_lGetFaceCfgHandle, ref struRecord, dwOutBuffSize);
                switch (dwStatus)
                {
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_SUCCESS://The data is successfully read. After processing this data, you need to call next
                        ProcessFaceDataFromDevice(ref struRecord, ref Flag);
                        if (Flag)
                            retval = true;
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_NEED_WAIT:
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FAILED:
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lGetFaceCfgHandle);
                        //MessageBox.Show("NET_SDK_GET_NEXT_STATUS_FAILED" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString(), "Error", MessageBoxButtons.OK);
                        Flag = false;
                        break;
                    case HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FINISH:
                        // MessageBox.Show("NET_SDK_GET_NEXT_STATUS_FINISH", "Tips", MessageBoxButtons.OK);
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lGetFaceCfgHandle);
                        Flag = false;
                        break;
                    default:
                        //MessageBox.Show("NET_SDK_GET_STATUS_UNKOWN" + HCNetSDK_Facial.NET_DVR_GetLastError().ToString(), "Error", MessageBoxButtons.OK);
                        Flag = false;
                        HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lGetFaceCfgHandle);
                        break;
                }
            }

            Marshal.FreeHGlobal(ptrStruCond);
            return retval;
        }
        //从结构体读取图像字节；保存为图片并显示
        private void ProcessFaceDataFromDevice(ref HCNetSDK_Facial.NET_DVR_FACE_RECORD struRecord, ref Boolean Flag)
        {
            string strpath = null;
            DateTime dt = DateTime.Now;
            string filePathName = null;
            if (Common.datadir != null)
            {
                filePathName = Common.datadir + "\\" + dt.ToString(this.textBoxDocumento.Text + "_yyyy-MM-dd_HH-mm-ss") + ".jpg"; ;
            }
            else
            {
                if (!Directory.Exists(Environment.CurrentDirectory + "\\imagenes\\"))
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\imagenes\\");

                filePathName = Environment.CurrentDirectory + "\\imagenes\\" + dt.ToString(this.textBoxDocumento.Text + "_yyyy-MM-dd_HH-mm-ss") + ".jpg";
            }

            strpath = filePathName;

            if (0 == struRecord.dwFaceLen)
            {
                return;
            }

            if (pictureBoxUsuario.Image != null)
            {
                pictureBoxUsuario.Image.Dispose();
                pictureBoxUsuario.Image = null;
            }

            try
            {
                using (FileStream fs = new FileStream(strpath, FileMode.OpenOrCreate))
                {
                    int FaceLen = struRecord.dwFaceLen;
                    byte[] by = new byte[FaceLen];
                    Marshal.Copy(struRecord.pFaceBuffer, by, 0, FaceLen);
                    fs.Write(by, 0, FaceLen);
                    fs.Close();
                }
                pictureBoxUsuario.Image = Image.FromFile(strpath);
                this.url_imagen = filePathName;
            }
            catch
            {
                Flag = false;
                HCNetSDK_Facial.NET_DVR_StopRemoteConfig(m_lGetFaceCfgHandle);
                MessageBox.Show("处理面部数据失败", "错误", MessageBoxButtons.OK);
            }
        }
        //点击列表时响应,显示选中的用户信息
        private void listView_Click(object sender, EventArgs e)
        {
            if  (listView.SelectedItems.Count > 0)                
            {
                nuevo = false;
                //this.buttonCapturarFoto.Enabled = false;
                ListViewItem item = listView.SelectedItems[0];
                this.textBoxDocumento.Text = item.SubItems[0].Text;
                this.cmbEstado.Text = item.SubItems[1].Text;
                this.textBoxNombreCompleto.Text = item.SubItems[2].Text;
                this.cmbCategoria.Text = item.SubItems[5].Text;
                this.textBoxDocumento.Enabled = false;
                //this.pictureBoxUsuario.ImageLocation = item.SubItems[5].Text;
                //this.url_imagen = item.SubItems[5].Text;
                BuscarImagenUsuario(this.textBoxDocumento.Text);
                
            }
        }

        //构建 SQL 查询表达式用于过滤查询
        private string ObtenerExpresionQuery()
        {
            string retval = null;

            retval = "SELECT * FROM employees WHERE 1 = 1 ";

            if (this.textBoxDocumento.Text.Trim() != "")
                retval += String.Format("AND employee_id LIKE '%{0}%' ", this.textBoxDocumento.Text);
            if (this.cmbEstado.Text.Trim() != "")
                retval += String.Format("AND status LIKE '%{0}%' ", this.cmbEstado.Text);
            // 使用完整姓名进行查询
            if (this.textBoxNombreCompleto.Text.Trim() != "")
                retval += String.Format("AND full_name LIKE '%{0}%' ", this.textBoxNombreCompleto.Text);
            return retval;
        }
        //执行查询并更新 listView
        private void filtrar(string query)
        {
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            if (bd.conn != null)
            {
                string sql = query;
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    if (rdr.HasRows)
                    {
                        if (this.listView.Items.Count > 0)
                            this.listView.Items.Clear();

                        while (rdr.Read())
                        {
                            ListViewItem lvi = new ListViewItem(rdr["employee_id"].ToString());
                            lvi.SubItems.Add(rdr["status"].ToString());
                            // 合并显示完整姓名
                            string nombreCompleto = rdr["full_name"].ToString();
                            lvi.SubItems.Add(nombreCompleto);
                            lvi.SubItems.Add(""); // 姓氏字段留空
                            lvi.SubItems.Add(rdr["photo_path"].ToString());
                            lvi.SubItems.Add("normal"); // 默认类别
                            listView.Items.Add(lvi);
                            lvi = null;
                        }
                        rdr.Close();
                        bd.desconectarMySQL();
                        
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
            
        }
        //"筛选"按钮响应，过滤出查询
        private void buttonFiltrar_Click(object sender, EventArgs e)
        {
            string query = ObtenerExpresionQuery().Trim();
            filtrar(query);
            Console.WriteLine(query);
        }
        //清空所有输入，用于新增
        private void buttonNuevo_Click(object sender, EventArgs e)
        {
            nuevo = true;
            LimpiarControles(false);
            this.textBoxDocumento.Enabled = true;
            this.buttonCapturarFoto.Enabled = true;
        }
        
        #region IRefreshableForm 实现
        
        /// <summary>
        /// 刷新设备数据（员工管理界面不需要）
        /// </summary>
        public void RefreshDeviceData()
        {
            // 员工管理界面不需要刷新设备数据
        }
        
        /// <summary>
        /// 刷新员工数据
        /// </summary>
        public void RefreshEmployeeData()
        {
            SafeUIUpdater.UpdateUI(this, () => 
            {
                CargarDatosTablaEmpleados();
            });
        }
        
        /// <summary>
        /// 刷新门状态（员工管理界面不需要）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">门状态</param>
        public void RefreshDoorStatus(string deviceId, DoorStatus status)
        {
            // 员工管理界面不需要刷新门状态
        }
        
        #endregion
        
        #region 数据变更事件处理
        
        /// <summary>
        /// 处理设备数据变更事件
        /// </summary>
        private void OnDeviceDataChanged(object sender, DeviceDataChangedEventArgs e)
        {
            // 员工管理界面不需要处理设备数据变更
        }
        
        /// <summary>
        /// 处理员工数据变更事件
        /// </summary>
        private void OnEmployeeDataChanged(object sender, EmployeeDataChangedEventArgs e)
        {
            // 避免自己触发的事件导致重复刷新
            if (e.Source == this.GetType().Name) return;
            
            if (this.IsFormVisible)
            {
                RefreshEmployeeData();
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
    }
}
