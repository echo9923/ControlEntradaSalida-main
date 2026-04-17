# NET_DVR_IPALARMOUTINFO_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTINFO_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTINFO_V40.html)

IP报警输出口信息结构体。

## 语法

```c
struct{
  DWORD    dwIPID;
  DWORD    dwAlarmOut;
  BYTE     byRes[32];
}NET_DVR_IPALARMOUTINFO_V40, *LPNET_DVR_IPALARMOUTINFO_V40;
```

## Members

- `dwIPID`：IP设备ID，取值范围[1,MAX_IP_DEVICE_V40]，其中#define MAX_IP_DEVICE_V40 64
- `dwAlarmOut`：IP设备ID对应的报警输出号
- `byRes`：保留，置为0

## See Also

NET_DVR_IPALARMOUTCFG_V40

## 相关链接

- [NET_DVR_IPALARMOUTCFG_V40](../structures/NET_DVR_IPALARMOUTCFG_V40.md)
