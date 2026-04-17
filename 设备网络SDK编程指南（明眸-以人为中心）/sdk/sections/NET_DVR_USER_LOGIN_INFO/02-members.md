# Members


- `sDeviceAddress`：设备地址，IP 或者普通域名
- `byUseTransport`：是否启用能力集透传：0- 不启用透传，默认；1- 启用透传
- `wPort`：设备端口号，例如：8000
- `sUserName`：登录用户名，例如：admin
- `sPassword`：登录密码，例如：12345
- `cbLoginResult`：登录状态回调函数，bUseAsynLogin 为1时有效
- `pUser`：用户数据
- `bUseAsynLogin`：是否异步登录：0- 否，1- 是
- `byProxyType`：代理服务器类型：0- 不使用代理，1- 使用标准代理，2- 使用EHome代理
- `byUseUTCTime`：是否使用UTC时间：0- 不进行转换，默认；1- 输入输出UTC时间，SDK进行与设备时区的转换；2- 输入输出平台本地时间，SDK进行与设备时区的转换
- `byLoginMode`：登录模式(不同模式具体含义详见“Remarks”说明)：0- SDK私有协议，1- ISAPI协议，2- 自适应（设备支持协议类型未知时使用，一般不建议）
- `byHttps`：ISAPI协议登录时是否启用HTTPS(byLoginMode为1时有效)：0- 不启用，1- 启用，2- 自适应（设备支持协议类型未知时使用，一般不建议）
- `iProxyID`：代理服务器序号，添加代理服务器信息时相对应的服务器数组下表值
- `byRes3`：保留，置为0
