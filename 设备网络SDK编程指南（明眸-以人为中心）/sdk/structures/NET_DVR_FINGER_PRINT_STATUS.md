# NET_DVR_FINGER_PRINT_STATUS

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_STATUS.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_STATUS.html)

指纹状态参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byCardReaderRecvStatus[MAX_CARD_READER_NUM_512];
  BYTE     byFingerPrintID;
  BYTE     byFingerType;
  BYTE     byTotalStatus;
  BYTE     byRes1;
  BYTE     byErrorMsg[ERROR_MSG_LEN];
  DWORD    dwCardReaderNo;
  BYTE     byRes[24];
}NET_DVR_FINGER_PRINT_STATUS, *LPNET_DVR_FINGER_PRINT_STATUS;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：指纹关联的卡号
- `byCardReaderRecvStatus`：指纹读卡器状态，数组下标表示读卡器序号，数组值：0- 失败，1- 成功，2- 该指纹模组不在线，3- 重试或指纹质量差，4- 内存已满，5- 已存在该指纹，6- 已存在该指纹ID，7- 非法指纹ID，8- 该指纹模组无需配置，9- 指纹类型不支持
- `byFingerPrintID`：指纹编号，有效值范围为1~10
- `byFingerType`：指纹类型：0- 普通指纹，1- 胁迫指纹
- `byTotalStatus`：下发总的状态：0- 当前指纹未向所有读卡器下发完成，1- 当前指纹已向所有读卡器下发完成(指的是门禁主机往所有的读卡器下发了，不管成功与否)
- `byRes1`：保留
- `byErrorMsg`：下发错误信息，当byCardReaderRecvStatus为5时，表示已存在指纹对应的卡号
- `dwCardReaderNo`：指纹读卡器编号，可用于下发错误返回
- `byRes`：保留

## Remarks

设备是否支持指纹参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS.md)
