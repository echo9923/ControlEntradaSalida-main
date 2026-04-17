# NET_DVR_SetAlarmDeviceUser

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_SetAlarmDeviceUser.html](https://open.hikvision.com/hardware/definitions/NET_DVR_SetAlarmDeviceUser.html)

报警主机设备用户配置。

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `lUserIndex`：[in] 报警主机设备用户索引
- `lpDeviceUser`：[in] 设备用户配置

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

报警主机设备用户，即网络用户，包括admin用户、管理员、普通操作员，最大个数和支持的权限可以通过能力集NET_DVR_GetDeviceAbility（能力集类型：DEVICE_USER_ABILITY，对应节点）获取。

admin用户：默认的第一个用户为admin用户，admin用户也属于管理员，但权限要高于其他普通管理员。一个设备只有一个admin用户，admin用户可以设置并修改普通用户的权限，可以查看所有用户的信息，admin用户的权限不可修改。

管理员用户：对视频报警主机，拥有除了恢复默认参数、格式化硬盘、升级系统程序、重启外的所有admin用户的权限。其他报警主机的管理员用户可以拥有所有权限。管理员权限不能被修改，admin用户也不能修改管理员权限。管理员用户可以查看普通用户和自己的信息，不能查看admin用户及其他管理员用户信息，可以设置和修改普通用户的权限，不能修改自己的权限。

普通用户：默认拥有获取参数权限，其他权限均需设置。设置的最大化权限为管理员用户所拥有的权限。普通用户只能查看自己的信息，不能查看admin用户、管理员用户及其他普通用户的信息，不能修改自己的权限。

接口支持的设备：DS_19XX（1900系列，只有1906产品，其他都为串口协议）、DS_19DXX（动环监控报警主机）、DS_19AXX（通用报警主机类产品）、DS_19CXX（自助银行报警主机）、DS_1HXX（防护舱）。设备用户是指通过SDK连接的远程登录的用户。而操作用户是指报警主机的本地用户，如用键盘操作的用户。

## See Also

NET_DVR_GetAlarmDeviceUser

NET_DVR_Login   NET_DVR_Login_V40

## 相关链接

- [NET_DVR_ALARM_DEVICE_USER](../structures/NET_DVR_ALARM_DEVICE_USER.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ALARM.md)
- [NET_DVR_GetAlarmDeviceUser](NET_DVR_GetAlarmDeviceUser.md)
- [NET_DVR_Login](NET_DVR_Login.md)
- [NET_DVR_Login_V40](NET_DVR_Login_V40.md)
