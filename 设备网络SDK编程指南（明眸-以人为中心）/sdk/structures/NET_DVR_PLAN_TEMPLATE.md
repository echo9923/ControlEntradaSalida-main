# NET_DVR_PLAN_TEMPLATE

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_PLAN_TEMPLATE.html](https://open.hikvision.com/hardware/structures/NET_DVR_PLAN_TEMPLATE.html)

计划模板配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byEnable;
  BYTE     byRes1[3];
  BYTE     byTemplateName[TEMPLATE_NAME_LEN];
  DWORD    dwWeekPlanNo;
  DWORD    dwHolidayGroupNo[MAX_HOLIDAY_GROUP_NUM];
  BYTE     byRes2[32];
}NET_DVR_PLAN_TEMPLATE,*LPNET_DVR_PLAN_TEMPLATE;
```

## Members

- `dwSize`：结构体大小
- `byEnable`：是否使能：0- 否，1- 是
- `byRes1`：保留，置为0
- `byGroupName`：计划模板名称
- `dwWeekPlanNo`：周计划编号，0表示无效
- `dwHolidayGroupNo`：假日组编号，按值表示，采用紧凑型排列，中间遇到0则后续无效
- `byRes2`：保留，置为0

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
