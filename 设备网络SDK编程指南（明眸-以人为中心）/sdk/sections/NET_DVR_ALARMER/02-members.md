# Members


- `byUserIDValid`：userid是否有效：0－无效；1－有效
- `bySerialValid`：序列号是否有效：0－无效；1－有效
- `byVersionValid`：版本号是否有效：0－无效；1－有效
- `byDeviceNameValid`：设备名字是否有效：0－无效；1－有效
- `byMacAddrValid`：MAC地址是否有效：0－无效；1－有效
- `byLinkPortValid`：Login端口是否有效：0－无效；1－有效
- `byDeviceIPValid`：设备IP是否有效：0－无效；1－有效
- `bySocketIPValid`：Socket IP是否有效：0-无效；1-有效
- `lUserID`：NET_DVR_Login或NET_DVR_Login_V30返回值, 布防时有效
- `sSerialNumber`：序列号
- `dwDeviceVersion`：版本信息：V3.0以上版本支持的设备最高8位为主版本号，次高8位为次版本号，低16位为修复版本号；V3.0以下版本支持的设备高16位表示主版本，低16位表示次版本
- `sDeviceName`：设备名称
- `byMacAddr`：MAC地址
- `wLinkPort`：设备通讯端口
- `sDeviceIP`：设备IP地址
- `sSocketIP`：报警主动上传时的Socket IP地址
- `byIpProtocol`：IP协议：0－IPV4；1－IPV6
- `byRes2`：保留，置为0
