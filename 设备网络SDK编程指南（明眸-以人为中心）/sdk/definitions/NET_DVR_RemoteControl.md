# NET_DVR_RemoteControl

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_RemoteControl.html](https://open.hikvision.com/hardware/definitions/NET_DVR_RemoteControl.html)

远程控制。

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwCommand`：[in] 控制命令，详见列表
- `lpInBuffer`：[in] 输入参数，具体内容跟控制命令相关，详见列表
- `dwInBufferSize`：[in] 输入参数长度

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_Login_V40

## 相关链接

- [NET_DVR_REMOTECONTROL_ALARM_PARAM](../structures/NET_DVR_REMOTECONTROL_ALARM_PARAM.md)
- [NET_DVR_REMOTECONTROL_STUDY_PARAM](../structures/NET_DVR_REMOTECONTROL_STUDY_PARAM.md)
- [NET_DVR_WIRELESS_ALARM_STUDY_PARAM](../structures/NET_DVR_WIRELESS_ALARM_STUDY_PARAM.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Login_V40](../definitions/NET_DVR_Login_V40.md)
