# NET_DVR_StopRealPlay

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_StopRealPlay.html](https://open.hikvision.com/hardware/definitions/NET_DVR_StopRealPlay.html)

停止预览。

## Parameters

- `lRealHandle`：[in] 预览句柄，NET_DVR_RealPlay或者NET_DVR_RealPlay_V30的返回值

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## See Also

NET_DVR_RealPlay_V30

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_RealPlay_V30](NET_DVR_RealPlay_V30.md)
