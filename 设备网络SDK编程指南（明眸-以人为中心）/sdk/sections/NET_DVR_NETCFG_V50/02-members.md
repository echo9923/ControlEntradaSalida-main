# Members


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
- `wAlarmHost2IpPort`：报警主机2端口号
- `struAlarmHost2IpAddr`：报警主机2 IP地址
- `byEnableDNS`：手动设置DNS服务器地址使能：0- 自动获取，1- 手动设置
- `byRes`：保留，置为0
