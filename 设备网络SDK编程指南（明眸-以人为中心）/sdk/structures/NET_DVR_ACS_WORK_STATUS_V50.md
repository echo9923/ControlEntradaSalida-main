# NET_DVR_ACS_WORK_STATUS_V50

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ACS_WORK_STATUS_V50.html](https://open.hikvision.com/hardware/structures/NET_DVR_ACS_WORK_STATUS_V50.html)

门禁主机工作状态结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byDoorLockStatus[MAX_DOOR_NUM_256];
  BYTE     byDoorStatus[MAX_DOOR_NUM_256];
  BYTE     byMagneticStatus[MAX_DOOR_NUM_256];
  BYTE     byCaseStatus[MAX_CASE_SENSOR_NUM];
  WORD     wBatteryVoltage;
  BYTE     byBatteryLowVoltage;
  BYTE     byPowerSupplyStatus;
  BYTE     byMultiDoorInterlockStatus;
  BYTE     byAntiSneakStatus;
  BYTE     byHostAntiDismantleStatus;
  BYTE     byIndicatorLightStatus;
  BYTE     byCardReaderOnlineStatus[MAX_CARD_READER_NUM_512];
  BYTE     byCardReaderAntiDismantleStatus[MAX_CARD_READER_NUM_512];
  BYTE     byCardReaderVerifyMode[MAX_CARD_READER_NUM_512];
  BYTE     bySetupAlarmStatus[MAX_ALARMHOST_ALARMIN_NUM];
  BYTE     byAlarmInStatus[MAX_ALARMHOST_ALARMIN_NUM];
  BYTE     byAlarmOutStatus[MAX_ALARMHOST_ALARMOUT_NUM];
  DWORD    dwCardNum;
  BYTE     byFireAlarmStatus;
  BYTE     byBatteryChargeStatus;
  BYTE     byMasterChannelControllerStatus;
  BYTE     bySlaveChannelControllerStatus;
  BYTE     byAntiSneakServerStatus; 
  BYTE     byRes3[3];
  DWORD    dwWhiteFaceNum;
  DWORD    dwBlackFaceNum;
  BYTE     byRes2[108];
}NET_DVR_ACS_WORK_STATUS_V50,*LPNET_DVR_ACS_WORK_STATUS_V50;
```

## Members

- `dwSize`：结构体大小
- `byDoorLockStatus`：门锁状态（或者梯控的继电器开合状态）：0- 正常关，1- 正常开，2- 短路报警，3- 断路报警，4- 异常报警
- `byDoorStatus`：门状态（或者梯控的楼层状态）：1- 休眠，2- 常开状态（对于梯控，表示自由状态），3- 常闭状态（对于梯控，表示禁用状态），4- 普通状态（对于梯控，表示受控状态）
- `byMagneticStatus`：门磁状态，0-正常关，1-正常开，2-短路报警，3-断路报警，4-异常报警
- `byCaseStatus`：事件报警输入状态：0- 无输入，1- 有输入
- `wBatteryVoltage`：蓄电池电压值，实际值乘10，单位：伏特
- `byBatteryLowVoltage`：蓄电池是否处于低压状态：0- 否，1- 是
- `byPowerSupplyStatus`：设备供电状态：1- 交流电供电，2- 蓄电池供电
- `byMultiDoorInterlockStatus`：多门互锁状态：0- 关闭，1- 开启
- `byAntiSneakStatus`：反潜回状态：0-关闭，1-开启
- `byHostAntiDismantleStatus`：主机防拆状态：0- 关闭，1- 开启
- `byIndicatorLightStatus`：指示灯状态，0-掉线，1-在线
- `byCardReaderOnlineStatus`：读卡器在线状态：0- 不在线，1- 在线
- `byCardReaderAntiDismantleStatus`：读卡器防拆状态：0- 关闭，1- 开启
- `byCardReaderVerifyMode`：读卡器当前验证方式：0- 无效，1- 休眠，2- 刷卡+密码，3- 刷卡，4- 刷卡或密码，5- 指纹，6- 指纹加密码，7- 指纹或刷卡，8- 指纹加刷卡，9- 指纹加刷卡加密码（无先后顺序），10- 人脸或指纹或刷卡或密码，11- 人脸+指纹，12- 人脸+密码，13- 人脸+刷卡，14- 人脸，15- 工号+密码，16- 指纹或密码，17- 工号+指纹，18- 工号+指纹+密码，19- 人脸+指纹+刷卡，20- 人脸+密码+指纹，21- 工号+人脸
- `bySetupAlarmStatus`：报警输入口布防状态：0- 对应报警输入口处于撤防状态，1- 对应报警输入口处于布防状态
- `byAlarmInStatus`：按字节表示报警输入口状态：0- 对应报警输入口当前无报警，1- 对应报警输入口当前有报警
- `byAlarmOutStatus`：按字节表示报警输出口状态：0- 对应报警输出口无报警，1- 对应报警输出口有报警
- `dwCardNum`：已添加的卡数量
- `byFireAlarmStatus`：消防报警状态显示：0-正常、1-短路报警、2-断开报警
- `byBatteryChargeStatus`：电池充电状态：0-无效；1-充电中；2-未充电
- `byMasterChannelControllerStatus`：主通道控制器在线状态：0-无效；1-不在线；2-在线
- `bySlaveChannelControllerStatus`：从通道控制器在线状态：0-无效；1-不在线；2-在线
- `byAntiSneakServerStatus`：反潜回服务器状态：0-无效，1-未启用，2-正常，3-断开
- `byRes3`：保留参数
- `dwWhiteFaceNum`：已添加的白名单人脸数量（通过能力集判断）
- `dwBlackFaceNum`：已添加的黑名单人脸数量（通过能力集判断）
- `byRes2`：保留，置为0

## Remarks

对应的能力集：为ACS_ABILITY能力集的节点。

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
