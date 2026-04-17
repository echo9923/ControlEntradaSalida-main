# NET_DVR_CAPTURE_FACE_COND

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CAPTURE_FACE_COND.html](https://open.hikvision.com/hardware/structures/NET_DVR_CAPTURE_FACE_COND.html)

采集人脸信息条件参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byRes[128];
}NET_DVR_CAPTURE_FACE_COND, *LPNET_DVR_CAPTURE_FACE_COND;
```

## Members

- `dwSize`：结构体大小
- `byRes`：保留，置为0

## Remarks

设备是否支持采集人脸信息或者支持的参数能力，可以通过设备能力集进行判断，对应门禁能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_StartRemoteConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_collect.md)
