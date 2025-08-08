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
    public partial class ParamInformeEntradaSalida : Form
    {
        private string comboboxplanid = null;
        //构造函数，初始化窗体控件。
        public ParamInformeEntradaSalida()
        {
            InitializeComponent();
        }
        //调用数据库中存储过程 CREAR_TABLA_INFORME_ES()；该过程负责整理原始进出数据为报表格式，临时存入temp_informe_es表。
        private bool CrearTablaInformeES()
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
                    string sql = "CALL CREAR_TABLA_INFORME_ES()";//数据库中存储过程 CREAR_TABLA_INFORME_ES()
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

        //构建最终 SQL 查询语句字符串；根据用户勾选/填写的条件筛选,查询关联表 empleados 和 temp_informe_es,按 fecha, documento 升序排序。
        private string ObtenerExpresionQuery()
        {
            
            string retval = null;
            retval = "SELECT temp_informe_es.id, temp_informe_es.documento, nombres, empleados.apellidos, temp_informe_es.fecha, temp_informe_es.hora_a, temp_informe_es.hora_b FROM empleados, temp_informe_es WHERE empleados.documento = temp_informe_es.documento ";

            if (this.radioButtonTodosEmpleados.Checked == false)
            {
                retval += String.Format("AND temp_informe_es.documento = '{0}' ", this.textBoxDocumentoEmpleado.Text);
            }
            
            if (this.radioButtonTodasFechas.Checked == false)
            {
                string fechainicial = dateTimePickerFechaInicial.Value.ToString("yyyy-MM-dd");
                string fechafinal = dateTimePickerFechaFinal.Value.ToString("yyyy-MM-dd");
                retval += String.Format("AND temp_informe_es.fecha BETWEEN CAST('{0}' AS DATE) AND CAST('{1}' AS DATE) ", fechainicial, fechafinal);
                
            }
            if (this.radioButtonTodasHoras.Checked == false)
            {
                string horainicial = dateTimePickerHoraInicial.Value.ToString("HH:MM:ss");
                string horafinal = dateTimePickerHoraFinal.Value.ToString("HH:MM:ss");
                retval += String.Format("AND temp_informe_es.hora_a AND temp_informe_es.hora_b BETWEEN CAST('{0}' AS TIME) AND CAST('{1}' AS TIME) ", horainicial, horafinal);
            }
            if (this.textBoxNombreEmpleado.Text.Length > 0)
            {
                retval += String.Format("AND empleados.nombres LIKE '%{0}%' ", this.textBoxNombreEmpleado.Text);
            }
            if (this.textBoxApellidosEmpleado.Text.Length > 0)
            {
                retval += String.Format("AND empleados.apellidos LIKE '%{0}%' ", this.textBoxApellidosEmpleado.Text);
            }
            retval += "ORDER BY fecha, documento ASC";

            return retval;
        }
        //执行 SQL 查询；解析每一行记录为 InformeEntradaSalida 实例，并添加到列表中；同时将结果添加到 listView 控件中进行显示；如果没有记录，弹出“没有可显示的记录”。
        private bool GenerarConsulta(string sql, out List<InformeEntradaSalida> listaes)
        {
            listaes = new List<InformeEntradaSalida>();
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
                            DateTime fecha;
                            DateTime horaa;
                            DateTime horab;
                            string strfecha;
                            string strhoraa;
                            string strhorab;
                            try
                            {
                                fecha = DateTime.Parse(rdr["fecha"].ToString());
                                strfecha = fecha.ToString("yyyy-MM-dd");
                            }
                            catch
                            {
                                strfecha = "";
                            }
                            try
                            {
                                horaa = DateTime.Parse(rdr["hora_a"].ToString());
                                strhoraa = horaa.ToString("HH:MM:ss");
                            }
                            catch
                            {
                                strhoraa = "";
                            }
                            try
                            {
                                horab = DateTime.Parse(rdr["hora_b"].ToString());
                                strhorab = horab.ToString("HH:MM:ss");
                            }
                            catch
                            {
                                strhorab = "";
                            }
                            

                            InformeEntradaSalida ies = new InformeEntradaSalida();
                            ies.num = rdr["id"].ToString();
                            ies.documento = rdr["documento"].ToString();
                            ies.nombres = rdr["nombres"].ToString();
                            ies.apellidos = rdr["apellidos"].ToString();
                            ies.fecha = strfecha;
                            ies.horaa = strhoraa;
                            ies.horab = strhorab;
                            listaes.Add(ies);
                            ies = null;

                            ListViewItem lvi = new ListViewItem(rdr["id"].ToString());//id
                            lvi.SubItems.Add(rdr["documento"].ToString());//文档号
                            lvi.SubItems.Add(rdr["nombres"].ToString());//名字
                            lvi.SubItems.Add(rdr["apellidos"].ToString());//姓氏
                            lvi.SubItems.Add(strfecha);
                            lvi.SubItems.Add(strhoraa);
                            lvi.SubItems.Add(strhorab);
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
        //点击“查看报表”按钮的事件处理,调用 CrearTablaInformeES() 生成临时表；调用 ObtenerExpresionQuery() 获取 SQL 查询字符串；清空当前 listView；调用 GenerarConsulta(...) 执行 SQL 并填充界面；创建 Informe 报表窗体并传入数据，展示报表。
        private void buttonVerInforme_Click(object sender, EventArgs e)
        {
            if (CrearTablaInformeES())
            {
                string result = ObtenerExpresionQuery().Trim();
                if (this.listView.Items.Count > 0)
                    this.listView.Items.Clear();
                List<InformeEntradaSalida> objinf = null;
                bool res = GenerarConsulta(result, out objinf);
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


        }
        //启用时间选择器（起始与结束时间）
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
        //用户手动填写文档号时，自动取消“所有员工”选项。
        private void textBoxDocumentoEmpleado_Click(object sender, EventArgs e)
        {

            this.radioButtonTodosEmpleados.Checked = false;
        }
        //当用户选择“所有员工”时，清空文档号、姓名、姓氏文本框。
        private void radioButtonTodosEmpleados_CheckedChanged(object sender, EventArgs e)
        {
            this.textBoxDocumentoEmpleado.Text = "";
            this.textBoxNombreEmpleado.Text = "";
            this.textBoxApellidosEmpleado.Text = "";
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
    }
}
