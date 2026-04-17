# NET_DVR_ETHERNET_V30

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ETHERNET_V30.html](https://open.hikvision.com/hardware/structures/NET_DVR_ETHERNET_V30.html)

以太网配置结构体。

## 语法

```c
struct{
  NET_DVR_IPADDR    struDVRIP;
  NET_DVR_IPADDR    struDVRIPMask;
  DWORD             dwNetInterface;
  WORD              wDVRPort;
  WORD              wMTU;
  BYTE              byMACAddr[MACADDR_LEN];
  BYTE              byEthernetPortNo; 
  BYTE              byRes[1];
}NET_DVR_ETHERNET_V30, *LPNET_DVR_ETHERNET_V30;
```

## Members

- `struDVRIP`：设备IP地址
- `struDVRIPMask`：设备IP地址掩码
- `dwNetInterface`：网络接口：1-10MBase-T；2-10MBase-T全双工；3-100MBase-TX；4-100M全双工；5-10M/100M/1000M自适应；6-1000M全双工
- `wDVRPort`：设备端口号
- `wMTU`：MTU设置，默认1500
- `byMACAddr`：设备物理地址
- `byEthernetPortNo`：网口号，0-无效，1-网口0，2-网口1以此类推，只读
- `byRes`：保留

## Remarks

MTU的设置范围为500-9676，若MTU设置过小客户端将无法注册到设备，并且客户端预览、回放、配置参数也会失败。

## See Also

NET_DVR_NETCFG_V30    NET_DVR_ONE_BONDING

## 相关链接

- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_NETCFG_V30](../structures/NET_DVR_NETCFG_V30.md)
- [NET_DVR_ONE_BONDING](../structures/NET_DVR_ONE_BONDING.md)
