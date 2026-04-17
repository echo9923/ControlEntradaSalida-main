# NET_DVR_FINGER_PRINT_BYREADER

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYREADER.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYREADER.html)

按按读卡器方式删除指纹的处理方式结构体。

## 语法

```c
struct{
  DWORD    dwCardReaderNo;
  BYTE     byClearAllCard;
  BYTE     byRes1[3];
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byRes[100];
}NET_DVR_FINGER_PRINT_BYREADER, *LPNET_DVR_FINGER_PRINT_BYREADER;
```

## Members

- `dwCardReaderNo`：读卡器编号
- `byClearAllCard`：是否删除所有卡的指纹信息：0- 按卡号删除指纹信息，1- 删除所有卡的指纹信息
- `byRes1`：保留，置为0
- `byCardNo`：指纹关联的卡号，byClearAllCard为0时有效
- `byRes`：保留，置为0

## See Also

NET_DVR_DEL_FINGER_PRINT_MODE

## 相关链接

- [NET_DVR_DEL_FINGER_PRINT_MODE](../structures/NET_DVR_DEL_FINGER_PRINT_MODE.md)
