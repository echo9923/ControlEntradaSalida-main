# NET_DVR_FINGER_PRINT_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_CFG.html)

指纹参数配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  DWORD    dwFingerPrintLen;
  BYTE     byEnableCardReader[MAX_CARD_READER_NUM_512];
  BYTE     byFingerPrintID;
  BYTE     byFingerType;
  BYTE     byRes1[30];
  BYTE     byFingerData[MAX_FINGER_PRINT_LEN];
  BYTE     byRes[64];
}NET_DVR_FINGER_PRINT_CFG, *LPNET_DVR_FINGER_PRINT_CFG;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：指纹关联的卡号
- `dwFingerPrintLen`：指纹数据长度
- `byEnableCardReader`：需要下发指纹的读卡器，数组下标表示读卡器序号，数组值：0- 不下发，1- 下发
- `byFingerPrintID`：指纹编号，有效值范围为1~10
- `byFingerType`：指纹类型：0- 普通指纹，1- 胁迫指纹
- `byRes1`：保留，置为0
- `byFingerData`：指纹数据内容
- `byRes`：保留，置为0

## Remarks

设备是否支持指纹参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_SendRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS.md)
- [NET_DVR_SendRemoteConfig](../definitions/NET_DVR_SendRemoteConfig_ACS.md)
