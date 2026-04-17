# Members


- `dwSize`：结构体大小
- `byDoorName`：门名称
- `byMagneticType`：门磁类型：0- 常闭，1- 常开
- `byOpenButtonType`：开门按钮类型：0- 常闭，1- 常开
- `byOpenDuration`：开门持续时间（楼层继电器动作时间），取值范围：1~255s
- `byDisabledOpenDuration`：残疾人卡开门持续时间，取值范围：1~255s
- `byMagneticAlarmTimeout`：门磁检测超时报警时间，取值范围：0~255s，0表示不报警
- `byEnableDoorLock`：是否启用闭门回锁：0- 否，1- 是
- `byEnableLeaderCard`：是否启用首卡常开功能：0- 否，1- 是
- `byLeaderCardMode`：首卡模式，0-不启用首卡功能，1-首卡常开模式，2-首卡授权模式（使用了此字段，则byEnableLeaderCard无效）
- `dwLeaderCardOpenDuration`：首卡常开持续时间，取值范围：1~1440，单位：min（分钟）
- `byStressPassword`：胁迫密码
- `bySuperPassword`：超级密码
- `byUnlockPassword`：解除码，解锁密码
- `byUseLocalController`：只读，是否连接在就地控制器上，0-否，1-是
- `byRes1`：保留，置为0
- `wLocalControllerID`：只读，就地控制器序号，byUseLocalController=1时有效，1-64,0代表未注册
- `wLocalControllerDoorNumber`：只读，就地控制器的门编号，byUseLocalController=1时有效，1-4,0代表未注册
- `wLocalControllerStatus`：只读，byUseLocalController=1时有效，就地控制器在线状态：0-离线，1-网络在线，2-环路1上的RS485串口1，3-环路1上的RS485串口2，4-环路2上的RS485串口1，5-环路2上的RS485串口2，6-环路3上的RS485串口1，7-环路3上的RS485串口2，8-环路4上的RS485串口1，9-环路4上的RS485串口2（只读）
- `byLockInputCheck`：是否启用门锁输入检测(1字节，0不启用，1启用，默认不启用)
- `BYTE byLockInputType`：门锁输入类型(1字节，0常闭，1常开，默认常闭)
- `byDoorTerminalMode`：门相关端子工作模式(1字节，0防剪防短，1普通，默认防剪防短)
- `byOpenButton`：是否启用开门按钮(0是，1否，默认是)
- `byLadderControlDelayTime`：梯控访客延迟时间，取值范围：1~255，单位：分钟
- `byRes2`：保留，置为0
