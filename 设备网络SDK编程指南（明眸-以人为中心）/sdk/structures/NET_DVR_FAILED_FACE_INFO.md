# NET_DVR_FAILED_FACE_INFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FAILED_FACE_INFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_FAILED_FACE_INFO.html)

建模失败人脸信息结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byCardNo[ACS_CARD_NO_LEN];
  BYTE     byErrorCode;
  BYTE     byRes[127];
}NET_DVR_FAILED_FACE_INFO, *LPNET_DVR_FAILED_FACE_INFO;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：人脸关联的卡号
- `byErrorCode`：失败错误码，0-无效，1-读取图片文件失败，2-打开图片文件失败，3-内存不足，4-人脸建模失败，5-眼间距太小（小于60），6-卡权限不存在
- `byRes`：保留

## See Also

/NET_DVR_StartRemoteConfig  NET_DVR_StopRemoteConfig

## 相关链接

- [/NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_GetFailedFaceInfo.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig_ACS_GetFailedFaceInfo.md)
