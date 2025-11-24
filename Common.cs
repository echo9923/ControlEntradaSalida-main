using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ControlEntradaSalida.Configuration;

//Common类，它实现了多个与海康威视设备通信、数据库连接及文件目录管理相关的工具函数。
namespace ControlEntradaSalida
{
    class Common
    {   //设备相关参数
        public static string datadir = null;//储存地址

        private uint iLastErr = 0;

        //使用海康威视 SDK 向设备发送 ISAPI（XML 配置）请求并接收结果
        public bool ISAPIQuery(int userID, string requestURL, string inputParam, out string outputResult, out string outputStatus)
        {
            outputResult = null;
            outputStatus = null;

            if (userID < 0)
            {
                outputResult = "设备未连接";
                return false;
            }

            HCNetSDK.NET_DVR_XML_CONFIG_INPUT inputStruct = new HCNetSDK.NET_DVR_XML_CONFIG_INPUT
            {
                byRes = new byte[32],
                dwSize = (uint)Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_XML_CONFIG_INPUT))
            };

            HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT outputStruct = new HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT
            {
                byRes = new byte[32],
                dwSize = (uint)Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT))
            };

            string url = requestURL ?? string.Empty;
            byte[] urlBytes = Encoding.UTF8.GetBytes(url);

            string payload = inputParam ?? string.Empty;
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            IntPtr urlPtr = IntPtr.Zero;
            IntPtr payloadPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            IntPtr statusPtr = IntPtr.Zero;

            try
            {
                urlPtr = Marshal.AllocHGlobal(urlBytes.Length + 1);
                if (urlBytes.Length > 0)
                {
                    Marshal.Copy(urlBytes, 0, urlPtr, urlBytes.Length);
                }
                Marshal.WriteByte(urlPtr, urlBytes.Length, 0);

                inputStruct.lpRequestUrl = urlPtr;
                inputStruct.dwRequestUrlLen = (uint)urlBytes.Length;

                payloadPtr = Marshal.AllocHGlobal(payloadBytes.Length + 1);
                if (payloadBytes.Length > 0)
                {
                    Marshal.Copy(payloadBytes, 0, payloadPtr, payloadBytes.Length);
                }
                Marshal.WriteByte(payloadPtr, payloadBytes.Length, 0);

                inputStruct.lpInBuffer = payloadPtr;
                inputStruct.dwInBufferSize = (uint)payloadBytes.Length;

                int outBufferSize = 3 * 1024 * 1024;
                int statusBufferSize = 4096 * 4;

                outputPtr = Marshal.AllocHGlobal(outBufferSize);
                statusPtr = Marshal.AllocHGlobal(statusBufferSize);

                outputStruct.lpOutBuffer = outputPtr;
                outputStruct.dwOutBufferSize = (uint)outBufferSize;
                outputStruct.lpStatusBuffer = statusPtr;
                outputStruct.dwStatusSize = (uint)statusBufferSize;

                bool success = HCNetSDK.NET_DVR_STDXMLConfig(userID, ref inputStruct, ref outputStruct);
                if (!success)
                {
                    iLastErr = HCNetSDK.NET_DVR_GetLastError();
                    outputStatus = PtrToStringUtf8(statusPtr);
                    outputResult = "NET_DVR_STDXMLConfig failed, error code= " + iLastErr;
                    return false;
                }

                outputResult = PtrToStringUtf8(outputPtr);
                outputStatus = PtrToStringUtf8(statusPtr);
                return true;
            }
            finally
            {
                if (urlPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(urlPtr);
                }

                if (payloadPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(payloadPtr);
                }

                if (outputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outputPtr);
                }

                if (statusPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(statusPtr);
                }
            }
        }

        private string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            int length = 0;
            while (Marshal.ReadByte(ptr, length) != 0)
            {
                length++;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }


        //从 App.config 中读取 MySQL 数据库连接字符串
        public string obtenerCadenaConexion()
        {
            string connectionString = ExternalConfiguration.Current.Database.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("外部配置文件未提供数据库连接字符串。");
            }

            return connectionString.Trim();
        }

        /// <summary>
        /// 使用海康 SDK 发送 ISAPI 请求并返回二进制结果（用于抓拍图片）。
        /// </summary>
        public bool ISAPIBinaryRequest(int userID, string requestURL, string inputParam, out byte[] outputBytes, out string outputStatus)
        {
            outputBytes = null;
            outputStatus = null;

            if (userID < 0)
            {
                outputStatus = "设备未连接";
                return false;
            }

            HCNetSDK.NET_DVR_XML_CONFIG_INPUT inputStruct = new HCNetSDK.NET_DVR_XML_CONFIG_INPUT
            {
                byRes = new byte[32],
                dwSize = (uint)Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_XML_CONFIG_INPUT))
            };

            HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT outputStruct = new HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT
            {
                byRes = new byte[32],
                dwSize = (uint)Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_XML_CONFIG_OUTPUT))
            };

            string url = requestURL ?? string.Empty;
            byte[] urlBytes = Encoding.UTF8.GetBytes(url);

            string payload = inputParam ?? string.Empty;
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            IntPtr urlPtr = IntPtr.Zero;
            IntPtr payloadPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            IntPtr statusPtr = IntPtr.Zero;

            try
            {
                urlPtr = Marshal.AllocHGlobal(urlBytes.Length + 1);
                if (urlBytes.Length > 0)
                {
                    Marshal.Copy(urlBytes, 0, urlPtr, urlBytes.Length);
                }
                Marshal.WriteByte(urlPtr, urlBytes.Length, 0);

                inputStruct.lpRequestUrl = urlPtr;
                inputStruct.dwRequestUrlLen = (uint)urlBytes.Length;

                payloadPtr = Marshal.AllocHGlobal(payloadBytes.Length + 1);
                if (payloadBytes.Length > 0)
                {
                    Marshal.Copy(payloadBytes, 0, payloadPtr, payloadBytes.Length);
                }
                Marshal.WriteByte(payloadPtr, payloadBytes.Length, 0);

                inputStruct.lpInBuffer = payloadPtr;
                inputStruct.dwInBufferSize = (uint)payloadBytes.Length;

                int outBufferSize = 1024 * 1024; // 1MB 足够单帧 JPEG
                int statusBufferSize = 4096;

                outputPtr = Marshal.AllocHGlobal(outBufferSize);
                statusPtr = Marshal.AllocHGlobal(statusBufferSize);

                outputStruct.lpOutBuffer = outputPtr;
                outputStruct.dwOutBufferSize = (uint)outBufferSize;
                outputStruct.lpStatusBuffer = statusPtr;
                outputStruct.dwStatusSize = (uint)statusBufferSize;

                bool success = HCNetSDK.NET_DVR_STDXMLConfig(userID, ref inputStruct, ref outputStruct);
                if (!success)
                {
                    iLastErr = HCNetSDK.NET_DVR_GetLastError();
                    outputStatus = PtrToStringUtf8(statusPtr);
                    return false;
                }

                outputStatus = PtrToStringUtf8(statusPtr);

                // 读取实际长度：如果返回数据中没有 '\0'，默认整个缓冲有效；否则读到首个 0 为止
                int length = outBufferSize;
                for (int i = 0; i < outBufferSize; i++)
                {
                    if (Marshal.ReadByte(outputPtr, i) == 0)
                    {
                        length = i;
                        break;
                    }
                }

                if (length > 0)
                {
                    outputBytes = new byte[length];
                    Marshal.Copy(outputPtr, outputBytes, 0, length);
                }
                else
                {
                    outputBytes = Array.Empty<byte>();
                }

                return true;
            }
            finally
            {
                if (urlPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(urlPtr);
                }

                if (payloadPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(payloadPtr);
                }

                if (outputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outputPtr);
                }

                if (statusPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(statusPtr);
                }
            }
        }

        public int obtenerTiempoEsperaComando()
        {
            int? configuredTimeout = ExternalConfiguration.Current.Database.CommandTimeoutSeconds;
            return configuredTimeout.HasValue && configuredTimeout.Value > 0
                ? configuredTimeout.Value
                : 30;
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
        public bool Login(string ip, string puerto, string usuario, string contrasena, out int userID, out string msg)
        {
            bool ret = false;
            userID = -1;
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
                userID = lUserID;
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
