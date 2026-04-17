# NET_DVR_VIDEO_CALL_PARAM

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_VIDEO_CALL_PARAM.html](https://open.hikvision.com/hardware/structures/NET_DVR_VIDEO_CALL_PARAM.html)

可视对讲信令处理参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  DWORD    dwCmdType;
  WORD     wPeriod;
  WORD     wBuildingNumber;
  WORD     wUnitNumber;
  SHORT    wFloorNumber;
  WORD     wRoomNumber;
  BYTE     byRes[118];
}NET_DVR_VIDEO_CALL_PARAM, *LPNET_DVR_VIDEO_CALL_PARAM;
```

## Members

- `dwSize`：结构体大小
- `dwCmdType`：信令类型：0- 请求呼叫，1- 取消本次呼叫，2- 接听本次呼叫，3- 拒绝本地来电呼叫，4- 被叫响铃超时，5- 结束本次通话，6- 设备正在通话中，7- 客户端正在通话中
- `wPeriod`：期号，取值范围：[0,9]
- `wBuildingNumber`：楼号
- `wUnitNumber`：单元号
- `wFloorNumber`：层号
- `wRoomNumber`：房间号
- `byRes`：保留，置为0

## Remarks

可视通话能力，对应IP可视对讲主机能力集（接口：NET_DVR_GetDeviceAbility，能力集类型：IP_VIEW_DEV_ABILITY）中节点。

该长连接配置接口结合报警、预览及对讲接口，可以完成可视通话的功能，具体流程机制如下所示：

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_SendRemoteConfig

## 相关链接

- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_INTERCOM.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_intercom_call.md)
- [NET_DVR_SendRemoteConfig](../definitions/NET_DVR_SendRemoteConfig_intercom_call.md)
