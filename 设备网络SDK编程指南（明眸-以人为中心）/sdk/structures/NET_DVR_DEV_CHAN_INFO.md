# NET_DVR_DEV_CHAN_INFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_DEV_CHAN_INFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_DEV_CHAN_INFO.html)

前端设备信息结构体。

## 语法

```c
struct{
  NET_DVR_IPADDR   struIP;
  WORD             wDVRPort;
  BYTE             byChannel;
  BYTE             byTransProtocol;
  BYTE             byTransMode;
  BYTE             byFactoryType;
  BYTE             byDeviceType;
  BYTE             byDispChan;
  BYTE             bySubDispChan;
  BYTE             byResolution;
  BYTE             byRes[2];
  BYTE             byDomain[MAX_DOMAIN_NAME];
  BYTE             sUserName[NAME_LEN];
  BYTE             sPassword[PASSWD_LEN];
}NET_DVR_DEV_CHAN_INFO,*LPNET_DVR_DEV_CHAN_INFO;
```

## Members

- `struIP`：设备IP地址
- `wDVRPort`：设备端口号
- `byChannel`：通道号,目前设备的模拟通道号是从1开始的，对于9000等设备的IPC接入，数字通道号从33开始
- `byTransProtocol`：传输协议类型：0-TCP，1-UDP，2-多播方式，3-RTP
- `byTransMode`：传输码流模式：0－主码流，1－子码流
- `byFactoryType`：前端设备厂家类型， 通过接口NET_DVR_GetIPCProtoList获取
- `byDeviceType`：设备类型(视频综合平台使用)：1- IPC，2- ENCODER
- `byDispChan`：显示通道号（智能配置使用），根据能力集决定使用解码通道还是显示通道
- `bySubDispChan`：显示通道子通道号（智能配置时使用）
- `byResolution`：分辨率：1- CIF，2- 4CIF，3- 720P，4- 1080P，5- 500W
- `byRes`：保留，置为0
- `byDomain`：设备域名
- `sUserName`：设备登陆帐号
- `sPassword`：设备密码

## See Also

NET_DVR_MATRIX_CHAN_INFO_V30    NET_DVR_MATRIX_DEC_CHAN_INFO_V30

NET_DVR_PU_STREAM_CFG

NET_DVR_GetIPCProtoList

## 相关链接

- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_MATRIX_CHAN_INFO_V30](../structures/NET_DVR_MATRIX_CHAN_INFO_V30.md)
- [NET_DVR_MATRIX_DEC_CHAN_INFO_V30](../structures/NET_DVR_MATRIX_DEC_CHAN_INFO_V30.md)
- [NET_DVR_PU_STREAM_CFG](../structures/NET_DVR_PU_STREAM_CFG.md)
- [NET_DVR_GetIPCProtoList](../definitions/NET_DVR_GetIPCProtoList.md)
