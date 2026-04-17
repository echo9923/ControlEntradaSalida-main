# NET_DVR_CAPTURE_FACE_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CAPTURE_FACE_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_CAPTURE_FACE_CFG.html)

人脸信息采集结果结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  DWORD    dwFaceTemplate1Size;
  char     *pFaceTemplate1Buffer;
  DWORD    dwFaceTemplate2Size;
  char     *pFaceTemplate2Buffer;
  DWORD    dwFacePicSize;
  char     *pFacePicBuffer;
  BYTE     byFaceQuality1;
  BYTE     byFaceQuality2;
  BYTE     byCaptureProgress;
  BYTE     byRes[125];
}NET_DVR_CAPTURE_FACE_CFG, *LPNET_DVR_CAPTURE_FACE_CFG;
```

## Members

- `dwSize`：结构体大小
- `dwFaceTemplate1Size`：人脸模板1数据大小，等于0时表示无人脸模板1数据
- `pFaceTemplate1Buffer`：人脸模板1数据缓存（不大于2.5k）
- `dwFaceTemplate2Size`：人脸模板2数据大小，等于0时表示无人脸模板2数据
- `pFaceTemplate2Buffer`：人脸模板2数据缓存（不大于2.5K）
- `dwFacePicSize`：人脸图片数据大小，等于0时表示无人脸图片数据
- `pFacePicBuffer`：人脸图片数据缓存
- `byFaceQuality1`：模板1对应的人脸质量，取值范围：1~100
- `byFaceQuality2`：模板2对应的人脸质量，取值范围：1~100
- `byCaptureProgress`：采集进度，目前只有两种进度值：0- 未采集到人脸，100- 采集到人脸（只有在进度为100时，才解析人脸信息）
- `byRes`：保留，置为0

## Remarks

设备是否支持采集人脸信息或者支持的参数能力，可以通过设备能力集进行判断，对应门禁能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_collect.md)
