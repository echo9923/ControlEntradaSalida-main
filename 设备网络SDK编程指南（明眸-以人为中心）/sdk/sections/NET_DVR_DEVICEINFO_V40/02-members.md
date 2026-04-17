# Members


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
