# NET_DVR_FACE_PARAM_BYREADER

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_BYREADER.html](https://open.hikvision.com/hardware/structures/NET_DVR_FACE_PARAM_BYREADER.html)

按读卡器删除人脸参数条件结构体。

## 语法

```c
struct{
  DWORD    dwCardReaderNo;
  BYTE     byClearAllCard;
  BYTE     byRes1[3];
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byRes[548];
}NET_DVR_FACE_PARAM_BYREADER, *LPNET_DVR_FACE_PARAM_BYREADER;
```

## Members

- `dwCardReaderNo`：人脸读卡器编号（0-主机）
- `byClearAllCard`：是否删除所有卡的人脸信息：0- 按卡号删除人脸信息，1- 删除所有卡的人脸信息
- `byRes1`：保留，置为0
- `byCardNo`：人脸关联的卡号，byClearAllCard为0时有效
- `byRes`：保留，置为0

## See Also

NET_DVR_DEL_FACE_PARAM_MODE

## 相关链接

- [NET_DVR_DEL_FACE_PARAM_MODE](../structures/NET_DVR_DEL_FACE_PARAM_MODE.md)
