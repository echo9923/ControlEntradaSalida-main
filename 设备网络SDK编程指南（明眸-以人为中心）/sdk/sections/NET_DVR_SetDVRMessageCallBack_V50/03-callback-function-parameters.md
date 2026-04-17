# Callback Function Parameters


- `lCommand`：[out] 上传的消息类型，不同的报警信息对应不同的类型，通过类型区分是什么报警信息，详见“Remarks”中列表
- `pAlarmer`：[out] 报警设备信息，包括设备序列号、IP地址、登录IUserID句柄等
- `pAlarmInfo`：[out] 报警信息，通过lCommand值判断pAlarmer对应的结构体，详见“Remarks”中列表
- `dwBufLen`：[out] 报警信息缓存大小
- `pUser`：[out] 用户数据
