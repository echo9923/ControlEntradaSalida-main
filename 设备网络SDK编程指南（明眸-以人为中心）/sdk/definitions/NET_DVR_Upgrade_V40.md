# NET_DVR_Upgrade_V40

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_Upgrade_V40.html](https://open.hikvision.com/hardware/definitions/NET_DVR_Upgrade_V40.html)

远程升级设备固件。

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwUpgradeType`：[in] 升级类型，具体定义如下：

enum _ENUM_UPGRADE_TYPE{
  ENUM_UPGRADE_DVR           = 0, //普通设备升级
  ENUM_UPGRADE_ADAPTER       = 1, //DVR适配器升级
  ENUM_UPGRADE_VCALIB        = 2, //智能库升级
  ENUM_UPGRADE_OPTICAL       = 3, //光端机升级
  ENUM_UPGRADE_ACS           = 4, //门禁系统升级
  ENUM_UPGRADE_AUXILIARY_DEV = 5  //辅助设备升级
}ENUM_UPGRADE_TYPE
- `sFileName`：[in]  升级的文件路径（包括文件名）。路径长度和操作系统有关，sdk不做限制，windows默认路径长度小于等于256字节（包括文件名在内）。
- `pInbuffer`：[in]  升级条件缓冲区，不同的升级类型对应不同的升级条件，具体如下表所示
- `dwBufferLen`：[in]  缓冲区大小

## Return Values

-1表示失败，其他值作为NET_DVR_GetUpgradeState等函数的参数。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

设备本身固件升级，对应升级类型：ENUM_UPGRADE_DVR；设备的其他组件或者配件升级，选择其他升级类型，比如便携主机的路由器升级即采用辅助设备升级，对应升级类型：ENUM_UPGRADE_AUXILIARY_DEV。

是否支持门禁功能对应AcsAbility能力集的节点；
扩展模块升级是否需要重启为设备软硬件能力集(BasicCapability能力集的节点。

是否支持门禁功能对应AcsAbility能力集的节点；
升级后是否需要重启为设备软硬件能力集(BasicCapability能力集的节点。

## See Also

NET_DVR_CloseUpgradeHandle   
NET_DVR_GetUpgradeState   NET_DVR_GetUpgradeProgress

## Reference Interface

该接口扩展源于

NET_DVR_Upgrade

## 相关链接

- [NET_DVR_AUXILIARY_DEV_UPGRADE_PARAM](../structures/NET_DVR_AUXILIARY_DEV_UPGRADE_PARAM.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [BasicCapability](../XMLs/DEVICE_SOFTHARDWARE_ABILITY.md)
- [NET_DVR_CloseUpgradeHandle](../definitions/NET_DVR_CloseUpgradeHandle.md)
- [NET_DVR_GetUpgradeState](../definitions/NET_DVR_GetUpgradeState.md)
- [NET_DVR_GetUpgradeProgress](../definitions/NET_DVR_GetUpgradeProgress.md)
- [NET_DVR_Upgrade](NET_DVR_Upgrade.md)
