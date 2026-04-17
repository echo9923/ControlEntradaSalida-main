# Members


- `byEnable`：是否启用
- `byRes1`：保留，置为0
- `struStreamServer`：流媒体服务器地址
- `wStreamServerPort`：流媒体服务器端口
- `byStreamServerTransmitType`：流媒体传输协议类型：0- TCP，1- UDP
- `byRes2`：保留，置为0
- `struIPServer`：IPServer 地址
- `wIPServerPort`：IPServer 端口
- `byRes3`：保留，置为0
- `sDVRName`：设备名称
- `wDVRNameLen`：设备名称长度
- `wDVRSerialLen`：序列号长度
- `sDVRSerialNumber`：设备序列号
- `sUserName`：设备登陆用户名
- `sPassWord`：设备登陆密码
- `wDVRPort`：设备端口号
- `byRes4`：保留，置为0
- `byChannel`：设备通道，参数值为通道号，例如byChannel=1表示通道1
- `byTransProtocol`：传输协议类型：0- TCP，1- UDP
- `byTransMode`：传输码流模式：0- 主码流，1- 子码流
- `byFactoryType`：前端设备厂家类型，通过接口NET_DVR_GetIPCProtoList获取
