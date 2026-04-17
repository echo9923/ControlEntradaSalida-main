# NET_DVR_Login_V40

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_Login_V40.html](https://open.hikvision.com/hardware/definitions/NET_DVR_Login_V40.html)

用户注册设备（支持异步登录）。

## Parameters

- `pLoginInfo`：[in] 登录参数，包括设备地址、登录用户、密码等
- `lpDeviceInfo`：[out] 设备信息(同步登录即pLoginInfo中bUseAsynLogin为0时有效)

## Return Values

异步登录的状态、用户ID和设备信息通过NET_DVR_USER_LOGIN_INFO结构体中设置的回调函数(fLoginResultCallBack)返回。对于同步登录，接口返回-1表示登录失败，其他值表示返回的用户ID值。用户ID具有唯一性，后续对设备的操作都需要通过此ID实现。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

pLoginInfo中bUseAsynLogin为0时登录为同步模式，接口返回成功即表示登录成功；pLoginInfo中bUseAsynLogin为1时登录为异步模式，登录是否成功在输入参数设置的回调函数中返回。

DS-7116、DS-81xx、DS-90xx、DS-91xx等系列设备允许有32个注册用户名，且同时最多允许128个用户注册；DS-80xx等设备允许有16个注册用户名，且同时最多允许128个用户注册。

SDK支持2048个注册，返回UserID的取值范围为0~2047。

## See Also

NET_DVR_Logout

## Reference Interface

该接口扩展源于

NET_DVR_Login_V30

## 相关链接

- [LPNET_DVR_USER_LOGIN_INFO](../structures/NET_DVR_USER_LOGIN_INFO.md)
- [LPNET_DVR_DEVICEINFO_V40](../structures/NET_DVR_DEVICEINFO_V40.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Logout](NET_DVR_Logout.md)
- [NET_DVR_Login_V30](NET_DVR_Login_V30.md)
