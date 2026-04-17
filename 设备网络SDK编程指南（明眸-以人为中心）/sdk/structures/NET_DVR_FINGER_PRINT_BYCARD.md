# NET_DVR_FINGER_PRINT_BYCARD

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYCARD.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_BYCARD.html)

按卡号方式删除指纹的处理方式结构体。

## 语法

```c
struct{
  BYTE    byCardNo[ACS_CARD_NO_LEN];
  BYTE    byEnableCardReader[MAX_CARD_READER_NUM_512];
  BYTE    byFingerPrintID[MAX_FINGER_PRINT_NUM];
  BYTE    byRes1[34];
}NET_DVR_FINGER_PRINT_BYCARD, *LPNET_DVR_FINGER_PRINT_BYCARD;
```

## Members

- `byCardNo`：指纹关联的卡号
- `byEnableCardReader`：指纹的读卡器是否有效，数组下标表示读卡器序号，数组值：0- 无效，1- 有效
- `byFingerPrintID`：需要控制的指纹，数组下标表示指纹编号，数组值：0- 不删除，1- 删除
- `byRes1`：保留

## See Also

NET_DVR_DEL_FINGER_PRINT_MODE

## 相关链接

- [NET_DVR_DEL_FINGER_PRINT_MODE](../structures/NET_DVR_DEL_FINGER_PRINT_MODE.md)
