# NET_DVR_StopVoiceCom

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_StopVoiceCom.html](https://open.hikvision.com/hardware/definitions/NET_DVR_StopVoiceCom.html)

停止语音对讲或者语音转发。

## Parameters

- `lVoiceComHandle`：[in] NET_DVR_StartVoiceCom或NET_DVR_StartVoiceCom_V30、NET_DVR_StartVoiceCom_MR或NET_DVR_StartVoiceCom_MR_V30的返回值

## Return Values

TRUE表示成功,FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_StartVoiceCom   NET_DVR_StartVoiceCom_V30
NET_DVR_StartVoiceCom_MR   NET_DVR_StartVoiceCom_MR_V30

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_StartVoiceCom](NET_DVR_StartVoiceCom.md)
- [NET_DVR_StartVoiceCom_V30](NET_DVR_StartVoiceCom_V30.md)
- [NET_DVR_StartVoiceCom_MR](NET_DVR_StartVoiceCom_MR.md)
- [NET_DVR_StartVoiceCom_MR_V30](NET_DVR_StartVoiceCom_MR_V30.md)
