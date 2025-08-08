using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// BaseDatosMySQL类,连接和断开 MySQL 数据库
namespace ControlEntradaSalida
{
    class BaseDatosMySQL
    {
       
        public MySqlConnection conn { get; set; }//保存数据库连接对象
        public string errormsg { get; set; }//存储连接出错时的错误信息
        public string errornum { get; set; }//存储连接出错时的错误号
        public BaseDatosMySQL()
        {
            errormsg = null;
            errornum = null;
            conn = null;
        }
        public void conectarMySQL(string connStr)
        {
            /*创建 conn 对象，使用作为参数传递的连接字符串信息 */
            conn = new MySqlConnection(connStr);
            try
            {
                /* 打开与数据库的连接 */
                conn.Open();
            }
            catch (MySqlException ex)
            {
                /* 如果在连接数据库时发生任何错误，将把错误信息存储在这些变量中*/
                errormsg = ex.Message;
                errornum = Convert.ToString(ex.Number);
                conn = null;
            }
        }
        public void desconectarMySQL()//断开数据库连接
        {
            conn.Close();
        }
    }
}

