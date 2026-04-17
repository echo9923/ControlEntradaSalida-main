# NET_DVR_EVENT_CARD_LINKAGE_CFG_V51

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_EVENT_CARD_LINKAGE_CFG_V51.html](https://open.hikvision.com/hardware/structures/NET_DVR_EVENT_CARD_LINKAGE_CFG_V51.html)

事件/卡号联动配置结构体。

## 语法

```c
struct{
  DWORD                               dwSize;
  BYTE                                byProMode;
  BYTE                                byRes1[3];
  DWORD                               dwEventSourceID;
  NET_DVR_EVETN_CARD_LINKAGE_UNION    uLinkageInfo;
  BYTE                                byAlarmout[MAX_ALARMHOST_ALARMOUT_NUM];
  BYTE                                byRes2[32];
  BYTE                                byOpenDoor[MAX_DOOR_NUM_256];
  BYTE                                byCloseDoor[MAX_DOOR_NUM_256];
  BYTE                                byNormalOpen[MAX_DOOR_NUM_256];
  BYTE                                byNormalClose[MAX_DOOR_NUM_256];
  BYTE                                byMainDevBuzzer;
  BYTE                                byCapturePic;
  BYTE                                byRecordVideo;
  BYTE                                byMainDevStopBuzzer;    
  BYTE                                byRes3[28];
  BYTE                                byReaderBuzzer[MAX_CARD_READER_NUM_512];
  BYTE                                byAlarmOutClose[MAX_ALARMHOST_ALARMOUT_NUM];
  BYTE                                byAlarmInSetup[MAX_ALARMHOST_ALARMOUT_NUM];
  BYTE                                byAlarmInClose[MAX_ALARMHOST_ALARMOUT_NUM];
  BYTE                                byReaderStopBuzzer[MAX_ALARMHOST_ALARMOUT_NUM];
  BYTE                                byRes[512];
}NET_DVR_EVENT_CARD_LINKAGE_CFG_V51, *LPNET_DVR_EVENT_CARD_LINKAGE_CFG_V50;
```

## Members

- `dwSize`：结构体大小
- `byProMode`：联动方式：0- 事件，1- 卡号
- `byRes1`：保留，置为0
- `dwEventSourceID`：事件源ID，0xffffffff表示联动全部，其他取值：当主类型为设备事件时无效；当主类型是为门事件时，为门编号；当主类型为读卡器事件时，为读卡器ID；当主类型为报警输入事件时，为防区报警输入ID或事件报警输入ID
- `uLinkageInfo`：联动方式参数
- `byAlarmout`：关联的报警输出号，按数组表示，数组下标表示报警输出口序号，数组值：0- 不关联，1- 关联
- `byRes2`：保留，置为0
- `byOpenDoor`：是否联动开门，按数组表示，数组下标表示门编号，数组值：0- 不联动，1- 联动
- `byCloseDoor`：是否联动关门，按数组表示，数组下标表示门编号，数组值：0- 不联动，1- 联动
- `byNormalOpen`：是否联动常开，按数组表示，数组下标表示门编号，数组值：0- 不联动，1- 联动
- `byNormalClose`：是否联动常关，按数组表示，数组下标表示门编号，数组值：0- 不联动，1- 联动
- `byMainDevBuzzer`：是否联动主机蜂鸣器：0- 不联动，1- 联动输出
- `byCapturePic`：是否联动抓拍：0- 不联动抓拍，1- 联动抓拍
- `byRecordVideo`：是否联动录像：0-不联动录像，1-联动录像
- `byMainDevStopBuzzer`：主机停止蜂鸣   0-不联动，1-联动输出
- `byRes3`：保留
- `byReaderBuzzer`：是否联动读卡器蜂鸣器，按数组表示，数组下标表示读卡器编号，数组值：0-不联动，1-联动
- `byAlarmOutClose`：关联报警输出关闭，按字节表示，为0表示不关联，为1表示关联
- `byAlarmInSetup`：关联防区布防，按字节表示，为0表示不关联，为1表示关联
- `byAlarmInClose`：关联防区撤防，按字节表示，为0表示不关联，为1表示关联
- `byReaderStopBuzzer`：联动读卡器停止蜂鸣，按字节表示，0-不联动，1-联动
- `byRes`：保留，置为0

## Remarks

设备是否支持事件/卡号联动配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_GetDeviceConfig   NET_DVR_SetDeviceConfig

## 相关链接

- [NET_DVR_EVETN_CARD_LINKAGE_UNION](../structures/NET_DVR_EVETN_CARD_LINKAGE_UNION.md)
- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_GetDeviceConfig](../definitions/NET_DVR_GetDeviceConfig_ACS.md)
- [NET_DVR_SetDeviceConfig](../definitions/NET_DVR_SetDeviceConfig_ACS.md)
