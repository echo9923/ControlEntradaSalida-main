# NET_DVR_FINGER_PRINT_INFO_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_INFO_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_INFO_COND.html)

指纹参数配置条件结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byEnableCardReader[MAX_CARD_READER_NUM_512];
  DWORD    dwFingerPrintNum;
  BYTE     byFingerPrintID;
  BYTE     byCallbackMode;
  BYTE     byRes1[26];
}NET_DVR_FINGER_PRINT_INFO_COND, *LPNET_DVR_FINGER_PRINT_INFO_COND;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：指纹关联的卡号
- `byEnableCardReader`：指纹的读卡器是否有效，数组下标表示读卡器序号，数组值：0- 无效，1- 有效
- `dwFingerPrintNum`：设置或获指纹数量，获取时置为0xffffffff表示获取所有指纹信息
- `byFingerPrintID`：指纹编号，有效值范围为1~10，获取时置为0xff表示该卡所有指纹
- `byCallbackMode`：设备回调方式：0- 已向所有读卡器下发完成，1- 在时间段内只下发了部分也返回
- `byRes1`：保留

## Remarks

设备是否支持指纹参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS.md)
