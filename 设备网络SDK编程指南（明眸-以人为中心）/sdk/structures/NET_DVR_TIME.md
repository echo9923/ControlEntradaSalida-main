# NET_DVR_TIME

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_TIME.html](https://open.hikvision.com/hardware/structures/NET_DVR_TIME.html)

时间参数结构体。

## 语法

```c
struct{
  DWORD    dwYear;
  DWORD    dwMonth;
  DWORD    dwDay;
  DWORD    dwHour;
  DWORD    dwMinute;
  DWORD    dwSecond;
}NET_DVR_TIME, *LPNET_DVR_TIME;
```

## Members

- `dwYear`：年
- `dwMonth`：月
- `dwDay`：日
- `dwHour`：时
- `dwMinute`：分
- `dwSecond`：秒
