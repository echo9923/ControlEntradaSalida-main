# NET_DVR_HOLIDAY_PLAN_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_HOLIDAY_PLAN_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_HOLIDAY_PLAN_CFG.html)

假日计划配置结构体。

## 语法

```c
struct{
  DWORD                          dwSize;
  BYTE                           byEnable;
  BYTE                           byRes1[3];
  NET_DVR_DATE                   struBeginDate;
  NET_DVR_DATE                   struEndDate;
  NET_DVR_SINGLE_PLAN_SEGMENT    struPlanCfg[MAX_DAYS][MAX_TIMESEGMENT_V30];
  BYTE                           byRes2[16];
}NET_DVR_HOLIDAY_PLAN_CFG,*LPNET_DVR_HOLIDAY_PLAN_CFG;
```

## Members

- `dwSize`：结构体大小
- `byEnable`：是否使能：0- 否，1- 是
- `byRes1`：保留，置为0
- `struBeginDate`：假日开始日期
- `struEndDate`：假日结束日期
- `struPlanCfg`：假日计划参数，一周7天，每天最多8个时间段
- `byRes2`：保留，置为0

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_DATE](../structures/NET_DVR_DATE.md)
- [NET_DVR_SINGLE_PLAN_SEGMENT](../structures/NET_DVR_SINGLE_PLAN_SEGMENT.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
