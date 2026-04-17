# NET_DVR_GetNextRemoteConfig

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_GetNextRemoteConfig.html](https://open.hikvision.com/hardware/definitions/NET_DVR_GetNextRemoteConfig.html)

逐个获取查找到的信息。

## Parameters

- `lHandle`：[in] 查找句柄，NET_DVR_StartRemoteConfig的返回值
- `lpOutBuff`：[out] 输出数据缓冲区，与NET_DVR_StartRemoteConfig的命令（dwCommand）有关，详见列表
- `dwOutBuffSize`：[in] 缓冲区长度

## Remarks

在调用该接口获取查找之前，必须先调用NET_DVR_StartRemoteConfig得到当前的查找句柄。此接口用于获取一条已查找到的信息，若要获取全部的已查找到的信息，需要循环调用此接口。

调用NET_DVR_StartRemoteConfig时传入不同的命令号(dwCommand)，lpOutBuff对应不同的结构体，如下表所示：

## Return Values

-1表示失败，其他值表示当前的获取状态等信息，详见下表。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_NET_DISK_SERACH_RET](../structures/NET_DVR_NET_DISK_SERACH_RET.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_StartRemoteConfig](NET_DVR_StartRemoteConfig.md)
- [NET_DVR_StopRemoteConfig](NET_DVR_StopRemoteConfig.md)
