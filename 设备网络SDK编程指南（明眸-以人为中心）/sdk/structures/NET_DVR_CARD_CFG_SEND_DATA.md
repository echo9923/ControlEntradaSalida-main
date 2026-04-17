# NET_DVR_CARD_CFG_SEND_DATA

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CARD_CFG_SEND_DATA.html](https://open.hikvision.com/hardware/structures/NET_DVR_CARD_CFG_SEND_DATA.html)

获取卡参数的发送数据。

## 语法

```c
struct{
  DWORD   dwSize;
  BYTE    byCardNo[ACS_CARD_NO_LEN];
  DWORD   dwCardUserId;
  BYTE    byRes[12];
}NET_DVR_CARD_CFG_SEND_DATA,*LPNET_DVR_CARD_CFG_SEND_DATA;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：卡号
- `dwCardUserId`：持卡人ID
- `byRes`：保留，置为0

## See Also

NET_DVR_SendRemoteConfig

NET_DVR_GetDeviceConfig  NET_DVR_SetDeviceConfig

## 相关链接

- [NET_DVR_SendRemoteConfig](../definitions/NET_DVR_SendRemoteConfig_ACS.md)
- [NET_DVR_GetDeviceConfig](../definitions/NET_DVR_GetDeviceConfig_ACS.md)
- [NET_DVR_SetDeviceConfig](../definitions/NET_DVR_SetDeviceConfig_ACS.md)
