# NET_DVR_HOLIDAY_GROUP_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_HOLIDAY_GROUP_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_HOLIDAY_GROUP_CFG.html)

假日组配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byEnable;
  BYTE     byRes1[3];
  BYTE     byGroupName[HOLIDAY_GROUP_NAME_LEN];
  DWORD    dwHolidayPlanNo[MAX_HOLIDAY_PLAN_NUM];
  BYTE     byRes2[32];
}NET_DVR_HOLIDAY_GROUP_CFG,*LPNET_DVR_HOLIDAY_GROUP_CFG;
```

## Members

- `dwSize`：结构体大小
- `byEnable`：是否使能：0- 否，1- 是
- `byRes1`：保留，置为0
- `byGroupName`：假日组名称
- `dwHolidayPlanNo`：假日计划编号，按值表示，采用紧凑型排列，中间遇到0则后续无效
- `byRes2`：保留，置为0

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
