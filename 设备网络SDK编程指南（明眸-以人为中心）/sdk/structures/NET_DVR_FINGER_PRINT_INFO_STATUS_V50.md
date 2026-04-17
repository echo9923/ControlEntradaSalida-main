# NET_DVR_FINGER_PRINT_INFO_STATUS_V50

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_INFO_STATUS_V50.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_INFO_STATUS_V50.html)

指纹信息状态结构体。

## 语法

```c
struct{
  DWORD                    dwSize;
  BYTE                     dwCardReaderNo;
  BYTE                     byStatus;
  BYTE                     byRes[63];
}NET_DVR_FINGER_PRINT_INFO_STATUS_V50, *LPNET_DVR_FINGER_PRINT_INFO_STATUS_V50;
```

## Members

- `dwSize`：结构体大小
- `dwCardReaderNo`：按值表示，指纹读卡器编号
- `byStatus`：状态：0-无效，1-处理中，2-删除失败，3-成功
- `byRes`：保留，置为0

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_FingerPrint.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig_ACS_FingerPrint.html.md)
