# NET_DVR_FACE_PARAM_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_COND.html)

人脸参数配置条件结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byEnableCardReader[MAX_CARD_READER_NUM_512];
  DWORD    dwFaceNum;
  BYTE     byFaceID;
  BYTE     byFaceDataType;
  BYTE     byRes[126];
}NET_DVR_FACE_PARAM_COND, *LPNET_DVR_FACE_PARAM_COND;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：人脸关联的卡号
- `byEnableCardReader`：人脸的读卡器是否有效，按数组表示，每位数组表示一个读卡器，数组取值：0-无效，1-有效
- `dwFaceNum`：设置或获取人脸数量，获取时置为0xffffffff表示获取所有人脸信息
- `byFaceID`：人脸ID编号，有效取值范围：1~2，0xff表示该卡所有人脸
- `byFaceDataType`：人脸数据类型：0-模板（默认），1-图片
- `byRes`：保留，置为0

## Remarks

设备是否支持人脸参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_FACE.md)
