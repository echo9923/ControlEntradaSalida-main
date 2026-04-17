# NET_DVR_GetLastError

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html](https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html)

返回最后操作的错误码。

## Return Values

返回值为错误码。错误码主要分为网络通讯库、RTSP通讯库、软硬解库、语音对讲库等错误码，详见下表。

## Remarks

RTSP通讯库错误码中410、420、430、440，大多数情况由于网络原因引起。

通过NET_DVR_GetErrorMsg函数还能获取错误号信息。

## See Also

NET_DVR_GetErrorMsg

## 相关链接

- [RTSP通讯库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html#RTSPCODE)
- [软解码库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html#PLAYCODE)
- [转封装库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html#TRANSCODE)
- [语音对讲库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html#VOICECODE)
- [Qos流控库错误码](https://open.hikvision.com/hardware/definitions/NET_DVR_GetLastError.html#QOSCODE)
- [NET_DVR_GetErrorMsg](NET_DVR_GetErrorMsg.md)
