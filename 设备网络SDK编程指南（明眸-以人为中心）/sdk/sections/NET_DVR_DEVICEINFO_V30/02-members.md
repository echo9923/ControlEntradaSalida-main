# Members


- `sSerialNumber`：序列号
- `byAlarmInPortNum`：模拟报警输入个数
- `byAlarmOutPortNum`：模拟报警输出个数
- `byDiskNum`：硬盘个数
- `byDVRType`：设备类型，详见下文列表
- `byChanNum`：设备模拟通道个数，数字（IP）通道最大个数为byIPChanNum + byHighDChanNum*256
- `byStartChan`：模拟通道的起始通道号，从1开始。数字通道的起始通道号见下面参数byStartDChan
- `byAudioChanNum`：设备语音对讲通道数
- `byIPChanNum`：设备最大数字通道个数，低8位，高8位见byHighDChanNum。可以根据IP通道个数来判断是否调用NET_DVR_GetDVRConfig（配置命令NET_DVR_GET_IPPARACFG_V40）获取模拟和数字通道相关参数（NET_DVR_IPPARACFG_V40）。
- `byZeroChanNum`：零通道编码个数
- `byMainProto`：主码流传输协议类型：0- private，1- rtsp，2- 同时支持私有协议和rtsp协议取流（默认采用私有协议取流）
- `bySubProto`：子码流传输协议类型：0- private，1- rtsp，2- 同时支持私有协议和rtsp协议取流（默认采用私有协议取流）
- `bySupport`：能力，位与结果为0表示不支持，1表示支持

   bySupport & 0x1，表示是否支持智能搜索

   bySupport & 0x2，表示是否支持备份

   bySupport & 0x4，表示是否支持压缩参数能力获取

   bySupport & 0x8, 表示是否支持双网卡

   bySupport & 0x10, 表示支持远程SADP

   bySupport & 0x20, 表示支持Raid卡功能

   bySupport & 0x40, 表示支持IPSAN目录查找

   bySupport & 0x80, 表示支持rtp over rtsp
- `bySupport1`：能力集扩充，位与结果为0表示不支持，1表示支持

   bySupport1 & 0x1, 表示是否支持snmp v30

   bySupport1 & 0x2, 表示是否支持区分回放和下载

   bySupport1 & 0x4, 表示是否支持布防优先级

   bySupport1 & 0x8, 表示智能设备是否支持布防时间段扩展

   bySupport1 & 0x10,表示是否支持多磁盘数（超过33个）

   bySupport1 & 0x20,表示是否支持rtsp over http

   bySupport1 & 0x80,表示是否支持车牌新报警信息，且还表示是否支持NET_DVR_IPPARACFG_V40配置
- `bySupport2`：能力集扩充，位与结果为0表示不支持，1表示支持

   bySupport2 & 0x1, 表示解码器是否支持通过URL取流解码

   bySupport2 & 0x2, 表示是否支持FTPV40

   bySupport2 & 0x4, 表示是否支持ANR(断网录像)

   bySupport2 & 0x20, 表示是否支持单独获取设备状态子项

   bySupport2 & 0x40, 表示是否是码流加密设备
- `wDevType`：设备型号，详见下文列表
- `bySupport3`：能力集扩展，位与结果：0- 不支持，1- 支持

   bySupport3 & 0x1, 表示是否支持多码流

   bySupport3 & 0x4, 表示是否支持按组配置，具体包含通道图像参数、报警输入参数、IP报警输入/输出接入参数、用户参数、设备工作状态、JPEG抓图、定时和时间抓图、硬盘盘组管理等

   bySupport3 & 0x20, 表示是否支持通过DDNS域名解析取流
- `byMultiStreamProto`：是否支持多码流，按位表示，位与结果：0-不支持，1-支持

byMultiStreamProto & 0x1, 表示是否支持码流3

byMultiStreamProto & 0x2, 表示是否支持码流4

byMultiStreamProto & 0x40,表示是否支持主码流

byMultiStreamProto & 0x80,表示是否支持子码流
- `byStartDChan`：起始数字通道号，0表示无数字通道，比如DVR或IPC
- `byStartDTalkChan`：起始数字对讲通道号，区别于模拟对讲通道号，0表示无数字对讲通道
- `byHighDChanNum`：数字通道个数，高8位
- `bySupport4`：能力集扩展，按位表示，位与结果：0- 不支持，1- 支持
  
bySupport4 & 0x01, 表示是否所有码流类型同时支持RTSP和私有协议

bySupport4 & 0x10, 表示是否支持域名方式挂载网络硬盘
- `byLanguageType`：支持语种能力，按位表示，位与结果：0- 不支持，1- 支持
  
byLanguageType ==0，表示老设备，不支持该字段

byLanguageType & 0x1，表示是否支持中文

byLanguageType & 0x2，表示是否支持英文
- `byVoiceInChanNum`：音频输入通道数
- `byStartVoiceInChanNo`：音频输入起始通道号，0表示无效
- `byRes3`：保留，置为0
- `byMirrorChanNum`：镜像通道个数，录播主机中用于表示导播通道
- `wStartMirrorChanNo`：起始镜像通道号
- `byRes2`：保留，置为0
