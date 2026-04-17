# NET_DVR_CARD_USER_INFO_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CARD_USER_INFO_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_CARD_USER_INFO_CFG.html)

卡号关联用户信息配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     sUsername[NAME_LEN];
  BYTE     byRes2[256];
}NET_DVR_CARD_USER_INFO_CFG,*LPNET_DVR_CARD_USER_INFO_CFG;
```

## Members

- `dwSize`：结构体大小
- `sUsername`：用户名
- `byRes2`：保留，置为0

## Remarks

设备是否支持卡号关联用户配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_GetDeviceConfig   NET_DVR_SetDeviceConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_GetDeviceConfig](../definitions/NET_DVR_GetDeviceConfig_ACS.md)
- [NET_DVR_SetDeviceConfig](../definitions/NET_DVR_SetDeviceConfig_ACS.md)
