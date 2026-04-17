# NET_DVR_Init

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_Init.html](https://open.hikvision.com/hardware/definitions/NET_DVR_Init.html)

初始化SDK，调用其他SDK函数的前提。

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## Remarks

SDK初始化之前可以调用NET_DVR_SetSDKInitCfg设置SDK支持的登录布防连接个数、设置组件库加载路径（仅Linux版本支持）等初始化参数。

## See Also

NET_DVR_Cleanup

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_SetSDKInitCfg](../definitions/NET_DVR_SetSDKInitCfg.md)
- [NET_DVR_Cleanup](NET_DVR_Cleanup.md)
