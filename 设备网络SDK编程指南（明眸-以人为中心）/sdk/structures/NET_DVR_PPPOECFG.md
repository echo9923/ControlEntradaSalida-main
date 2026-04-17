# NET_DVR_PPPOECFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_PPPOECFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_PPPOECFG.html)

PPPoE配置结构体。

## 语法

```c
struct{
  DWORD             dwPPPOE;
  BYTE              sPPPoEUser[NAME_LEN];
  char              sPPPoEPassword[PASSWD_LEN];
  NET_DVR_IPADDR    struPPPoEIP;
}NET_DVR_PPPOECFG, *LPNET_DVR_PPPOECFG;
```

## Members

- `dwPPPOE`：是否启用PPPoE：0-不启用，1-启用
- `sPPPoEUser`：PPPoE用户名
- `sPPPoEPassword`：PPPoE密码
- `struPPPoEIP`：PPPoE IP地址

## See Also

NET_DVR_NETCFG_V30

NET_DVR_NETCFG_MULTI

## 相关链接

- [NET_DVR_IPADDR](../structures/NET_DVR_IPADDR.md)
- [NET_DVR_NETCFG_V30](../structures/NET_DVR_NETCFG_V30.md)
- [NET_DVR_NETCFG_MULTI](../structures/NET_DVR_NETCFG_MULTI.md)
