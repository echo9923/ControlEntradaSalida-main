# Remarks


dwStreamType(码流类型)、dwLinkMode(连接方式)、bPassbackRecord(录像回传)、byPreviewMode(延迟预览模式)、byStreamID(流ID)这些参数的取值需要设备支持。

NET_DVR_RealPlay_V40支持多播方式预览（dwLinkMode设为2），不需要传多播组地址，底层自动从设备获取已配置的多播组地址（NET_DVR_NETCFG_V50中的参数struMulticastIpAddr）并以该多播组地址实现多播。

设备码流类型详细介绍可以参考“帮助”->“常见问题解答”中的Question 33。
