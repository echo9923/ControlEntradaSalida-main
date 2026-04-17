# NET_DVR_ACS_ALARM_INFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ACS_ALARM_INFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_ACS_ALARM_INFO.html)

门禁主机报警信息结构体。

## 语法

```c
struct{
  DWORD                     dwSize;
  DWORD                     dwMajor;
  DWORD                     dwMinor;
  NET_DVR_TIME              struTime;
  BYTE                      sNetUser[MAX_NAMELEN];
  NET_DVR_IPADDR            struRemoteHostAddr ;
  NET_DVR_ACS_EVENT_INFO    struAcsEventInfo;
  DWORD                     dwPicDataLen;
  char                      *pPicData;
  BYTE                      byRes[24];
}NET_DVR_ACS_ALARM_INFO,*LPNET_DVR_ACS_ALARM_INFO;
```

## Members

- `dwSize`：结构体大小
- `dwMajor`：报警主类型，具体定义见“Remarks”说明
- `dwMinor`：报警次类型，次类型含义根据主类型不同而不同，具体定义见“Remarks”说明
- `struTime`：报警时间
- `sNetUser`：网络操作的用户名
- `struRemoteHostAddr`：远程主机地址
- `struAcsEventInfo`：报警信息详细参数
- `dwPicDataLen`：图片数据大小，不为0是表示后面带数据
- `pPicData`：图片数据缓冲区
- `byRes`：保留，置为0

## Remarks

门禁主机报警信息获取只能通过NET_DVR_SetDVRMessageCallBack_V31设置报警回调函数，回调函数有返回值，需要返回TRUE告知设备已经接收数据，设备才会上传下一条信息。

报警主类型定义如下所示：

根据不同的主类型的次类型定义如下所示：

## See Also

NET_DVR_SetDVRMessageCallBack_V31   NET_DVR_StartListen_V30

## 相关链接

- [NET_DVR_TIME](../structures/NET_DVR_TIME.md)
- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_ACS_EVENT_INFO](../structures/NET_DVR_ACS_EVENT_INFO.md)
- [NET_DVR_SetDVRMessageCallBack_V31](../definitions/NET_DVR_SetDVRMessageCallBack_V31.md)
- [NET_DVR_StartListen_V30](../definitions/NET_DVR_StartListen_V30.md)
