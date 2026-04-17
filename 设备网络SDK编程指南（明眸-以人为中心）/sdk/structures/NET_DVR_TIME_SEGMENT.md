# NET_DVR_TIME_SEGMENT

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_TIME_SEGMENT.html](https://open.hikvision.com/hardware/structures/NET_DVR_TIME_SEGMENT.html)

时间段参数结构体。

## 语法

```c
struct{
  NET_DVR_SIMPLE_DAYTIME    struBeginTime;
  NET_DVR_SIMPLE_DAYTIME    struEndTime;
}NET_DVR_TIME_SEGMENT,*LPNET_DVR_TIME_SEGMENT;
```

## Members

- `struBeginTime`：开始时间点（时分秒）
- `struEndTime`：开始时间点（时分秒）

## See Also

NET_DVR_SINGLE_PLAN_SEGMENT

## 相关链接

- [NET_DVR_SIMPLE_DAYTIME](../structures/NET_DVR_SIMPLE_DAYTIME.md)
- [NET_DVR_SINGLE_PLAN_SEGMENT](../structures/NET_DVR_SINGLE_PLAN_SEGMENT.md)
