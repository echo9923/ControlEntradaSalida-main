# NET_DVR_PU_STREAM_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_PU_STREAM_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_PU_STREAM_CFG.html)

动态解码参数结构体。

## 语法

```c
struct{
  DWORD                             dwSize;
  NET_DVR_STREAM_MEDIA_SERVER_CFG   struStreamMediaSvrCfg;
  NET_DVR_DEV_CHAN_INFO             struDevChanInfo;
}NET_DVR_PU_STREAM_CFG,*LPNET_DVR_PU_STREAM_CFG;
```

## Members

- `dwSize`：结构体大小
- `struStreamMediaSvrCfg`：流媒体服务器配置参数
- `struDevChanInfo`：设备通道配置参数

## See Also

NET_DVR_GET_STREAM_UNION   NET_DVR_INPUTSTREAMCFG

NET_IVMS_DEVSCHED    NET_DVR_GetDVRConfig    NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_STREAM_MEDIA_SERVER_CFG](../structures/NET_DVR_STREAM_MEDIA_SERVER_CFG.md)
- [NET_DVR_DEV_CHAN_INFO](../structures/NET_DVR_DEV_CHAN_INFO.md)
- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
- [NET_DVR_INPUTSTREAMCFG](../structures/NET_DVR_INPUTSTREAMCFG.md)
- [NET_IVMS_DEVSCHED](../structures/NET_IVMS_DEVSCHED.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_VCA.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_VCA.md)
