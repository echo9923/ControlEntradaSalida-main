# Members


- `dwSize`：结构体大小
- `byCardNo`：人脸关联的卡号
- `dwFaceLen`：人脸数据长度
- `pFaceBuffer`：人脸数据缓冲区指针，dwFaceLen不为0时存放人脸数据（DES加密处理，设备端返回的即加密后的数据）
- `byEnableCardReader`：需要下发人脸的读卡器，按数组表示，每位数组表示一个读卡器，数组取值：0-不下发该读卡器，1-下发到该读卡器
- `byFaceID`：人脸ID编号，有效取值范围：1~2
- `byFaceDataType`：人脸数据类型：0- 模板（默认），1- 图片
- `byRes`：保留，置为0
