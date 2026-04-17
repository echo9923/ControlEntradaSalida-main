# NET_DVR_PU_STREAM_URL

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_PU_STREAM_URL.html](https://open.hikvision.com/hardware/structures/NET_DVR_PU_STREAM_URL.html)

URL取流配置结构体。

## 语法

```c
struct{
  BYTE    byEnable;
  BYTE    strURL[240];
  BYTE    byTransPortocol;
  WORD    wIPID;
  BYTE    byChannel;
  BYTE    byRes[7];
}NET_DVR_PU_STREAM_URL,*LPNET_DVR_PU_STREAM_URL;
```

## Members

- `byEnable`：是否启用：0- 禁用，1- 启用
- `strURL`：取流URL路径
- `byTransPortocol`：传输协议类型：0-TCP，1-UDP
- `wIPID`：设备ID号，wIPID = iDevInfoIndex + iGroupNO*64 +1
- `byChannel`：设备通道号
- `byRes`：保留，置为0

## Remarks

通过流媒体服务器取流的URL格式举例:

{rtsp://ip[:port]/urlExtension}[?username=username][?password=password][?linkmode=linkmode]

URL路径也支持其他自定义路径（需前端IPC支持）。

## See Also

NET_DVR_DEC_STREAM_MODE   NET_DVR_GET_STREAM_UNION   NET_DVR_STREAM_TYPE_UNION

## 相关链接

- [NET_DVR_DEC_STREAM_MODE](../structures/NET_DVR_DEC_STREAM_MODE.md)
- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
- [NET_DVR_STREAM_TYPE_UNION](../structures/NET_DVR_STREAM_TYPE_UNION.md)
