# NET_DVR_IPALARMOUTCFG_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTCFG_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMOUTCFG_V40.html)

报警输入参数（扩展）结构体。

## 语法

```c
struct{
  DWORD                         dwSize;
  DWORD                         dwCurIPAlarmOutNum;
  NET_DVR_IPALARMOUTINFO_V40    struIPAlarmOutInfo[MAX_IP_ALARMOUT_V40];
  BYTE                          byRes[256];
}NET_DVR_IPALARMOUTCFG_V40,*LPNET_DVR_IPALARMOUTCFG_V40;
```

## Members

- `dwSize`：结构体大小
- `dwCurIPAlarmOutNum`：当前报警输出口数
- `struIPAlarmOutInfo`：IP报警输出口信息
- `byRes`：保留，置为0

## See Also

NET_DVR_GetDVRConfig

## Reference Structure

该结构扩展源于

NET_DVR_IPALARMOUTCFG

## 相关链接

- [NET_DVR_IPALARMOUTINFO_V40](NET_DVR_IPALARMOUTINFO_V40.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_IPCHANCFG.md)
- [NET_DVR_IPALARMOUTCFG](../structures/NET_DVR_IPALARMOUTCFG.md)
