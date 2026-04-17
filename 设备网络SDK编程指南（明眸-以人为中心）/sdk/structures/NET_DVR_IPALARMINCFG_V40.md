# NET_DVR_IPALARMINCFG_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMINCFG_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPALARMINCFG_V40.html)

IP报警输入配置（扩展）结构体。

## 语法

```c
struct{
  DWORD                        dwSize;
  DWORD                        dwCurIPAlarmInNum;
  NET_DVR_IPALARMININFO_V40    struIPAlarmInInfo[MAX_IP_ALARMIN_V40];
  BYTE                         byRes[256];
}NET_DVR_IPALARMINCFG_V40, *LPNET_DVR_IPALARMINCFG_V40;
```

## Members

- `dwSize`：结构体大小
- `dwCurIPAlarmInNum`：当前报警输入口数
- `struIPAlarmInInfo`：IP报警输入信息
- `byRes`：保留

## See Also

NET_DVR_GetDVRConfig

## 相关链接

- [NET_DVR_IPALARMININFO_V40](../structures/NET_DVR_IPALARMININFO_V40.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_IPCHANCFG.md)
