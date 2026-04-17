# NET_DVR_CloseAlarmChan_V30

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_CloseAlarmChan_V30.html](https://open.hikvision.com/hardware/definitions/NET_DVR_CloseAlarmChan_V30.html)

撤销报警上传通道。

## Parameters

- `lAlarmHandle`：[in] NET_DVR_SetupAlarmChan_V30或者NET_DVR_SetupAlarmChan_V41的返回值

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## See Also

NET_DVR_SetupAlarmChan_V30  
NET_DVR_SetupAlarmChan_V41

## Reference Interface

该接口扩展源于

NET_DVR_CloseAlarmChan

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_SetupAlarmChan_V30](NET_DVR_SetupAlarmChan_V30.md)
- [NET_DVR_SetupAlarmChan_V41](NET_DVR_SetupAlarmChan_V41.md)
- [NET_DVR_CloseAlarmChan](NET_DVR_CloseAlarmChan.md)
