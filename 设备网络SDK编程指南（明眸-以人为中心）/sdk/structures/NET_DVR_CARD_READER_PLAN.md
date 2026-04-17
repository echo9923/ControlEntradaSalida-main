# NET_DVR_CARD_READER_PLAN

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CARD_READER_PLAN.html](https://open.hikvision.com/hardware/structures/NET_DVR_CARD_READER_PLAN.html)

读卡器验证计划配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  DWORD    dwTemplateNo;
  BYTE     byRes[64];
}NET_DVR_CARD_READER_PLAN,*LPNET_DVR_CARD_READER_PLAN;
```

## Members

- `dwSize`：结构体大小
- `dwTemplateNo`：计划模板编号，为0表示取消关联、恢复默认状态（刷卡开门）。非0回复关联（读卡器序号，与相同序号的读卡器认证方式计划模板关联）
- `byRes`：保留，置为0

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
