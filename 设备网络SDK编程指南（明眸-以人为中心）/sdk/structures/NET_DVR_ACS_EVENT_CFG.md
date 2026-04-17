# NET_DVR_ACS_EVENT_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ACS_EVENT_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_ACS_EVENT_CFG.html)

门禁主机报警事件配置结构体。

## 语法

```c
struct{
  DWORD                     dwSize;
  DWORD                     dwMajor;
  DWORD                     dwMinor;
  NET_DVR_TIME              struTime;
  BYTE                      sNetUser[MAX_NAMELEN];
  NET_DVR_IPADDR              struRemoteHostAddr;
  NET_DVR_ACS_EVENT_DETAIL              struAcsEventInfo;
   DWORD                     dwPicDataLen;
   char                      *pPicData;
  BYTE                      byRes[64];
}NET_DVR_ACS_EVENT_DETAIL,*LPNET_DVR_ACS_EVENT_DETAIL;
```

## Members

- `dwSize`：结构体大小
- `dwMajor`：报警主类型，参考宏定义
- `dwMinor`：报警次类型，参考宏定义
- `struTime`：时间
- `struRemoteHostAddr`：远程主机地址
- `struAcsEventInfo`：详细参数
- `dwPicDataLen`：图片数据大小，不为0是表示后面带数据
- `pPicData`：图片数据
- `byRes`：保留，置为0

## Remarks

门禁主机报警信息获取只能通过NET_DVR_SetDVRMessageCallBack_V31设置报警回调函数，回调函数有返回值，需要返回TRUE告知设备已经接收数据，设备才会上传下一条信息。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_SetDVRMessageCallBack_V31

## 相关链接

- [NET_DVR_TIME](../structures/NET_DVR_TIME.md)
- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_ACS_EVENT_DETAIL](../structures/NET_DVR_ACS_EVENT_DETAIL.md)
- [NET_DVR_SetDVRMessageCallBack_V31](../definitions/NET_DVR_SetDVRMessageCallBack_V31.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_Event.md)
