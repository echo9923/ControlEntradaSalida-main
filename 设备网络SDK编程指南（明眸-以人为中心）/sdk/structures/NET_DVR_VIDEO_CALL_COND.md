# NET_DVR_VIDEO_CALL_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_VIDEO_CALL_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_VIDEO_CALL_COND.html)

可视对讲信令处理条件参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byRes[128];
}NET_DVR_VIDEO_CALL_COND, *LPNET_DVR_VIDEO_CALL_COND;
```

## Members

- `dwSize`：结构体大小
- `byRes`：保留，置为0

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_intercom_call.md)
