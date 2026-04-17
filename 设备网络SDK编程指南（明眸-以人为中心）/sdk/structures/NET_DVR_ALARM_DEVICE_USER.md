# NET_DVR_ALARM_DEVICE_USER

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ALARM_DEVICE_USER.html](https://open.hikvision.com/hardware/structures/NET_DVR_ALARM_DEVICE_USER.html)

报警主机设备用户配置结构体。

## 语法

```c
struct{
  DWORD              dwSize;
  BYTE               sUserName[NAME_LEN];
  BYTE               sPassword[PASSWD_LEN];
  NET_DVR_IPADDR     struUserIP;
  BYTE               byMACAddr[MACADDR_LEN];
  BYTE               byUserType;
  BYTE               byAlarmOnRight;
  BYTE               byAlarmOffRight;
  BYTE               byBypassRight;
  BYTE               byOtherRight[MAX_RIGHT];
  BYTE               byNetPreviewRight[MAX_ALARMHOST_VIDEO_CHAN];
  BYTE               byNetRecordRight[MAX_ALARMHOST_VIDEO_CHAN];
  BYTE               byNetPlaybackRight[MAX_ALARMHOST_VIDEO_CHAN];
  BYTE               byNetPTZRight[MAX_ALARMHOST_VIDEO_CHAN];
  BYTE  		  sOriginalPassword[PASSWD_LEN];
  BYTE   	           byRes2[152];
}NET_DVR_ALARM_DEVICE_USER,*LPNET_DVR_ALARM_DEVICE_USER;
```

## Members

- `dwSize`：结构体大小
- `sUserName`：用户名
- `sPassword`：密码
- `struUserIP`：用户IP地址(为0时表示允许任何地址)
- `byMACAddr`：物理地址
- `byUserType`：用户类型：0- 普通用户，1- 管理员用户
- `byAlarmOnRight`：布防权限
- `byAlarmOffRight`：撤防权限
- `byBypassRight`：旁路权限
- `byOtherRight`：其他权限，参数取值为1表示使能：

byOtherRight[0]：日志权限

byOtherRight[1]：重启关机

byOtherRight[2]：参数设置权限

byOtherRight[3]：参数获取权限

byOtherRight[4]：恢复默认参数权限

byOtherRight[5]：警号输出权限

byOtherRight[6]：PTZ 控制权限

byOtherRight[7]：远程升级权限
 
byOtherRight[8]：报警输出控制

byOtherRight[9]：串口控制

byOtherRight[10]：门禁控制

byOtherRight[11]：语音对讲

byOtherRight[12]：远程控制本地输出

byOtherRight[13]：硬盘配置

byOtherRight[14]：格式化硬盘

byOtherRight[15]：模拟量控制
- `byNetPreviewRight`：远程可以预览的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `byNetRecordRight`：远程可以录像的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `byNetPlaybackRight`：远程可以回放的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `byNetPTZRight`：远程可以PTZ的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `sOriginalPassword`：原始密码
- `byRes2`：保留

## Remarks

admin用户：

默认的第一个用户为admin用户，admin用户也属于管理员，但权限要高于其他普通管理员。一个设备只有一个admin用户，admin用户可以设置并修改普通用户的权限，可以查看所有用户的信息，admin用户的权限不可修改。

管理员用户:

对视频报警主机，拥有除了恢复默认参数、格式化硬盘、升级系统程序、重启外的所有admin用户的权限。其他报警主机的管理员用户可以拥有所有权限。管理员权限不能被修改，admin用户也不能修改管理员权限。

管理员用户可以查看普通用户和自己的信息，不能查看admin用户及其他管理员用户信息，可以设置和修改普通用户的权限，不能修改自己的权限。

普通用户:

默认拥有获取参数权限，其他权限均需设置。设置的最大化权限为管理员用户所拥有的权限。
普通用户只能查看自己的信息，不能查看admin用户、管理员用户及其他普通用户的信息，不能修改自己的权限。

## See Also

NET_DVR_GetAlarmDeviceUser    NET_DVR_SetAlarmDeviceUser

## 相关链接

- [NET_DVR_IPADDR](NET_DVR_IPADDR.md)
- [NET_DVR_GetAlarmDeviceUser](..\接口定义\NET_DVR_GetAlarmDeviceUser.md)
- [NET_DVR_SetAlarmDeviceUser](..\接口定义\NET_DVR_SetAlarmDeviceUser.md)
