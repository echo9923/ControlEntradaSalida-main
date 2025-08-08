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
{   //门禁系统中的一个事件报表参数选择与生成模块，功能类似于 ParamInformeEntradaSalida.cs，但它针对的是原始事件数据（如刷卡、人脸识别、门禁动作等）表 entradas_salidas，而非整理后的进出时间段。
    /*提供 UI 选择查询条件（员工、日期、时间等）；
    构建 SQL 查询语句；
    查询 entradas_salidas 表与 empleados 联表数据；
    显示在界面 ListView 中；
    打开报表窗口 Informe 显示 InformeEventos 报表。
     */
    public partial class ParamInformeEventos : Form
    {
        private string comboboxplanid = null;
        //构造函数，初始化窗体控件
        public ParamInformeEventos()
        {
            InitializeComponent();
        }



        //构造 SQL 查询语句，用于查询 entradas_salidas 表的事件记录,支持多种筛选条件拼接,默认按 documento, fecha, hora 升序排序。
        private string ObtenerExpresionQuery()
        {
            
            string retval = null;
            retval = "SELECT entradas_salidas.num, empleados.documento, empleados.nombres, empleados.apellidos, entradas_salidas.fecha, entradas_salidas.hora FROM empleados, entradas_salidas WHERE empleados.documento = entradas_salidas.documento ";

            if (this.radioButtonTodosEmpleados.Checked == false)//员工文档号（精确匹配）
            {
                retval += String.Format("AND empleados.documento = '{0}' ", this.textBoxDocumentoEmpleado.Text);
            }
            
            if (this.radioButtonTodasFechas.Checked == false)//日期范围
            {
                string fechainicial = dateTimePickerFechaInicial.Value.ToString("yyyy-MM-dd");
                string fechafinal = dateTimePickerFechaFinal.Value.ToString("yyyy-MM-dd");
                retval += String.Format("AND entradas_salidas.fecha BETWEEN CAST('{0}' AS DATE) AND CAST('{1}' AS DATE) ", fechainicial, fechafinal);
                
            }
            if (this.radioButtonTodasHoras.Checked == false)//时间范围；
            {
                string horainicial = dateTimePickerHoraInicial.Value.ToString("HH:MM:ss");
                string horafinal = dateTimePickerHoraFinal.Value.ToString("HH:MM:ss");
                retval += String.Format("AND entradas_salidas.hora BETWEEN CAST('{0}' AS TIME) AND CAST('{1}' AS TIME) ", horainicial, horafinal);
            }
            if (this.textBoxNombreEmpleado.Text.Length > 0)//姓名、姓氏（模糊匹配）；
            {
                retval += String.Format("AND empleados.nombres LIKE '%{0}%' ", this.textBoxNombreEmpleado.Text);
            }
            if (this.textBoxApellidosEmpleado.Text.Length > 0)
            {
                retval += String.Format("AND empleados.apellidos LIKE '%{0}%' ", this.textBoxApellidosEmpleado.Text);
            }
            retval += "ORDER BY documento, fecha, hora ASC";

            return retval;
        }
        //执行 SQL 查询；将查询结果填充进一个 List<InformeEventos> 对象；同时在界面的 ListView 控件中显示；如果无数据，弹出提示框
        private bool GenerarConsulta(string sql, out List<InformeEventos> lieventos)
        {
            lieventos = new List<InformeEventos>();
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

                            DateTime fecha = DateTime.Parse(rdr["fecha"].ToString());//日期
                            DateTime hora = DateTime.Parse(rdr["hora"].ToString());//时间

                            InformeEventos ie = new InformeEventos();
                            ie.num = rdr["num"].ToString();//编号
                            ie.documento = rdr["documento"].ToString();//文档号
                            ie.nombres = rdr["nombres"].ToString();//名字
                            ie.apellidos = rdr["apellidos"].ToString();
                            ie.fecha = fecha.ToString("yyyy-MM-dd");
                            ie.hora = hora.ToString("HH:MM:ss");
                            lieventos.Add(ie);
                            ie = null;

                            ListViewItem lvi = new ListViewItem(rdr["num"].ToString());
                            lvi.SubItems.Add(rdr["documento"].ToString());
                            lvi.SubItems.Add(rdr["nombres"].ToString());
                            lvi.SubItems.Add(rdr["apellidos"].ToString());
                            lvi.SubItems.Add(rdr["hora"].ToString());
                            lvi.SubItems.Add(rdr["fecha"].ToString());
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
        //“查看报表”按钮事件处理函数；调用 ObtenerExpresionQuery() 获取 SQL查询语句,调用 GenerarConsulta() 执行查询；
        private void buttonVerInforme_Click(object sender, EventArgs e)
        {
            string result = ObtenerExpresionQuery().Trim();
            if (this.listView.Items.Count > 0)
                this.listView.Items.Clear();
            List<InformeEventos> objinf = null;
            bool res = GenerarConsulta(result, out objinf);
            if (res && objinf != null)
            {
                Informe frmInforme = new Informe();//创建 Informe 报表窗体并设置
                frmInforme.listads = objinf;//数据源
                frmInforme.embeddedresource = "ControlEntradaSalida.InformeEventos.rdlc";//使用的报表模板
                frmInforme.nombredatasource = "InformeEventos"; //数据集名称
                frmInforme.Show();
            }
        }


        //启用起止日期选择器。
        private void radioButtonRangoFechas_Click(object sender, EventArgs e)
        {
            this.dateTimePickerFechaInicial.Enabled = true;
            this.dateTimePickerFechaFinal.Enabled = true;
        }
        //禁用日期选择器
        private void radioButtonTodasFechas_Click(object sender, EventArgs e)
        {
            this.dateTimePickerFechaInicial.Enabled = false;
            this.dateTimePickerFechaFinal.Enabled = false;
        }
        //窗体加载事件；默认勾选“所有员工
        private void ParamInformeConsumos_Load(object sender, EventArgs e)
        {
            this.radioButtonTodosEmpleados.Checked = true;


        }
        //启用时间范围选择器。
        private void radioButtonRangoHoras_CheckedChanged(object sender, EventArgs e)
        {
            this.dateTimePickerHoraInicial.Enabled = true;
            this.dateTimePickerHoraFinal.Enabled = true;
        }
        //禁用时间选择器。
        private void radioButtonTodasHoras_CheckedChanged(object sender, EventArgs e)
        {
            this.dateTimePickerHoraInicial.Enabled = false;
            this.dateTimePickerHoraFinal.Enabled = false;
        }
        //用户点击文档号输入框时，自动取消“所有员工”选项。
        private void textBoxDocumentoEmpleado_Click(object sender, EventArgs e)
        {

            this.radioButtonTodosEmpleados.Checked = false;
        }
        //勾选“所有员工”时，清空相关文本框：文档号、姓名、姓氏。
        private void radioButtonTodosEmpleados_CheckedChanged(object sender, EventArgs e)
        {
            this.textBoxDocumentoEmpleado.Text = "";
            this.textBoxNombreEmpleado.Text = "";
            this.textBoxApellidosEmpleado.Text = "";
        }
    }
}
