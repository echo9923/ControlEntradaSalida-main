# NET_DVR_IPCHANINFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPCHANINFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPCHANINFO.html)

IP通道信息结构体。

## 语法

```c
struct{
  BYTE     byEnable;
  BYTE     byIPID;
  BYTE     byChannel;
  BYTE     byIPIDHigh;
  BYTE     byRes[32];
}NET_DVR_IPCHANINFO, *LPNET_DVR_IPCHANINFO;
```

## Members

- `byEnable`：IP通道在线状态，是一个只读的属性；0表示HDVR或者NVR设备的数字通道连接对应的IP设备失败，该通道不在线；1表示连接成功，该通道在线
- `byIPID`：IP设备ID的低8位，byIPID = iDevID % 256
- `byChannel`：IP设备的通道号，例如设备A（HDVR或者NVR设备）的IP通道01，对应的是设备B里的通道04，则byChannel=4。
- `byIPIDHigh`：IP设备ID的高8位，byIPIDHigh = iDevID /256
- `byRes`：保留，置为0

## Remarks

iDevID为设备ID号，iDevID = byIPIDHigh*256 + byIPID。通过iDevID值查找具体的设备信息struIPDevInfo（结构体NET_DVR_IPPARACFG_V40的数组参数），与设备信息数组下标（iDevInfoIndex）换算关系为：iDevID = iDevInfoIndex + iGroupNO*64 +1。

## See Also

NET_DVR_GET_STREAM_UNION   NET_DVR_IPALARMINFO  
    NET_DVR_IPALARMINFO_V31

NET_DVR_IPPARACFG    
    NET_DVR_IPPARACFG_V31   
    NET_DVR_IPPARACFG_V40

## 相关链接

- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
- [NET_DVR_IPALARMINFO](../structures/NET_DVR_IPALARMINFO.md)
- [NET_DVR_IPALARMINFO_V31](../structures/NET_DVR_IPALARMINFO_V31.md)
- [NET_DVR_IPPARACFG](../structures/NET_DVR_IPPARACFG.md)
- [NET_DVR_IPPARACFG_V31](../structures/NET_DVR_IPPARACFG_V31.md)
- [NET_DVR_IPPARACFG_V40](../structures/NET_DVR_IPPARACFG_V40.md)
