# NET_DVR_EVETN_CARD_LINKAGE_UNION

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_EVETN_CARD_LINKAGE_UNION.html](https://open.hikvision.com/hardware/structures/NET_DVR_EVETN_CARD_LINKAGE_UNION.html)

事件/卡号联动方式联合体。

## 语法

```c
union{
  BYTE                          byCardNo[ACS_CARD_NO_LEN];
  NET_DVR_EVENT_LINKAGE_INFO    struEventLinkage;
  BYTE                          byMACAddr[MACADDR_LEN];
}NET_DVR_EVETN_CARD_LINKAGE_UNION,*LPNET_DVR_EVETN_CARD_LINKAGE_UNION;
```

## Members

- `byCardNo`：卡号，byProMode为1时有效
- `struEventLinkage`：事件联动参数，byProMode为0时有效
- `byMACAddr`：物理MAC地址

## See Also

NET_DVR_EVENT_CARD_LINKAGE_CFG

## 相关链接

- [NET_DVR_EVENT_LINKAGE_INFO](../structures/NET_DVR_EVENT_LINKAGE_INFO.md)
- [NET_DVR_EVENT_CARD_LINKAGE_CFG](../structures/NET_DVR_EVENT_CARD_LINKAGE_CFG.md)
