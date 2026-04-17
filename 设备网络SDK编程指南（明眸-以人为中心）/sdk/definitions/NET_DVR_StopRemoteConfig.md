# NET_DVR_StopRemoteConfig

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_StopRemoteConfig.html](https://open.hikvision.com/hardware/definitions/NET_DVR_StopRemoteConfig.html)

关闭长连接配置接口所创建的句柄，释放资源。

## Parameters

- `lHandle`：[in] 句柄，NET_DVR_StartRemoteConfig的返回值

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_StartRemoteConfig](NET_DVR_StartRemoteConfig.md)
