# NET_DVR_FACE_PARAM_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_CFG.html)

人脸参数配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  DWORD    dwFaceLen;
  char     *pFaceBuffer;
  BYTE     byEnableCardReader[MAX_CARD_READER_NUM_512];
  BYTE     byFaceID;
  BYTE     byFaceDataType;
  BYTE     byRes[126];
}NET_DVR_FACE_PARAM_CFG, *LPNET_DVR_FACE_PARAM_CFG;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：人脸关联的卡号
- `dwFaceLen`：人脸数据长度
- `pFaceBuffer`：人脸数据缓冲区指针，dwFaceLen不为0时存放人脸数据（DES加密处理，设备端返回的即加密后的数据）
- `byEnableCardReader`：需要下发人脸的读卡器，按数组表示，每位数组表示一个读卡器，数组取值：0-不下发该读卡器，1-下发到该读卡器
- `byFaceID`：人脸ID编号，有效取值范围：1~2
- `byFaceDataType`：人脸数据类型：0- 模板（默认），1- 图片
- `byRes`：保留，置为0

## Remarks

设备是否支持人脸参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_SendRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_FACE.md)
- [NET_DVR_SendRemoteConfig](../definitions/NET_DVR_SendRemoteConfig_ACS_FACE.md)
