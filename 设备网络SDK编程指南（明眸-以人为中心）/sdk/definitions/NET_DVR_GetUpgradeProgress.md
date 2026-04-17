# NET_DVR_GetUpgradeProgress

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_GetUpgradeProgress.html](https://open.hikvision.com/hardware/definitions/NET_DVR_GetUpgradeProgress.html)

获取远程升级的进度。

## Parameters

- `lUpgradeHandle`：[in] NET_DVR_Upgrade_V50、NET_DVR_Upgrade_V40或NET_DVR_Upgrade的返回值

## Return Values

-1表示失败，0～100表示升级进度。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_Upgrade_V40   NET_DVR_Upgrade

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Upgrade_V40](NET_DVR_Upgrade_V40.md)
- [NET_DVR_Upgrade](NET_DVR_Upgrade.md)
