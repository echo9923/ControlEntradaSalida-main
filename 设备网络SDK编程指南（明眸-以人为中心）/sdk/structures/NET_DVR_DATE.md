# NET_DVR_DATE

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_DATE.html](https://open.hikvision.com/hardware/structures/NET_DVR_DATE.html)

日期信息结构体。

## 语法

```c
struct{
  WORD    wYear;
  BYTE    byMonth;
  BYTE    byDay;
}NET_DVR_DATE,*LPNET_DVR_DATE;
```

## Members

- `wYear`：年
- `byMonth`：月
- `byDay`：日

## See Also

NET_DVR_HOLIDAY_PLAN_CFG   NET_DVR_ID_CARD_INFO

NET_DVR_GetDeviceConfig   NET_DVR_GetDeviceConfig

## 相关链接

- [NET_DVR_HOLIDAY_PLAN_CFG](../structures/NET_DVR_HOLIDAY_PLAN_CFG.md)
- [NET_DVR_ID_CARD_INFO](../structures/NET_DVR_ID_CARD_INFO.md)
- [NET_DVR_GetDeviceConfig](../definitions/NET_DVR_GetDeviceConfig_RECORD.md)
