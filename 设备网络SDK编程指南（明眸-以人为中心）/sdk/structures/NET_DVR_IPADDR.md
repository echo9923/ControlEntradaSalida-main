# NET_DVR_IPADDR

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPADDR.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPADDR.html)

IP地址结构体。

## 语法

```c
struct{
  char    sIpV4[16];
  BYTE    sIpV6[128];
}NET_DVR_IPADDR, *LPNET_DVR_IPADDR;
```

## Members

- `sIpV4`：设备IPv4地址
- `sIpV6`：设备IPv6地址
