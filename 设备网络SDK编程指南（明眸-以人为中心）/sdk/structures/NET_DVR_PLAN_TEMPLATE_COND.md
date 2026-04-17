# NET_DVR_PLAN_TEMPLATE_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_PLAN_TEMPLATE_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_PLAN_TEMPLATE_COND.html)

卡权限计划模板配置条件结构体。

## 语法

```c
struct{
  DWORD   dwSize;
  DWORD   dwPlanTemplateNumber;
  WORD    wLocalControllerID;
  BYTE    byRes[106];
}NET_DVR_PLAN_TEMPLATE_COND,*LPNET_DVR_PLAN_TEMPLATE_COND;
```

## Members

- `dwSize`：结构体大小
- `dwPlanTemplateNumber`：计划模板编号，从1开始，最大值从门禁能力集获取
- `wLocalControllerID`：就地控制器序号[1,64]，0表示门禁主机
- `byRes`：保留，置为0

## See Also

NET_DVR_GetDeviceConfig   NET_DVR_SetDeviceConfig

## 相关链接

- [NET_DVR_GetDeviceConfig](../definitions/NET_DVR_GetDeviceConfig_ACS.md)
- [NET_DVR_SetDeviceConfig](../definitions/NET_DVR_SetDeviceConfig_ACS.md)
