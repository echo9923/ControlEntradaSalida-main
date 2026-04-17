# Members


- `dwSize`：结构体大小
- `dwCmdType`：信令类型：0- 请求呼叫，1- 取消本次呼叫，2- 接听本次呼叫，3- 拒绝本地来电呼叫，4- 被叫响铃超时，5- 结束本次通话，6- 设备正在通话中，7- 客户端正在通话中
- `wPeriod`：期号，取值范围：[0,9]
- `wBuildingNumber`：楼号
- `wUnitNumber`：单元号
- `wFloorNumber`：层号
- `wRoomNumber`：房间号
- `byRes`：保留，置为0
