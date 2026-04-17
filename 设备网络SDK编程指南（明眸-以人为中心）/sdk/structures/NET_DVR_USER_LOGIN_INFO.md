# NET_DVR_USER_LOGIN_INFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_USER_LOGIN_INFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_USER_LOGIN_INFO.html)

用户登录参数结构体。

## 语法

```c
struct{
  char                    sDeviceAddress[NET_DVR_DEV_ADDRESS_MAX_LEN];
  BYTE                    byUseTransport;
  WORD                    wPort;
  char                    sUserName[NET_DVR_LOGIN_USERNAME_MAX_LEN];
  char                    sPassword[NET_DVR_LOGIN_PASSWD_MAX_LEN];
  fLoginResultCallBack    cbLoginResult;
  void                    *pUser;
  BOOL                    bUseAsynLogin;
  BYTE                    byProxyType;
  BYTE                    byUseUTCTime;
  BYTE                    byLoginMode;
  BYTE                    byHttps;
  LONG                    iProxyID;
  BYTE                    byRes3[120];
}NET_DVR_USER_LOGIN_INFO,*LPNET_DVR_USER_LOGIN_INFO;
```

## Members

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

## Callback Function

```text
typedef void(CALLBACK *fLoginResultCallBack)(
  LONG                        lUserID,
  DWORD                       dwResult,
  LPNET_DVR_DEVICEINFO_V30    lpDeviceInfo,
  void                        *pUser
);
```

typedef void(CALLBACK *fLoginResultCallBack)(
  LONG                        lUserID,
  DWORD                       dwResult,
  LPNET_DVR_DEVICEINFO_V30    lpDeviceInfo,
  void                        *pUser
);

## Callback Function Parameters

- `lUserID`：[out] 用户ID，NET_DVR_Login_V40的返回值
- `dwResult`：[out] 登录状态：0- 异步登录失败，1- 异步登录成功
- `lpDeviceInfo`：[out] 设备信息，设备序列号、通道、能力等参数
- `pUser`：[out] 用户数据

## Remarks

设备登录模式有两种：SDK私有协议和ISAPI协议。

1) SDK私有协议是我司私有的TCP/IP协议，登录使用的是设备服务端口（默认为8000），我司网络设备除特殊产品外基本都支持该协议方式登录，因此一般建议使用SDK私有协议模式登录。

2) ISAPI协议是基于标准HTTP REST架构，HTTP协议或者HTTPS协议访问设备，登录使用的是设备HTTP端口（默认为80）或者HTTPS端口（默认为443）。不支持SDK私有协议的设备如猎鹰、刀锋等采用ISAPI协议的方式登录。

使用ISAPI协议方式登录时byUseTransport、cbLoginResult、pUser、bUseAsynLogin、byProxyType、byUseUTCTime、iProxyID这些参数都不支持。

## See Also

NET_DVR_Login_V40

## 相关链接

- [LPNET_DVR_DEVICEINFO_V30](../structures/NET_DVR_DEVICEINFO_V30.md)
- [NET_DVR_Login_V40](../definitions/NET_DVR_Login_V40.md)
