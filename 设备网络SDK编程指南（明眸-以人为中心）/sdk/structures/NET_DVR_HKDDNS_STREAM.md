# NET_DVR_HKDDNS_STREAM

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_HKDDNS_STREAM.html](https://open.hikvision.com/hardware/structures/NET_DVR_HKDDNS_STREAM.html)

hiDDNS取流配置结构体。

## 语法

```c
struct{
  BYTE    byEnable;
  BYTE    byRes[3];
  BYTE    byDDNSDomain[64];
  WORD    wPort;
  WORD    wAliasLen;
  BYTE    byAlias[NAME_LEN];
  WORD    wDVRSerialLen;
  BYTE    byRes1[2];
  BYTE    byDVRSerialNumber[SERIALNO_LEN];
  BYTE    byUserName[NAME_LEN];
  BYTE    byPassWord[PASSWD_LEN];
  BYTE    byChannel;
  BYTE    byRes2[11];
}NET_DVR_HKDDNS_STREAM,*LPNET_DVR_HKDDNS_STREAM;
```

## Members

- `byEnable`：是否启用
- `byRes`：保留
- `byDDNSDomain`：hiDDNS服务器地址
- `wPort`：hiDDNS端口，默认：80
- `wAliasLen`：别名长度
- `byAlias`：别名
- `wDVRSerialLen`：序列号长度
- `byRes1`：保留
- `byDVRSerialNumber`：设备序列号
- `byUserName`：设备登录用户名
- `byPassWord`：设备登录密码
- `byChannel`：设备通道号
- `byRes2`：保留

## See Also

NET_DVR_GET_STREAM_UNION

## 相关链接

- [NET_DVR_GET_STREAM_UNION](../structures/NET_DVR_GET_STREAM_UNION.md)
