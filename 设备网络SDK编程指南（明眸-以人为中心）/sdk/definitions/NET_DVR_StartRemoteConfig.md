# NET_DVR_StartRemoteConfig

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_StartRemoteConfig.html](https://open.hikvision.com/hardware/definitions/NET_DVR_StartRemoteConfig.html)

启动远程配置。

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwCommand`：[in] 配置命令，不同的功能对应不同的命令号(dwCommand)，lpInBuffer等参数也对应不同的内容，如下表所示：

	

		
dwCommand宏定义

        
宏定义值

        
含义

		
lpInBuffer

        
cbStateCallback

	

	

		
NET_DVR_FIND_NAS_DIRECTORY

		
6161

        
查找NAS目录

		
NET_DVR_NET_DISK_SERACH_PARAM

        
NULL
- `lpInBuffer`：[in] 输入参数，具体内容跟配置命令相关，详见列表
- `dwInBufferLen`：[in] 输入缓冲的大小
- `cbStateCallback`：[in] 状态回调函数
- `pUserData`：[in] 用户数据

## Callback Function

```text
typedef void(CALLBACK *fRemoteConfigCallback)(
  DWORD     dwType,
  void      *lpBuffer,
  DWORD     dwBufLen,
  void      *pUserData
);
```

typedef void(CALLBACK *fRemoteConfigCallback)(
  DWORD     dwType,
  void      *lpBuffer,
  DWORD     dwBufLen,
  void      *pUserData
);

## Callback Function Parameters

- `dwType`：[out] 状态
- `lpBuffer`：[out] 存放数据的缓冲区指针，获取音量时dwType状态无效，lpBuffer对应4字节声音强度
- `dwBufLen`：[out] 缓冲区大小
- `pUserData`：[out] 用户数据

## Return Values

-1表示失败，其他值作为NET_DVR_GetNextRemoteConfig、NET_DVR_StopRemoteConfig的句柄。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

调用该接口启动长连接远程配置后，还需要调用其他接口获取、设置相关参数或获取状态，如下表所示：

## See Also

NET_DVR_GetNextRemoteConfig   NET_DVR_GetRemoteConfigState   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_NET_DISK_SERACH_PARAM](../structures/NET_DVR_NET_DISK_SERACH_PARAM.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_GetNextRemoteConfig](../definitions/NET_DVR_GetNextRemoteConfig.md)
- [NET_DVR_GetRemoteConfigState](../definitions/NET_DVR_GetRemoteConfigState.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig.md)
