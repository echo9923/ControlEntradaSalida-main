using Microsoft.Reporting.WinForms;
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
{  //门禁系统中的报表展示窗体 Informe.cs，用于将数据以RDLC报表（内嵌报表） 的形式进行可视化展示。
    public partial class Informe : Form
    {
        public object listads = null;//报表数据源
        public string nombredatasource = null;//数据源名称
        public string embeddedresource = null;//报表嵌入资源路径（.rdlc 文件路径）
        //初始化窗体组件，准备加载 ReportViewer 控件
        public Informe()
        {
            InitializeComponent();
        }
        //这是窗体的“加载事件”。
        //1.创建数据源
        //2.加载 RDLC 报表
        //3.绑定数据并刷新
        private void Informe_Load(object sender, EventArgs e)
        {


            //ReportDataSource rds = new ReportDataSource("InformeEventos", listads);
            ReportDataSource rds = new ReportDataSource(this.nombredatasource, listads);//listads 是传入的数据列表,nombredatasource 是报表模板中定义的数据源名称，必须匹配 .rdlc 文件中使用的名字
            //this.reportViewer1.LocalReport.ReportEmbeddedResource = "ControlEntradaSalida.InformeEventos.rdlc";
            this.reportViewer1.LocalReport.ReportEmbeddedResource = this.embeddedresource;//指定 RDLC 报表模板的嵌入资源路径
            this.reportViewer1.LocalReport.DataSources.Clear();//清除原有数据源；
            this.reportViewer1.LocalReport.DataSources.Add(rds);//添加当前报表的数据源；
            this.reportViewer1.Width = this.Width - 5;
            this.reportViewer1.Height = this.Height - 5;
            this.reportViewer1.RefreshReport();//刷新控件，开始呈现报表内容。
        }

        private void reportViewer1_Load(object sender, EventArgs e)
        {

        }
    }
}
