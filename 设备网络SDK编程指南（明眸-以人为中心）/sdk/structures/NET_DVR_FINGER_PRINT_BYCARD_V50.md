# NET_DVR_FINGER_PRINT_BYCARD_V50

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYCARD_V50.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYCARD_V50.html)

指纹关联卡号参数信息结构体。

## 语法

```c
struct{
  BYTE                      byCardNo[ACS_CARD_NO_LEN];
  BYTE                      byEnableCardReader[MAX_CARD_READER_NUM];
  BYTE                      byFingerPrintID[MAX_FINGER_PRINT_NUM];
  BYTE                      byRes1[34];
}NET_DVR_FINGER_PRINT_BYCARD_V50,*LPNET_DVR_FINGER_PRINT_BYCARD_V50;
```

## Members

- `byCardNo`：指纹关联的卡号
- `byEnableCardReader`：指纹的读卡器信息，按位表示
- `byFingerPrintID`：需要删除的指纹编号，按数组下标，值表示0-不删除，1-删除该指纹
- `byRes1`：保留，置为0

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_FingerPrint.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig_ACS_FingerPrint.html.md)
