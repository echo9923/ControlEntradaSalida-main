# NET_DVR_GetErrorMsg

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html](https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html)

返回最后操作的错误码信息。

## Parameters

- `pErrorNo`：[out] 错误码数值的指针

## Return Values

返回值为错误码信息的指针。错误码主要分为网络通讯库、RTSP通讯库、软硬解库、语音对讲库等错误码，详见下表。

## Remarks

通过NET_DVR_GetLastError函数可获取错误号值。

## See Also

NET_DVR_GetLastError

## 相关链接

- [RTSP通讯库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html#RTSPCODE)
- [软解码库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html#PLAYCODE)
- [转封装库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html#TRANSCODE)
- [语音对讲库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html#VOICECODE)
- [Qos流控库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetErrorMsg.html#QOSCODE)
- [NET_DVR_GetLastError](NET_DVR_GetLastError.md)
