# NET_DVR_IPDEVINFO_V31

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPDEVINFO_V31.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPDEVINFO_V31.html)

IP设备信息结构体。

## 语法

```c
struct{
  BYTE               byEnable;
  BYTE               byProType;
  BYTE               byEnableQuickAdd;
  BYTE               byCameraType;
  BYTE               sUserName[NAME_LEN];
  BYTE               sPassword[PASSWD_LEN];
  BYTE               byDomain[MAX_DOMAIN_NAME];
  NET_DVR_IPADDR     struIP;
  WORD               wDVRPort;
  BYTE               szDeviceID[DEV_ID_LEN];
  BYTE               byEnableTiming;
  BYTE               byRes2;
}NET_DVR_IPDEVINFO_V31, *LPNET_DVR_IPDEVINFO_V31;
```

## Members

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

## Remarks

当某个IP设备参数对应的所有IP通道被删除，即IP通道资源的中所有IP通道参数的IPID减1没有与该IP设备参数的下标值相对应的时候，设备本地的该IP设备参数将被删除。

在该结构体中，设备域名为空，ipv4地址有效时，使用ipv4地址去连接设备；ipv4和设备域名都为空，ipv6地址有效时，使用ipv6去连接设备。

当协议类型为GB28181时，设置NET_DVR_IPDEVINFO_V31中的设备IP地址及域名字段无效，byDeviceID字段有；其它协议接入时，byDeviceID无效，设备IP及域名字段有效。

## See Also

NET_DVR_IPALARMINFO_V31    NET_DVR_IPPARACFG_V31    
NET_DVR_IPPARACFG_V40

## 相关链接

- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_GetIPCProtoList](../definitions/NET_DVR_GetIPCProtoList.md)
- [NET_DVR_IPALARMINFO_V31](../structures/NET_DVR_IPALARMINFO_V31.md)
- [NET_DVR_IPPARACFG_V31](../structures/NET_DVR_IPPARACFG_V31.md)
- [NET_DVR_IPPARACFG_V40](../structures/NET_DVR_IPPARACFG_V40.md)
