# Members


- `byEnable`：该IP设备是否启用
- `byProType`：协议类型(默认为私有协议)：0- 私有协议，1- 松下协议，2- 索尼，更多协议通过NET_DVR_GetIPCProtoList获取。
- `byEnableQuickAdd`：0- 不支持快速添加；1- 使用快速添加，快速添加需要设备IP或设备ID(GB28181协议接入)和协议类型，其他参数信息由设备默认指定
- `byCameraType`：通道接入的相机类型：0-无意义，1-老师跟踪，2-学生跟踪，3-老师全景，4-学生全景，5-多媒体，6–教师定位，7-学生定位，8-板书定位，9-板书相机
- `sUserName`：用户名
- `sPassword`：密码
- `byDomain`：设备域名
- `struIP`：IP地址
- `wDVRPort`：端口号
- `szDeviceID`：设备ID，GB28181协议接入时有效
- `byEnableTiming`：0-保留，1-不启用NVR对IPC自动校时，2-启用NVR对IPC自动校时
- `byRes2`：保留，置为0
