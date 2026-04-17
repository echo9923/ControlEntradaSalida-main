# NET_DVR_SetVoiceComClientVolume

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_SetVoiceComClientVolume.html](https://open.hikvision.com/hardware/definitions/NET_DVR_SetVoiceComClientVolume.html)

设置语音对讲客户端的音量。

## Parameters

- `lVoiceComHandle`：[in] 
   NET_DVR_StartVoiceCom或NET_DVR_StartVoiceCom_V30的返回值
- `wVolume`：[in] 
   设置音量，取值范围[0,0xffff]

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_StartVoiceCom  NET_DVR_StartVoiceCom_V30

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_StartVoiceCom](NET_DVR_StartVoiceCom.md)
- [NET_DVR_StartVoiceCom_V30](../definitions/NET_DVR_StartVoiceCom_V30.md)
