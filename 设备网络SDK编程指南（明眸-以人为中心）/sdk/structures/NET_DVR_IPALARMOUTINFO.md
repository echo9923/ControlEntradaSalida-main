# NET_DVR_IPALARMOUTINFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTINFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTINFO.html)

IP报警输出信息结构体。

## 语法

```c
struct{
  BYTE     byIPID;
  BYTE     byAlarmOut;
  BYTE     byRes[18];
}NET_DVR_IPALARMOUTINFO, *LPNET_DVR_IPALARMOUTINFO;
```

## Members

- `byIPID`：IP设备ID，取值范围[1,MAX_IP_DEVICE]，其中#define MAX_IP_DEVICE 32
- `byAlarmOut`：报警输出号
- `byRes`：保留，置为0

## See Also

NET_DVR_IPALARMINFO   NET_DVR_IPALARMINFO_V31

NET_DVR_IPALARMOUTCFG

## 相关链接

- [NET_DVR_IPALARMINFO](../structures/NET_DVR_IPALARMINFO.md)
- [NET_DVR_IPALARMINFO_V31](../structures/NET_DVR_IPALARMINFO_V31.md)
- [NET_DVR_IPALARMOUTCFG](../structures/NET_DVR_IPALARMOUTCFG.md)
