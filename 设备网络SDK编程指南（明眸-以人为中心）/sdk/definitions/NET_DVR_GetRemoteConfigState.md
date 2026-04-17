# NET_DVR_GetRemoteConfigState

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_GetRemoteConfigState.html](https://open.hikvision.com/hardware/definitions/NET_DVR_GetRemoteConfigState.html)

获取长连接配置的状态。

## Parameters

- `lHandle`：[in] 句柄，NET_DVR_StartRemoteConfig的返回值
- `pState`：[out] 返回的状态值，不同的配置命令对应不同的状态取值，具体见下表

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_StartRemoteConfig](NET_DVR_StartRemoteConfig.md)
