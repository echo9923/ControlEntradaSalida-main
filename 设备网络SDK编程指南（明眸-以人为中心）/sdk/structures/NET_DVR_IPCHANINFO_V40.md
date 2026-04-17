# NET_DVR_IPCHANINFO_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPCHANINFO_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPCHANINFO_V40.html)

IP通道信息（扩展）结构体。

## 语法

```c
struct{
  BYTE     byEnable;
  BYTE     byRes1;
  WORD     wIPID;
  DWORD    dwChannel;
  BYTE     byTransProtocol;
  BYTE     byTransMode;
  BYTE     byFactoryType;
  BYTE     byRes[241];
}NET_DVR_IPCHANINFO_V40, *LPNET_DVR_IPCHANINFO_V40;
```

## Members

- `byEnable`：IP通道在线状态，是一个只读的属性；0表示HDVR或者NVR设备的数字通道连接对应的IP设备失败，该通道不在线；1表示连接成功，该通道在线
- `byRes1`：保留，置为0
- `wIPID`：IP设备ID
- `dwChannel`：IP设备的通道号，例如设备A（HDVR或者NVR设备）的IP通道01，对应的是设备B（DVS）里的通道04，则byChannel=4，如果前端接的是IPC则byChannel=1。
- `byTransProtocol`：传输协议类型：0- TCP，1- UDP，2- 多播，0xff- auto(自动)
- `byTransMode`：传输码流模式：0- 主码流，1- 子码流
- `byFactoryType`：前端设备厂家类型，通过接口NET_DVR_GetIPCProtoList获取
- `byRes`：保留，置为0

## See Also

NET_DVR_GET_STREAM_UNION

## 相关链接

- [NET_DVR_GetIPCProtoList](../definitions/NET_DVR_GetIPCProtoList.md)
- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
