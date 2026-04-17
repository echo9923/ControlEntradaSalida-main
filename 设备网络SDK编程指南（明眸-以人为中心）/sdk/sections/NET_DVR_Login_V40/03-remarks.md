# Remarks


pLoginInfo中bUseAsynLogin为0时登录为同步模式，接口返回成功即表示登录成功；pLoginInfo中bUseAsynLogin为1时登录为异步模式，登录是否成功在输入参数设置的回调函数中返回。

DS-7116、DS-81xx、DS-90xx、DS-91xx等系列设备允许有32个注册用户名，且同时最多允许128个用户注册；DS-80xx等设备允许有16个注册用户名，且同时最多允许128个用户注册。

SDK支持2048个注册，返回UserID的取值范围为0~2047。
