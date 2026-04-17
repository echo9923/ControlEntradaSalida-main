# NET_DVR_IPALARMININFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMININFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMININFO.html)

IP报警输入信息结构体。

## 语法

```c
struct{
  BYTE     byIPID;
  BYTE     byAlarmIn;
  BYTE     byRes[18];
}NET_DVR_IPALARMININFO, *LPNET_DVR_IPALARMININFO;
```

## Members

- `byIPID`：IP设备ID，取值范围[1,MAX_IP_DEVICE]，其中#define MAX_IP_DEVICE 32
- `byAlarmIn`：报警输入号
- `byRes`：保留，置为0

## See Also

NET_DVR_IPALARMINCFG   NET_DVR_IPALARMINFO    NET_DVR_IPALARMINFO_V31

## 相关链接

- [NET_DVR_IPALARMINCFG](../structures/NET_DVR_IPALARMINCFG.md)
- [NET_DVR_IPALARMINFO](../structures/NET_DVR_IPALARMINFO.md)
- [NET_DVR_IPALARMINFO_V31](../structures/NET_DVR_IPALARMINFO_V31.md)
