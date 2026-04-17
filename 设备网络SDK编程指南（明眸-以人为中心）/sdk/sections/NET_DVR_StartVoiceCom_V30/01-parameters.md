# Parameters


- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwVoiceChan`：[in] 语音通道号。对于设备本身的语音对讲通道，从1开始；对于设备的IP通道，为登录返回的起始对讲通道号(byStartDTalkChan) + IP通道索引 - 1，例如客户端通过NVR跟其IP Channel02所接前端IPC进行对讲，则dwVoiceChan=byStartDTalkChan + 1
- `bNeedCBNoEncData`：[in] 需要回调的语音数据类型：0- 编码后的语音数据，1- 编码前的PCM原始数据
- `cbVoiceDataCallBack`：[in] 音频数据回调函数
- `pUser`：[in] 用户数据指针
