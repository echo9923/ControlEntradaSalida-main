# NET_DVR_GetSDKBuildVersion

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_GetSDKBuildVersion.html](https://open.hikvision.com/hardware/definitions/NET_DVR_GetSDKBuildVersion.html)

获取SDK的版本号和build信息。

## Return Values

SDK的版本号和build信息。2个高字节表示版本号 ：25~32位表示主版本号，17~24位表示次版本号；2个低字节表示build信息。
如0x03000101：表示版本号为3.0，build 号是0101。
