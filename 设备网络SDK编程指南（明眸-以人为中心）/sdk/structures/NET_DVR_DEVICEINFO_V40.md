# NET_DVR_DEVICEINFO_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_DEVICEINFO_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_DEVICEINFO_V40.html)

设备参数结构体。

## 语法

```c
struct{
  NET_DVR_DEVICEINFO_V30    struDeviceV30;
  BYTE                      bySupportLock;
  BYTE                      byRetryLoginTime;
  BYTE                      byPasswordLevel;
  BYTE                      byProxyType;
  DWORD                     dwSurplusLockTime;
  BYTE                      byCharEncodeType;
  BYTE                      bySupportDev5;
  BYTE                      byLoginMode;
  BYTE                      byRes2[253];
}NET_DVR_DEVICEINFO_V40,*LPNET_DVR_DEVICEINFO_V40;
```

## Members

- `struDeviceV30`：设备参数
- `bySupportLock`：设备是否支持锁定功能，bySupportLock为1时，dwSurplusLockTime和byRetryLoginTime有效
- `byRetryLoginTime`：剩余可尝试登陆的次数，用户名、密码错误时，此参数有效
- `byPasswordLevel`：密码安全等级：0- 无效，1- 默认密码，2- 有效密码，3- 风险较高的密码，当管理员用户的密码为出厂默认密码（12345）或者风险较高的密码时，建议上层客户端提示用户更改密码
- `byProxyType`：代理服务器类型：0- 不使用代理，1- 使用标准代理，2- 使用EHome代理
- `dwSurplusLockTime`：剩余时间，单位：秒，用户锁定时此参数有效。在锁定期间，用户尝试登陆，不管用户名密码输入对错，设备锁定剩余时间重新恢复到30分钟
- `byCharEncodeType`：字符编码类型（SDK所有接口返回的字符串编码类型，透传接口除外）：0- 无字符编码信息(老设备)，1- GB2312(简体中文)，2- GBK，3- BIG5(繁体中文)，4- Shift_JIS(日文)，5- EUC-KR(韩文)，6- UTF-8，7- ISO8859-1，8- ISO8859-2，9- ISO8859-3，…，依次类推，21- ISO8859-15(西欧)
- `bySupportDev5`：支持v50版本的设备参数获取，设备名称和设备类型名称长度扩展为64字节
- `byLoginMode`：登录模式(不同模式具体含义详见“Remarks”说明)：0- SDK私有协议，1- ISAPI协议
- `byRes2`：保留，置为0

## Remarks

将密码输入分为数字(0~9)、小写字母(a~z)、大写字母(A~Z)、特殊符号（:\"除外）4类，等级分为4个等级，如下所示：

等级0（风险密码）：密码长度小于8位，或者只包含4类字符中的任意一类，或者密码与用户名一样，或者密码是用户名的倒写。例如：12345、abcdef。

等级1（弱密码）：包含两类字符，且组合为（数字+小写字母）或（数字+大写字母），且长度大于等于8位。例如：abc12345、123ABCDEF

等级2（中密码）：包含两类字符，且组合不能为（数字+小写字母）和（数字+大写字母），且长度大于等于8位。例如：12345***++、ABCDabcd。

等级3（强密码）：包含三类字符及以上，且长度大于等于8位。例如：Abc12345、abc12345++。

设备登录模式有两种：SDK私有协议和ISAPI协议。

1) SDK私有协议是我司私有的TCP/IP协议，登录使用的是设备服务端口（默认为8000），我司网络设备除特殊产品外基本都支持该协议方式登录，因此一般建议使用SDK私有协议模式登录。

2) ISAPI协议是基于标准HTTP REST架构，HTTP协议或者HTTPS协议访问设备，登录使用的是设备HTTP端口（默认为80）或者HTTPS端口（默认为443）。不支持SDK私有协议的设备如猎鹰、刀锋等采用ISAPI协议的方式登录。

使用ISAPI协议方式登录时bySupportLock、byRetryLoginTime、byPasswordLevel、byProxyType、dwSurplusLockTime、byCharEncodeType、bySupportDev5这些参数都不支持。

## See Also

NET_DVR_Login_V40

## 相关链接

- [NET_DVR_DEVICEINFO_V30](../structures/NET_DVR_DEVICEINFO_V30.md)
- [NET_DVR_Login_V40](../definitions/NET_DVR_Login_V40.md)
