# NET_DVR_CARD_CFG_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CARD_CFG_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_CARD_CFG_COND.html)

卡参数配置条件结构体。

## 语法

```c
struct{
  DWORD   dwSize;
  DWORD   dwCardNum;
  BYTE    byCheckCardNo;
  BYTE    byRes1[3];
  WORD    wLocalControllerID;
  BYTE    byRes2[2];
  DWORD   dwLockID;
  BYTE    byRes3[20];
}NET_DVR_CARD_CFG_COND,*LPNET_DVR_CARD_CFG_COND;
```

## Members

- `dwSize`：结构体大小
- `dwCardNum`：设置或获取卡数量，获取时置为0xffffffff表示获取所有卡信息
- `byCheckCardNo`：设备是否进行卡号校验：0- 不校验，1- 校验
- `byRes1`：保留，置为0
- `wLocalControllerID`：就地控制器序号，表示往就地控制器下发离线卡参数，0代表是门禁主机
- `byRes2`：保留，置为0
- `dwLockID`：锁ID
- `byRes3`：保留，置为0

## Remarks

设置卡参数（下发卡参数）时，如果将byCheckCardNo置为0，那么设备将不校验应用层下发的卡号信息，直接写入本地存储，可以一定程度提高卡号下发的速度，但是需要上层应用自己保证卡号信息不重复（整型值不能重复，比如，不能同时含有1和01这两种卡号）。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS.md)
