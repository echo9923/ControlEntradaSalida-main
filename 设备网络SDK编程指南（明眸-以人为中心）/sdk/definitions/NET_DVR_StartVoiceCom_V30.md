# NET_DVR_StartVoiceCom_V30

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_StartVoiceCom_V30.html](https://open.hikvision.com/hardware/definitions/NET_DVR_StartVoiceCom_V30.html)

启动语音对讲(Linux版本暂不支持)。

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwVoiceChan`：[in] 语音通道号。对于设备本身的语音对讲通道，从1开始；对于设备的IP通道，为登录返回的起始对讲通道号(byStartDTalkChan) + IP通道索引 - 1，例如客户端通过NVR跟其IP Channel02所接前端IPC进行对讲，则dwVoiceChan=byStartDTalkChan + 1
- `bNeedCBNoEncData`：[in] 需要回调的语音数据类型：0- 编码后的语音数据，1- 编码前的PCM原始数据
- `cbVoiceDataCallBack`：[in] 音频数据回调函数
- `pUser`：[in] 用户数据指针

## Callback Function

```text
typedef void(CALLBACK *fVoiceDataCallBack)(
  LONG     lVoiceComHandle,
  char     *pRecvDataBuffer,
  DWORD    dwBufSize,
  BYTE     byAudioFlag,
  void     *pUser
);
```

typedef void(CALLBACK *fVoiceDataCallBack)(
  LONG     lVoiceComHandle,
  char     *pRecvDataBuffer,
  DWORD    dwBufSize,
  BYTE     byAudioFlag,
  void     *pUser
);

## Callback Function Parameters

- `lVoiceComHandle`：[out] NET_DVR_StartVoiceCom_V30的返回值
- `pRecvDataBuffer`：[out] 存放音频数据的缓冲区指针
- `dwBufSize`：[out] 音频数据大小
- `byAudioFlag`：[out] 音频数据类型：0－本地采集的数据；1－设备发送过来的语音数据
- `pUser`：[out] 用户数据指针

## Return Values

-1表示失败，其他值作为NET_DVR_StopVoiceCom等函数的句柄参数。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

Windows 7操作系统下，如果不外接音频设备，该接口将返回失败。

在调用开始语音对讲之前可先配置设备的语音对讲音频编码类型，即可先调用参数配置中的NET_DVR_COMPRESSION_AUDIO
结构配置。

当前音频为G722编码时，音频数据的采样频率为16000，16位采样且是单通道的。因此，音频播放格式应如下定义：

const int SAMPLES_PER_SECOND = 16000; 

const int CHANNEL = 1;

const int BITS_PER_SAMPLE = 16;

WAVEFORMATEX m_wavFormatEx; 

m_wavFormatEx.cbSize = sizeof(m_wavFormatEx); 

m_wavFormatEx.nBlockAlign = CHANNEL * BITS_PER_SAMPLE / 8; 

m_wavFormatEx.nChannels = CHANNEL; 

m_wavFormatEx.nSamplesPerSec = SAMPLES_PER_SECOND; 

m_wavFormatEx.wBitsPerSample = BITS_PER_SAMPLE;
 

m_wavFormatEx.nAvgBytesPerSec = SAMPLES_PER_SECOND*m_wavFormatEx.nBlockAlign

当前音频为G711或者G726编码时，音频数据的采样频率为8000，16位采样且是单通道的。因此，音频播放格式应如下定义：

const int SAMPLES_PER_SECOND_G711_MU = 8000;

const int CHANNEL = 1;

const int BITS_PER_SAMPLE = 16;

WAVEFORMATEX m_wavFormatEx;

m_wavFormatEx.cbSize = sizeof(m_wavFormatEx);

m_wavFormatEx.nBlockAlign =	CHANNEL * BITS_PER_SAMPLE / 8;

m_wavFormatEx.nChannels = CHANNEL;

m_wavFormatEx.nSamplesPerSec = SAMPLES_PER_SECOND_G711_MU;

m_wavFormatEx.wBitsPerSample = BITS_PER_SAMPLE;

m_wavFormatEx.nAvgBytesPerSec = SAMPLES_PER_SECOND_G711_MU*
m_wavFormatEx.nBlockAlign;

## See Also

NET_DVR_StopVoiceCom

NET_DVR_Login  NET_DVR_Login_V40

## Reference Interface

该接口扩展源于

NET_DVR_StartVoiceCom

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_COMPRESSION_AUDIO](../structures/NET_DVR_COMPRESSION_AUDIO.md)
- [NET_DVR_StopVoiceCom](NET_DVR_StopVoiceCom.md)
- [NET_DVR_Login](NET_DVR_Login.md)
- [NET_DVR_Login_V40](NET_DVR_Login_V40.md)
- [NET_DVR_StartVoiceCom](NET_DVR_StartVoiceCom.md)
