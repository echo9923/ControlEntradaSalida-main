# Remarks


设备登录模式有两种：SDK私有协议和ISAPI协议。

1) SDK私有协议是我司私有的TCP/IP协议，登录使用的是设备服务端口（默认为8000），我司网络设备除特殊产品外基本都支持该协议方式登录，因此一般建议使用SDK私有协议模式登录。

2) ISAPI协议是基于标准HTTP REST架构，HTTP协议或者HTTPS协议访问设备，登录使用的是设备HTTP端口（默认为80）或者HTTPS端口（默认为443）。不支持SDK私有协议的设备如猎鹰、刀锋等采用ISAPI协议的方式登录。

使用ISAPI协议方式登录时byUseTransport、cbLoginResult、pUser、bUseAsynLogin、byProxyType、byUseUTCTime、iProxyID这些参数都不支持。
