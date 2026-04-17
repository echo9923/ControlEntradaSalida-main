# NET_DVR_RealPlay_V30

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_RealPlay_V30.html](https://open.hikvision.com/hardware/definitions/NET_DVR_RealPlay_V30.html)

实时预览。

## Parameters

- `lUserID`：[in] 
   NET_DVR_Login_V40等登录接口的返回值
- `lpClientInfo`：[in] 
   预览参数
- `cbRealDataCallBack`：[in] 
   码流数据回调函数
- `pUser`：[in] 
   用户数据
- `bBlocked`：[in] 
   请求码流过程是否阻塞：0－否；1－是

## Callback Function

```text
typedef void(CALLBACK *fRealDataCallBack_V30)(
  LONG      lRealHandle,
  DWORD     dwDataType,
  BYTE      *pBuffer,
  DWORD     dwBufSize,
  void      *pUser
);
```

typedef void(CALLBACK *fRealDataCallBack_V30)(
  LONG      lRealHandle,
  DWORD     dwDataType,
  BYTE      *pBuffer,
  DWORD     dwBufSize,
  void      *pUser
);

## Callback Function Parameters

- `lRealHandle`：[out] 当前的预览句柄
- `dwDataType`：[out] 数据类型

宏定义

宏定义值

含义

NET_DVR_SYSHEAD

1

系统头数据

NET_DVR_STREAMDATA

2

流数据（包括复合流或音视频分开的视频流数据）

NET_DVR_AUDIOSTREAMDATA

3

音频数据

NET_DVR_PRIVATE_DATA

112

私有数据,包括智能信息
- `pBuffer`：[out] 存放数据的缓冲区指针
- `dwBufSize`：[out] 缓冲区大小
- `pUser`：[out] 用户数据

## Return Values

-1表示失败，其他值作为NET_DVR_StopRealPlay等函数的句柄参数。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## Remarks

该接口中可以设置当前预览操作是否阻塞（通过bBlocked参数设置）。若设为不阻塞，表示发起与设备的连接就认为连接成功，如果发生码流接收失败、播放失败等情况以预览异常的方式通知上层。在循环播放的时候可以减短停顿的时间，与NET_DVR_RealPlay处理一致。
若设为阻塞，表示直到播放操作完成才返回成功与否。

该接口中的回调函数可以置为空，这样该函数将不回调码流数据给用户，不过用户仍可以通过接口NET_DVR_SetRealDataCallBack或NET_DVR_SetStandardDataCallBack注册捕获码流数据的回调函数以捕获码流数据。

客户端异常离线时，设备端对取流连接的保持时间为10秒。

## See Also

NET_DVR_StopRealPlay   NET_DVR_GetRealPlayerIndex   NET_DVR_ClientSetVideoEffect

NET_DVR_ClientGetVideoEffect   
NET_DVR_RigisterDrawFun   NET_DVR_OpenSound   
NET_DVR_OpenSoundShare

NET_DVR_CloseSoundShare
   NET_DVR_Volume
   NET_DVR_SetRealDataCallBack

NET_DVR_SetStandardDataCallBack   NET_DVR_SaveRealData   NET_DVR_StopSaveRealData

## Reference Interface

该接口扩展源于

NET_DVR_RealPlay

扩展接口可见

NET_DVR_RealPlay_V40

## 相关链接

- [LPNET_DVR_CLIENTINFO](../structures/NET_DVR_CLIENTINFO.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_StopRealPlay](NET_DVR_StopRealPlay.md)
- [NET_DVR_GetRealPlayerIndex](../definitions/NET_DVR_GetRealPlayerIndex.md)
- [NET_DVR_ClientSetVideoEffect](../definitions/NET_DVR_ClientSetVideoEffect.md)
- [NET_DVR_ClientGetVideoEffect](../definitions/NET_DVR_ClientGetVideoEffect.md)
- [NET_DVR_RigisterDrawFun](../definitions/NET_DVR_RigisterDrawFun.md)
- [NET_DVR_OpenSound](../definitions/NET_DVR_OpenSound.md)
- [NET_DVR_OpenSoundShare](../definitions/NET_DVR_OpenSoundShare.md)
- [NET_DVR_CloseSoundShare](../definitions/NET_DVR_CloseSoundShare.md)
- [NET_DVR_Volume](../definitions/NET_DVR_Volume.md)
- [NET_DVR_SetRealDataCallBack](../definitions/NET_DVR_SetRealDataCallBack.md)
- [NET_DVR_SetStandardDataCallBack](../definitions/NET_DVR_SetStandardDataCallBack.md)
- [NET_DVR_SaveRealData](../definitions/NET_DVR_SaveRealData.md)
- [NET_DVR_StopSaveRealData](../definitions/NET_DVR_StopSaveRealData.md)
- [NET_DVR_RealPlay](NET_DVR_RealPlay.md)
- [NET_DVR_RealPlay_V40](NET_DVR_RealPlay_V40.md)
