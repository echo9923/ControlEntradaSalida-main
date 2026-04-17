# Members


- `dwSize`：结构体大小
- `byCardNo`：指纹关联的卡号
- `dwFingerPrintLen`：指纹数据长度
- `byEnableCardReader`：需要下发指纹的读卡器，数组下标表示读卡器序号，数组值：0- 不下发，1- 下发
- `byFingerPrintID`：指纹编号，有效值范围为1~10
- `byFingerType`：指纹类型：0- 普通指纹，1- 胁迫指纹
- `byRes1`：保留，置为0
- `byFingerData`：指纹数据内容
- `byRes`：保留，置为0
