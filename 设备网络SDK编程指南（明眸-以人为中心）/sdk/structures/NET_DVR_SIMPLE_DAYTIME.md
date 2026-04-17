# NET_DVR_SIMPLE_DAYTIME

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_SIMPLE_DAYTIME.html](https://open.hikvision.com/hardware/structures/NET_DVR_SIMPLE_DAYTIME.html)

时间点信息结构体。

## 语法

```c
struct{
  BYTE    byHour;
  BYTE    byMinute;
  BYTE    bySecond;
  BYTE    byRes;
}NET_DVR_SIMPLE_DAYTIME,*LPNET_DVR_SIMPLE_DAYTIME;
```

## Members

- `byHour`：时
- `byMinute`：分
- `bySecond`：秒
- `byRes`：保留，置为0

## See Also

NET_DVR_TIME_SEGMENT

## 相关链接

- [NET_DVR_TIME_SEGMENT](../structures/NET_DVR_TIME_SEGMENT.md)
