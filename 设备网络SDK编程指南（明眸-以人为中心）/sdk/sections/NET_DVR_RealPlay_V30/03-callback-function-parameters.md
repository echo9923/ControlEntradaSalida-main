# Callback Function Parameters


- `lRealHandle`：[out] 当前的预览句柄
- `dwDataType`：[out] 数据类型

宏定义

宏定义值

含义

NET_DVR_SYSHEAD

1

系统头数据

NET_DVR_STREAMDATA

2

流数据（包括复合流或音视频分开的视频流数据）

NET_DVR_AUDIOSTREAMDATA

3

音频数据

NET_DVR_PRIVATE_DATA

112

私有数据,包括智能信息
- `pBuffer`：[out] 存放数据的缓冲区指针
- `dwBufSize`：[out] 缓冲区大小
- `pUser`：[out] 用户数据
