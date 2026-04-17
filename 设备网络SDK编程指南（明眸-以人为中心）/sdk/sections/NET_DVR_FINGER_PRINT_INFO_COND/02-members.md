# Members


- `dwSize`：结构体大小
- `byCardNo`：指纹关联的卡号
- `byEnableCardReader`：指纹的读卡器是否有效，数组下标表示读卡器序号，数组值：0- 无效，1- 有效
- `dwFingerPrintNum`：设置或获指纹数量，获取时置为0xffffffff表示获取所有指纹信息
- `byFingerPrintID`：指纹编号，有效值范围为1~10，获取时置为0xff表示该卡所有指纹
- `byCallbackMode`：设备回调方式：0- 已向所有读卡器下发完成，1- 在时间段内只下发了部分也返回
- `byRes1`：保留
