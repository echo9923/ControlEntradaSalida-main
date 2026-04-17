# NET_DVR_IPSERVER_STREAM

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_IPSERVER_STREAM.html](https://open.hikvision.com/hardware/structures/NET_DVR_IPSERVER_STREAM.html)

IP Server模式配置结构体。

## 语法

```c
struct{
  BYTE               byEnable;
  BYTE               byRes[3];
  NET_DVR_IPADDR     struIPServer;
  WORD               wPort;
  WORD               wDvrNameLen;
  BYTE               byDVRName[NAME_LEN];
  WORD               wDVRSerialLen;
  WORD               byRes1[2];
  BYTE               byDVRSerialNumber[SERIALNO_LEN];
  BYTE               byUserName[NAME_LEN];
  BYTE               byPassWord[PASSWD_LEN];
  BYTE               byChannel;
  BYTE               byRes2[11];
}NET_DVR_IPSERVER_STREAM, *LPNET_DVR_IPSERVER_STREAM;
```

## Members

- `byEnable`：是否启用
- `byRes`：保留，置为0
- `struIPServer`：IPServer 地址
- `wPort`：IPServer 端口
- `wDvrNameLen`：DVR 名称长度
- `byDVRName`：DVR名称
- `wDVRSerialLen`：序列号长度
- `byRes1`：保留，置为0
- `byDVRSerialNumber`：DVR序列号
- `byUserName`：DVR 登陆用户名
- `byPassWord`：DVR 登陆密码
- `byChannel`：DVR 通道，参数值为通道号，例如byChannel=1表示通道1
- `byRes2`：保留，置为0

## See Also

NET_DVR_GET_STREAM_UNION

## 相关链接

- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
