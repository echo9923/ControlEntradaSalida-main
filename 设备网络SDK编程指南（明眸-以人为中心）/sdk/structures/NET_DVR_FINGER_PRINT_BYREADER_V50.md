# NET_DVR_FINGER_PRINT_BYREADER_V50

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYREADER_V50.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYREADER_V50.html)

指纹读卡器参数信息结构体。

## 语法

```c
struct{
  DWORD                     dwCardReaderNo,
  BYTE                      byClearAllCard;
  BYTE                      byRes1[3];
  BYTE                      byCardNo[ACS_CARD_NO_LEN];
  BYTE                      byRes[100];
}NET_DVR_FINGER_PRINT_BYREADER_V50,*LPNET_DVR_FINGER_PRINT_BYREADER_V50;
```

## Members

- `dwCardReaderNo`：按值表示，指纹读卡器编号
- `byClearAllCard`：是否删除所有卡的指纹信息，0-按卡号删除指纹信息，1-删除所有卡的指纹信息
- `byRes1`：保留，置为0
- `byCardNo`：指纹关联的卡号
- `byRes`：保留，置为0

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_FingerPrint.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig_ACS_FingerPrint.html.md)
