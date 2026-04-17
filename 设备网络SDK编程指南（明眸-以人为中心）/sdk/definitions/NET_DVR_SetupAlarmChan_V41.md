# NET_DVR_SetupAlarmChan_V41

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_SetupAlarmChan_V41.html](https://open.hikvision.com/hardware/definitions/NET_DVR_SetupAlarmChan_V41.html)

建立报警上传通道，获取报警等信息。

## Parameters

- `lUserID`：[in] NET_DVR_Login或者NET_DVR_Login_V30的返回值
- `lpSetupParam`：[in] 报警布防参数

## Return Values

-1表示失败，其他值作为NET_DVR_CloseAlarmChan_V30函数的句柄参数。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

使用该接口支持上传V3.0以上版本支持的设备的报警结构。启动布防前，需要调用注册回调函数的接口NET_DVR_SetDVRMessageCallBack_V30才能获取到上传的报警等信息。

## See Also

NET_DVR_CloseAlarmChan_V30 

NET_DVR_Login  NET_DVR_Login_V40

NET_DVR_SetDVRMessageCallBack_V30

## Reference Interface

该接口扩展源于

NET_DVR_SetupAlarmChan_V30

## 相关链接

- [LPNET_DVR_SETUPALARM_PARAM](../structures/NET_DVR_SETUPALARM_PARAM.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_CloseAlarmChan_V30](NET_DVR_CloseAlarmChan_V30.md)
- [NET_DVR_Login](NET_DVR_Login.md)
- [NET_DVR_Login_V40](NET_DVR_Login_V40.md)
- [NET_DVR_SetDVRMessageCallBack_V30](NET_DVR_SetDVRMessageCallBack_V30.md)
- [NET_DVR_SetupAlarmChan_V30](NET_DVR_SetupAlarmChan_V30.md)
