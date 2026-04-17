# NET_DVR_Logout_V30

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_Logout_V30.html](https://open.hikvision.com/hardware/definitions/NET_DVR_Logout_V30.html)

用户注销。

## Parameters

- `lUserID`：[in] 
   用户ID号，NET_DVR_Login_V40等登录接口的返回值

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## Remarks

该接口强制停止该用户的所有操作和释放所有的资源，确保该ID对应的线程都安全退出，资源得到释放。建议使用NET_DVR_Logout接口实现注销功能。

## See Also

NET_DVR_Login_V40

## Reference Interface

该接口扩展源于

NET_DVR_Logout

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Logout](NET_DVR_Logout.md)
- [NET_DVR_Login_V40](NET_DVR_Login_V40.md)
