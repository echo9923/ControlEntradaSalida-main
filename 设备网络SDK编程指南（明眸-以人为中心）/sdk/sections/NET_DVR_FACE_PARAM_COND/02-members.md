# Members


- `dwSize`：结构体大小
- `byCardNo`：人脸关联的卡号
- `byEnableCardReader`：人脸的读卡器是否有效，按数组表示，每位数组表示一个读卡器，数组取值：0-无效，1-有效
- `dwFaceNum`：设置或获取人脸数量，获取时置为0xffffffff表示获取所有人脸信息
- `byFaceID`：人脸ID编号，有效取值范围：1~2，0xff表示该卡所有人脸
- `byFaceDataType`：人脸数据类型：0-模板（默认），1-图片
- `byRes`：保留，置为0
