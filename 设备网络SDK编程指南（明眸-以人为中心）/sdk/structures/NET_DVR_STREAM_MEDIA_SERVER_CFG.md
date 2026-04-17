# NET_DVR_STREAM_MEDIA_SERVER_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_STREAM_MEDIA_SERVER_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_STREAM_MEDIA_SERVER_CFG.html)

流媒体服务器参数结构体。

## 语法

```c
struct{
  BYTE             byValid;
  BYTE             byRes1[3];
  NET_DVR_IPADDR   struDevIP;
  WORD             wDevPort;
  BYTE             byTransmitType;
  BYTE             byRes2[69];
}NET_DVR_STREAM_MEDIA_SERVER_CFG,*LPNET_DVR_STREAM_MEDIA_SERVER_CFG;
```

## Members

- `byValid`：是否启用流媒体服务器取流：0-不启用，非0-启用
- `byRes1`：保留，置为0
- `struDevIP`：流媒体服务器的IP地址
- `wDevPort`：流媒体服务器端口
- `byTransmitType`：传输协议类型：0-TCP，1-UDP
- `byRes2`：保留，置为0

## See Also

NET_DVR_MATRIX_CHAN_INFO_V30    NET_DVR_MATRIX_DEC_CHAN_INFO_V30    NET_DVR_PU_STREAM_CFG

## 相关链接

- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_MATRIX_CHAN_INFO_V30](../structures/NET_DVR_MATRIX_CHAN_INFO_V30.md)
- [NET_DVR_MATRIX_DEC_CHAN_INFO_V30](../structures/NET_DVR_MATRIX_DEC_CHAN_INFO_V30.md)
- [NET_DVR_PU_STREAM_CFG](../structures/NET_DVR_PU_STREAM_CFG.md)
