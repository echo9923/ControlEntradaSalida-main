# NET_DVR_IPALARMINCFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMINCFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMINCFG.html)

IP报警输入配置结构体。

## 语法

```c
struct{
  DWORD                      dwSize;
  NET_DVR_IPALARMININFO      struIPAlarmInInfo[MAX_IP_ALARMIN];
}NET_DVR_IPALARMINCFG, *LPNET_DVR_IPALARMINCFG;
```

## Members

- `dwSize`：结构体大小
- `struIPAlarmInInfo`：IP报警输入信息

## Remarks

1.IP报警输入资源只能获取，设备从IP设备资源获取对应的报警参数后进行紧凑排列，然后传给网络SDK。

2.IP报警输入资源的下标索引值（0到MAX_IP_ALARMIN -1）加上MAX_ANALOG_ALARMIN对应的是报警输入相关参数（报警输入配置结构等）的下标索引值（MAX_ANALOG_ALARMIN到MAX_ALARMIN_V30-1）。

## See Also

NET_DVR_GetDVRConfig  NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_IPALARMININFO](../structures/NET_DVR_IPALARMININFO.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_old.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_old.md)
