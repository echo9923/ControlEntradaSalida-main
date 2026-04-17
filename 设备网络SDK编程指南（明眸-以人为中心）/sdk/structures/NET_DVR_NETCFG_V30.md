# NET_DVR_NETCFG_V30

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_NETCFG_V30.html](https://open.hikvision.com/hardware/structures/NET_DVR_NETCFG_V30.html)

网络配置结构体。

## 语法

```c
struct{
  DWORD                    dwSize;
  NET_DVR_ETHERNET_V30     struEtherNet[MAX_ETHERNET];
  NET_DVR_IPADDR           struRes1[2];
  NET_DVR_IPADDR           struAlarmHostIpAddr;
  BYTE                     byRes2[4];
  WORD                     wAlarmHostIpPort;
  BYTE                     byUseDhcp;
  BYTE                     byIPv6Mode;
  NET_DVR_IPADDR           struDnsServer1IpAddr;
  NET_DVR_IPADDR           struDnsServer2IpAddr;
  BYTE                     byIpResolver[MAX_DOMAIN_NAME];
  WORD                     wIpResolverPort;
  WORD                     wHttpPortNo;
  NET_DVR_IPADDR           struMulticastIpAddr;
  NET_DVR_IPADDR           struGatewayIpAddr;
  NET_DVR_PPPOECFG         struPPPoE;
  BYTE                     byEnablePrivateMulticastDiscovery;
  BYTE                     byEnableOnvifMulticastDiscovery;
  BYTE                     byEnableDNS;
  BYTE                     byRes[61];
}NET_DVR_NETCFG_V30,*LPNET_DVR_NETCFG_V30;
```

## Members

- `dwSize`：结构体大小
- `struEtherNet`：以太网口
- `struRes1`：保留，置为0
- `struAlarmHostIpAddr`：报警主机IP地址
- `byRes2`：保留，置为0
- `wAlarmHostIpPort`：报警主机端口号
- `byUseDhcp`：是否启用DHCP：0xff-无效；0-不启用；1-启用
- `byIPv6Mode`：IPv6分配方式：0-路由公告，1-手动设置，2-启用DHCP分配
- `struDnsServer1IpAddr`：域名服务器1的IP地址
- `struDnsServer2IpAddr`：域名服务器2的IP地址
- `byIpResolver`：IP解析服务器域名或IP地址（8000设备不支持域名）
- `wIpResolverPort`：IP解析服务器端口号
- `wHttpPortNo`：HTTP端口号
- `struMulticastIpAddr`：多播组地址
- `struGatewayIpAddr`：网关地址
- `struPPPoE`：PPPoE参数
- `byEnablePrivateMulticastDiscovery`：私有多播搜索(SADP)：0- 默认，1- 启用，2- 禁用
- `byEnableOnvifMulticastDiscovery`：Onvif多播搜索(SADP)：0- 默认，1- 启用，2- 禁用
- `byEnableDNS`：手动设置DNS服务器地址使能：0- 自动获取，1- 手动设置
- `byRes`：保留，置为0

## Remarks

8000等3.0协议以下的设备，参数byUseDhcp为0xff-无效，将其IP地址填成空，设备会自动去获取DHCP。

当byIPv6Mode选择“0-路由公告”或“2-启用DHCP分配”时，无需设置struEtherNet中的IPv6地址，设备自动获取；当byIPv6Mode选择“1-手动设置”时，需要设置struEtherNet中的IPv6地址。由于多IPv6地址（路由公告地址多个，可以正常使用），当前登录的设备IPv6地址可能和此结构中的struEtherNet中的IPv6地址不一致。

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## Reference Structure

该结构扩展源于

NET_DVR_NETCFG

扩展结构体可见

NET_DVR_NETCFG_V50

## 相关链接

- [NET_DVR_ETHERNET_V30](../structures/NET_DVR_ETHERNET_V30.md)
- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_PPPOECFG](../structures/NET_DVR_PPPOECFG.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_old.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_old.md)
- [NET_DVR_NETCFG](../structures/NET_DVR_NETCFG.md)
- [NET_DVR_NETCFG_V50](../structures/NET_DVR_NETCFG_V50.md)
