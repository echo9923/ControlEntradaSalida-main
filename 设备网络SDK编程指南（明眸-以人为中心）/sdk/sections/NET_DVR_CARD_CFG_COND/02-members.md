# Members


- `dwSize`：结构体大小
- `dwCardNum`：设置或获取卡数量，获取时置为0xffffffff表示获取所有卡信息
- `byCheckCardNo`：设备是否进行卡号校验：0- 不校验，1- 校验
- `byRes1`：保留，置为0
- `wLocalControllerID`：就地控制器序号，表示往就地控制器下发离线卡参数，0代表是门禁主机
- `byRes2`：保留，置为0
- `dwLockID`：锁ID
- `byRes3`：保留，置为0
