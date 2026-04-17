# NET_DVR_ACS_EVENT_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ACS_EVENT_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_ACS_EVENT_COND.html)

门禁主机报警事件信息结构体。

## 语法

```c
struct{
  DWORD                     dwSize;
  DWORD                     dwMajor;
  DWORD                     dwMinor;
  NET_DVR_TIME              struStartTime;
  NET_DVR_TIME              struEndTime;
  BYTE                      byCardNo[ACS_CARD_NO_LEN];
  BYTE                      byName[NAME_LEN];
  BYTE                      byPicEnable;
  BYTE                      byRes2[3];
  DWORD                     dwBeginSerialNo;
  DWORD                     dwEndSerialNo;
  BYTE                      byRes[244];
}NET_DVR_ACS_EVENT_COND,*LPNET_DVR_ACS_EVENT_COND;
```

## Members

- `dwSize`：结构体大小
- `dwMajor`：报警主类型，参考事件上传宏定义，0-全部
- `dwMinor`：报警次类型，参考事件上传宏定义，0-全部
- `struStartTime`：开始时间
- `struEndTime`：结束时间
- `byCardNo`：卡号（为空时默认全部）
- `byName`：持卡人姓名（为空时默认全部）
- `byPicEnable`：是否带图片，0-不带图片，1-带图片
- `byRes2`：保留，置为0
- `dwBeginSerialNo`：起始流水号（起始流水号与结束流水号都为0默认全部）
- `dwEndSerialNo`：结束流水号（起始流水号与结束流水号都为0默认全部）
- `byRes`：保留，置为0

## Remarks

门禁主机报警信息获取只能通过NET_DVR_SetDVRMessageCallBack_V31设置报警回调函数，回调函数有返回值，需要返回TRUE告知设备已经接收数据，设备才会上传下一条信息。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_TIME](../structures/NET_DVR_TIME.md)
- [NET_DVR_SetDVRMessageCallBack_V31](../definitions/NET_DVR_SetDVRMessageCallBack_V31.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_Event.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig_ACS_Event.md)
