# Remarks


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
