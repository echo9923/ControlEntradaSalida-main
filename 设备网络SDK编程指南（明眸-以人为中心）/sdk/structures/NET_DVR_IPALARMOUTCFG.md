# NET_DVR_IPALARMOUTCFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTCFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTCFG.html)

IP报警输出配置结构体。

## 语法

```c
struct{
  DWORD                      dwSize;
  NET_DVR_IPALARMOUTINFO     struIPAlarmOutInfo[MAX_IP_ALARMOUT];
}NET_DVR_IPALARMOUTCFG, *LPNET_DVR_IPALARMOUTCFG;
```

## Members

- `dwSize`：结构体大小
- `struIPAlarmOutInfo`：IP报警输出信息

## Remarks

1.IP报警输出资源只能获取，设备从IP设备资源获取对应的报警参数后进行紧凑排列，然后传给设备。

2.IP报警输出资源的下标索引值（0到MAX_IP_ALARMOUT -1）加上MAX_ANALOG_ALARMOUT对应的是报警输出相关参数（报警输出配置结构、联动触发报警输出等）的下标索引值（MAX_ANALOG_ALARMOUT到MAX_ALARMOUT_V30-1）。

## See Also

NET_DVR_GetDVRConfig

## Reference Structure

扩展结构可见

NET_DVR_IPALARMOUTCFG_V40

## 相关链接

- [NET_DVR_IPALARMOUTINFO](../structures/NET_DVR_IPALARMOUTINFO.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_old.md)
- [NET_DVR_IPALARMOUTCFG_V40](../structures/NET_DVR_IPALARMOUTCFG_V40.md)
