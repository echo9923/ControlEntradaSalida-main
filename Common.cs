using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//Common类，它实现了多个与海康威视设备通信、数据库连接及文件目录管理相关的工具函数。
namespace ControlEntradaSalida
{
    class Common
    {   //设备相关参数
        public static int m_UserID = -1;//id
        public static string ip = null;//ip
        public static string puerto = null;//端口
        public static string usuario = null;//用户
        public static string contrasena = null;//密码
        public static string datadir = null;//储存地址

        private uint iLastErr = 0;

        //	使用海康威视 SDK 向设备发送 ISAPI（XML 配置）请求并接收结果
        public bool ISAPIQuery(string requestURL, string inputParam, out string outputResult, out string outputStatus)
        {
            bool retval = true;

            outputResult = null;
            outputStatus = null;
            string msg = null;

            Common cmn = new Common();
            //bool retlogin = cmn.Login(Common.ip, Common.puerto, Common.usuario, Common.contrasena, out msg);
            bool retlogin = true;

            if (!retlogin)
            {
                retval = false;
            }
            else
            {
                HCNetSDK.NET_DVR_XML_CONFIG_INPUT pInputXml = new HCNetSDK.NET_DVR_XML_CONFIG_INPUT();
                Int32 nInSize = Marshal.SizeOf(pInputXml);
                pInputXml.dwSize = (uint)nInSize;

                string strRequestUrl = requestURL;
                uint dwRequestUrlLen = (uint)strRequestUrl.Length;
                pInputXml.lpRequestUrl = Marshal.StringToHGlobalAnsi(strRequestUrl);
                pInputXml.dwRequestUrlLen = dwRequestUrlLen;

                string strInputParam = inputParam;

                pInputXml.lpInBuffer = Marshal.StringToHGlobalAnsi(strInputParam);
                pInputXml.dwInBufferSize = (uint)strInputParam.Length;

                HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT pOutputXml = new HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT();
                pOutputXml.dwSize = (uint)Marshal.SizeOf(pInputXml);
                pOutputXml.lpOutBuffer = Marshal.AllocHGlobal(3 * 1024 * 1024);
                pOutputXml.dwOutBufferSize = 3 * 1024 * 1024;
                pOutputXml.lpStatusBuffer = Marshal.AllocHGlobal(4096 * 4);
                pOutputXml.dwStatusSize = 4096 * 4;

                if (!HCNetSDK.NET_DVR_STDXMLConfig(Common.m_UserID, ref pInputXml, ref pOutputXml))
                {
                    iLastErr = HCNetSDK.NET_DVR_GetLastError();
                    outputResult = "NET_DVR_STDXMLConfig failed, error code= " + iLastErr;
                    retval = false;
                }
                else
                {
                    string strOutputParam = Marshal.PtrToStringAnsi(pOutputXml.lpOutBuffer);
                    outputResult = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(strOutputParam));
                    outputStatus = Marshal.PtrToStringAnsi(pOutputXml.lpStatusBuffer);

                }
                Marshal.FreeHGlobal(pInputXml.lpRequestUrl);
                Marshal.FreeHGlobal(pOutputXml.lpOutBuffer);
                Marshal.FreeHGlobal(pOutputXml.lpStatusBuffer);
            }



            return retval;
        }


        //从 App.config 中读取 MySQL 数据库连接字符串
        public string obtenerCadenaConexion()
        {
            string cadenaConexion = null;
            cadenaConexion = ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;

            return cadenaConexion;
        }


        //初始化海康威视设备 SDK
        public static bool InicializarSDKHikVision()
        {
            bool retval = false;
            if (HCNetSDK.NET_DVR_Init() == true)
            {
                retval = true;
            }
            return retval;
        }
        //创建数据目录（用于本地存储）
        public static bool CrearDirectorioData()
        {
            bool retval = false;

            string commonData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            commonData += @"\Neapps\ControlEntradaSalida\data";
            try
            {
                if (!Directory.Exists(commonData))
                    Directory.CreateDirectory(commonData);
                retval = true;
                datadir = commonData;
            } catch
            {
                retval = false;
            }

            return retval;

        }
        //使用海康威视 SDK 登录设备（提供 IP、端口、用户名、密码）
        public bool Login(string ip, string puerto, string usuario, string contrasena, out string msg)
        {
            bool ret = false;
            msg = null;

            HCNetSDK.NET_DVR_USER_LOGIN_INFO struLoginInfo = new HCNetSDK.NET_DVR_USER_LOGIN_INFO();
            HCNetSDK.NET_DVR_DEVICEINFO_V40 struDeviceInfoV40 = new HCNetSDK.NET_DVR_DEVICEINFO_V40();
            struDeviceInfoV40.struDeviceV30.sSerialNumber = new byte[HCNetSDK.SERIALNO_LEN];

            struLoginInfo.sDeviceAddress = ip;
            struLoginInfo.sUserName = usuario;
            struLoginInfo.sPassword = contrasena;
            ushort.TryParse(puerto, out struLoginInfo.wPort);

            int lUserID = -1;
            lUserID = HCNetSDK.NET_DVR_Login_V40(ref struLoginInfo, ref struDeviceInfoV40);
            if (lUserID >= 0)
            {
                Common.m_UserID = lUserID;
                ret = true;
            }
            else
            {
                uint nErr = HCNetSDK.NET_DVR_GetLastError();
                if (nErr == HCNetSDK.NET_DVR_PASSWORD_ERROR)
                {
                    msg = "User name or password error!";
                    if (1 == struDeviceInfoV40.bySupportLock)
                    {
                        string strTemp1 = string.Format("Left {0} try opportunity", struDeviceInfoV40.byRetryLoginTime);
                        msg += " " + strTemp1;
                    }
                }
                else if (nErr == HCNetSDK.NET_DVR_USER_LOCKED)
                {
                    if (1 == struDeviceInfoV40.bySupportLock)
                    {
                        string strTemp1 = string.Format("User is locked, the remaining lock time is {0}", struDeviceInfoV40.dwSurplusLockTime);
                        msg = strTemp1;
                    }
                }
                else
                {
                    msg = "Login fail error: " + nErr.ToString();
                }
            }
            return ret;
        }
    }
}
