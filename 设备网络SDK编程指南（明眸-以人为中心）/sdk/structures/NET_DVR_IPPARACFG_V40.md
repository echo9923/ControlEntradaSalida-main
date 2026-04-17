# NET_DVR_IPPARACFG_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPPARACFG_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPPARACFG_V40.html)

IP设备资源及IP通道资源配置结构体。

## 语法

```c
struct{
  DWORD                   dwSize;
  DWORD                   dwGroupNum;
  DWORD                   dwAChanNum;
  DWORD                   dwDChanNum;
  DWORD                   dwStartDChan;
  BYTE                    byAnalogChanEnable[MAX_CHANNUM_V30];
  NET_DVR_IPDEVINFO_V31   struIPDevInfo[MAX_IP_DEVICE_V40];
  NET_DVR_STREAM_MODE     struStreamMode[MAX_CHANNUM_V30];
  BYTE                    byRes2[20];
}NET_DVR_IPPARACFG_V40, *LPNET_DVR_IPPARACFG_V40;
```

## Members

- `dwSize`：结构体大小
- `dwGroupNum`：设备支持的总组数（只读）。若设备支持的组数大于1，NET_DVR_GetDVRConfig（或者NET_DVR_SetDVRConfig）获取（或设置）相关通道参数需要按照组数调用多次命令分别获取（或设置）各组通道参数，此时接口中lChannel对应组号。
- `dwAChanNum`：最大模拟通道个数（只读）
- `dwDChanNum`：数字通道个数（只读）
- `dwStartDChan`：起始数字通道（只读）
- `byAnalogChanEnable`：模拟通道资源是否启用，数组下标与通道号一一对应，取值：0-禁用，1-启用。

例如：byAnalogChanEnable[i]=1表示通道(i+1)启用
- `struIPDevInfo`：IP设备信息，下标0对应设备IP ID为1
- `struStreamMode`：取流模式
- `byRes2`：保留，置为0

## See Also

NET_DVR_GetDVRConfig  NET_DVR_SetDVRConfig

## Reference Structure

该结构扩展源于

NET_DVR_IPPARACFG

## 相关链接

- [NET_DVR_IPDEVINFO_V31](../structures/NET_DVR_IPDEVINFO_V31.md)
- [NET_DVR_STREAM_MODE](../structures/NET_DVR_STREAM_MODE.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_IPCHANCFG.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_IPCHANCFG.md)
- [NET_DVR_IPPARACFG](../structures/NET_DVR_IPPARACFG.md)
