# NET_DVR_EVENT_LINKAGE_INFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_EVENT_LINKAGE_INFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_EVENT_LINKAGE_INFO.html)

事件联动参数结构体。

## 语法

```c
struct{
  WORD    wMainEventType;
  WORD    wSubEventType;
  BYTE    byRes[28];
}NET_DVR_EVENT_LINKAGE_INFO,*LPNET_DVR_EVENT_LINKAGE_INFO;
```

## Members

- `wMainEventType`：事件主类型：0- 设备事件，1- 报警输入事件，2- 门事件，3- 读卡器事件
- `wSubEventType`：事件次类型，不同的主类型对应不同的次类型，详见“Remarks”说明
- `byRes`：保留，置为0

## Remarks

不同的事件主类型对应不同的事件次类型，具体定义如下所示：

## See Also

NET_DVR_EVETN_CARD_LINKAGE_UNION

## 相关链接

- [NET_DVR_EVETN_CARD_LINKAGE_UNION](../structures/NET_DVR_EVETN_CARD_LINKAGE_UNION.md)
