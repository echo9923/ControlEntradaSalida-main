# NET_DVR_CloseUpgradeHandle

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_CloseUpgradeHandle.html](https://open.hikvision.com/hardware/definitions/NET_DVR_CloseUpgradeHandle.html)

关闭远程升级句柄，释放资源。

## Parameters

- `lUpgradeHandle`：[in] NET_DVR_Upgrade_V40或NET_DVR_Upgrade的返回值

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## See Also

NET_DVR_Upgrade_V40   NET_DVR_Upgrade

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Upgrade_V40](NET_DVR_Upgrade_V40.md)
- [NET_DVR_Upgrade](NET_DVR_Upgrade.md)
