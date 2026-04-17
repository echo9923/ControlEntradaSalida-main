# NET_DVR_FACE_PARAM_BYCARD

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_BYCARD.html](https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_BYCARD.html)

按卡号删除人脸参数条件结构体。

## 语法

```c
struct{
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byEnableCardReader[MAX_CARD_READER_NUM_512];
  BYTE     byFaceID[MAX_FACE_NUM];
  BYTE     byRes1[42];
}NET_DVR_FACE_PARAM_BYCARD, *LPNET_DVR_FACE_PARAM_BYCARD;
```

## Members

- `byCardNo`：人脸关联的卡号
- `byEnableCardReader`：人脸读卡器信息，按数组表示，每位数组表示一个读卡器，取值：0-不删除，1-删除
- `byFaceID`：需要删除的人脸ID编号，按数组下标，每位数组表示一个人脸ID，取值：0-不删除，1-删除该人脸
- `byRes1`：保留，置为0

## See Also

NET_DVR_DEL_FACE_PARAM_MODE

## 相关链接

- [NET_DVR_DEL_FACE_PARAM_MODE](../structures/NET_DVR_DEL_FACE_PARAM_MODE.md)
