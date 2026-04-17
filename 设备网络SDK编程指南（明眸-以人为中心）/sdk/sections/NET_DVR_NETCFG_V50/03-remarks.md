# Remarks


8000等3.0协议以下的设备，参数byUseDhcp为0xff-无效，将其IP地址填成空，设备会自动去获取DHCP。

当byIPv6Mode选择“0-路由公告”或“2-启用DHCP分配”时，无需设置struEtherNet中的IPv6地址，设备自动获取；当byIPv6Mode选择“1-手动设置”时，需要设置struEtherNet中的IPv6地址。由于多IPv6地址（路由公告地址多个，可以正常使用），当前登录的设备IPv6地址可能和此结构中的struEtherNet中的IPv6地址不一致。
