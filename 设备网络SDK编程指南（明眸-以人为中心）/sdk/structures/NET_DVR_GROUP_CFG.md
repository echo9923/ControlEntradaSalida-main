# NET_DVR_GROUP_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_GROUP_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_GROUP_CFG.html)

群组参数配置结构体。

## 语法

```c
struct{
  DWORD                       dwSize;
  BYTE                        byEnable;
  BYTE                        byRes1[3];
  NET_DVR_VALID_PERIOD_CFG    struValidPeriodCfg;
  BYTE                        byGroupName[GROUP_NAME_LEN];
  BYTE                        byRes2[32];
}NET_DVR_GROUP_CFG,*LPNET_DVR_GROUP_CFG;
```

## Members

- `dwSize`：结构体大小
- `byEnable`：是否启用该群组：0- 不启用，1- 启用
- `byRes1`：保留，置为0
- `struValidPeriodCfg`：群组有效期参数
- `byGroupName`：群组名称
- `byRes2`：保留，置为0

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_VALID_PERIOD_CFG](../structures/NET_DVR_VALID_PERIOD_CFG.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
