# NET_DVR_EVENT_CARD_LINKAGE_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_EVENT_CARD_LINKAGE_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_EVENT_CARD_LINKAGE_COND.html)

事件/卡号联动配置条件结构体。

## 语法

```c
struct{
  DWORD   dwSize;
  DWORD   dwEventID;
  WORD    wLocalControllerID;
  BYTE    byRes[106];
}NET_DVR_EVENT_CARD_LINKAGE_COND,*LPNET_DVR_EVENT_CARD_LINKAGE_COND;
```

## Members

- `dwSize`：结构体大小
- `dwEventID`：事件ID
- `wLocalControllerID`：就地控制器序号[1,64]，0表示门禁主机（目前设备只支持门禁主机）
- `byRes`：保留，置为0

## See Also

NET_DVR_GetDeviceConfig   NET_DVR_SetDeviceConfig

## 相关链接

- [NET_DVR_GetDeviceConfig](../definitions/NET_DVR_GetDeviceConfig_ACS.md)
- [NET_DVR_SetDeviceConfig](../definitions/NET_DVR_SetDeviceConfig_ACS.md)
