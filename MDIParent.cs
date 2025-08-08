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
{
    public partial class MDIParent : Form
    {   //初始化窗体和控件
        public MDIParent()
        {
            InitializeComponent();
            
        }
        //设备管理窗口
        private void gestiónDeDispositivosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionDispositivos frmGestionDispositivos = new GestionDispositivos();
            frmGestionDispositivos.MdiParent = this;
            frmGestionDispositivos.Show();
        }
        //员工管理窗口
        private void gestionDeEmpleadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionEmpleados frmGestionEmpleados = new GestionEmpleados();
            frmGestionEmpleados.MdiParent = this;
            frmGestionEmpleados.Show();
        }
        //进出记录采集窗口
        private void CapturarEntradaSalidaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CapturaEntradaSalida frmCapturaEntradaSalida = new CapturaEntradaSalida();
            frmCapturaEntradaSalida.MdiParent = this;
            frmCapturaEntradaSalida.Show();
        }
        //程序退出
        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //事件报表参数窗口
        private void eventosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParamInformeEventos frmParamInformeEventos = new ParamInformeEventos();
            frmParamInformeEventos.MdiParent = this;
            frmParamInformeEventos.Show();
        }
        //窗体加载时的初始化
        private void MDIParent_Load(object sender, EventArgs e)
        {
            if (!Common.InicializarSDKHikVision())//SDK初始化
                MessageBox.Show("HikVision SDK 初始化错误", "SDK初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (!Common.CrearDirectorioData())
                MessageBox.Show("创建数据目录时出错", "数据目录创建错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        //窗体关闭前的清理工作
        private void MDIParent_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Common.m_UserID >= 0)
            {
                HCNetSDK.NET_DVR_Logout_V30(Common.m_UserID);
                Common.m_UserID = -1;
            }
            HCNetSDK.NET_DVR_Cleanup();
        }
        //设备用户信息窗口
        private void consultarDatosDispositivoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionUsuariosDispositivo frmGestionUsuariosDispositivo = new GestionUsuariosDispositivo();
            frmGestionUsuariosDispositivo.MdiParent = this;
            frmGestionUsuariosDispositivo.Show();

        }
        //进出记录报表窗口
        private void entradasYSalidasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParamInformeEntradaSalida frmParamInformeEntradaSalida = new ParamInformeEntradaSalida();
            frmParamInformeEntradaSalida.MdiParent = this;
            frmParamInformeEntradaSalida.Show();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void controldoor_Click(object sender, EventArgs e)
        {
            controldoor frmGestionUsuariosDispositivo = new controldoor();
            frmGestionUsuariosDispositivo.MdiParent = this;
            frmGestionUsuariosDispositivo.Show();
        }

        private void Plantemplate_Click(object sender, EventArgs e)
        {
            Plantemplate frmGestionUsuariosDispositivo = new Plantemplate();
            frmGestionUsuariosDispositivo.MdiParent = this;
            frmGestionUsuariosDispositivo.Show();
        }
    }
}
