# NET_DVR_TIME_EX

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_TIME_EX.html](https://open.hikvision.com/hardware/structures/NET_DVR_TIME_EX.html)

时间参数结构体。

## 语法

```c
struct{
  WORD    wYear;
  BYTE    byMonth;
  BYTE    byDay;
  BYTE    byHour;
  BYTE    byMinute;
  BYTE    bySecond;
  BYTE    byRes;
}NET_DVR_TIME_EX, *LPNET_DVR_TIME_EX;
```

## Members

- `wYear`：年
- `byMonth`：月
- `byDay`：日
- `byHour`：时
- `byMinute`：分
- `bySecond`：秒
- `byRes`：保留
