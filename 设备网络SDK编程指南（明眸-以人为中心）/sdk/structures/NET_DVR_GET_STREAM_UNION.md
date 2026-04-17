# NET_DVR_GET_STREAM_UNION

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_GET_STREAM_UNION.html](https://open.hikvision.com/hardware/structures/NET_DVR_GET_STREAM_UNION.html)

取流方式联合体。

## 语法

```c
union{
  NET_DVR_IPCHANINFO         struChanInfo;
  NET_DVR_PU_STREAM_CFG      struPUStream;
  NET_DVR_IPSERVER_STREAM    struIPServerStream;
  NET_DVR_DDNS_STREAM_CFG    struDDNSStream;
  NET_DVR_PU_STREAM_URL      struStreamUrl;
  NET_DVR_HKDDNS_STREAM      struHkDDNSStream;
  NET_DVR_IPCHANINFO_V40     struIPChan;
}NET_DVR_GET_STREAM_UNION,*LPNET_DVR_GET_STREAM_UNION;
```

## Members

- `struChanInfo`：直接从设备取流的IP通道信息
- `struPUStream`：通过流媒体从设备取流
- `struIPServerStream`：通过IPServer获得IP地址后取流
- `struDDNSStream`：通过IPServer找到设备，再通过流媒体取设备的码流
- `struStreamUrl`：通过URL从流媒体取流
- `struHkDDNSStream`：通过hiDDNS连接设备然后从设备取流
- `struIPChan`：直接从设备取流（扩展）

## Remarks

NVR、混合型DVR设备主要支持直接从设备取流(struChanInfo)的模式；PCNVR（存储服务器）支持struChanInfo、struPUStream、struIPServerStream及struDDNSStream 4种取流模式。

## See Also

NET_DVR_STREAM_MODE

## 相关链接

- [NET_DVR_IPCHANINFO](../structures/NET_DVR_IPCHANINFO.md)
- [NET_DVR_PU_STREAM_CFG](../structures/NET_DVR_PU_STREAM_CFG.md)
- [NET_DVR_IPSERVER_STREAM](../structures/NET_DVR_IPSERVER_STREAM.md)
- [NET_DVR_DDNS_STREAM_CFG](../structures/NET_DVR_DDNS_STREAM_CFG.md)
- [NET_DVR_PU_STREAM_URL](../structures/NET_DVR_PU_STREAM_URL.md)
- [NET_DVR_HKDDNS_STREAM](../structures/NET_DVR_HKDDNS_STREAM.md)
- [NET_DVR_IPCHANINFO_V40](../structures/NET_DVR_IPCHANINFO_V40.md)
- [NET_DVR_STREAM_MODE](../structures/NET_DVR_STREAM_MODE.md)
