# NET_DVR_STREAM_MODE

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_STREAM_MODE.html](https://open.hikvision.com/hardware/structures/NET_DVR_STREAM_MODE.html)

取流方式配置结构体。

## 语法

```c
struct{
  BYTE                      byGetStreamType;
  BYTE                      byRes[3];
  NET_DVR_GET_STREAM_UNION  uGetStream;
}NET_DVR_STREAM_MODE,*LPNET_DVR_STREAM_MODE;
```

## Members

- `byGetStreamType`：取流方式：

0- 直接从设备取流，对应联合体中结构NET_DVR_IPCHANINFO；

1- 从流媒体取流，对应联合体中结构NET_DVR_IPSERVER_STREAM；

2- 通过IPServer获得IP地址后取流，对应联合体中结构NET_DVR_PU_STREAM_CFG；

3- 通过IPServer找到设备，再通过流媒体取设备的流，对应联合体中结构NET_DVR_DDNS_STREAM_CFG；

4- 通过流媒体由URL去取流，对应联合体中结构NET_DVR_PU_STREAM_URL；

5- 通过hiDDNS域名连接设备然后从设备取流，对应联合体中结构NET_DVR_HKDDNS_STREAM；

6- 直接从设备取流(扩展)，对应联合体中结构NET_DVR_IPCHANINFO_V40
- `byRes`：保留，置为0
- `uGetStream`：不同取流方式联合体

## See Also

NET_DVR_IPPARACFG_V40

## 相关链接

- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
- [NET_DVR_IPPARACFG_V40](../structures/NET_DVR_IPPARACFG_V40.md)
