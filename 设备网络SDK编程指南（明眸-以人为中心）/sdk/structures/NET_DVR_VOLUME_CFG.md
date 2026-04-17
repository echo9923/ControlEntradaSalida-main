# NET_DVR_VOLUME_CFG

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_VOLUME_CFG.html](https://open.hikvision.com/hardware/structures/NET_DVR_VOLUME_CFG.html)

音量调节参数配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  WORD     wVolume[MAX_AUDIOOUT_PRO_TYPE];
  BYTE     byPhantomPowerSupply;
  BYTE     byEnableAEC;
  BYTE     byRes1[2];
  BYTE     byEnableFBC[MAX_AUDIOOUT_PRO_TYPE];
  WORD     wVolumeEx[MAX_AUDIOOUT_PRO_TYPE];
  BYTE     byRes[4];
 }NET_DVR_VOLUME_CFG,*LPNET_DVR_VOLUME_CFG;
```

## Members

- `dwSize`：结构体大小
- `wVolume`：音量大小，数组0表示音频输出，数组1表示音频编码，具体索引代表含义以能力集为准
- `byPhantomPowerSupply`：是否使用幻象电源供电(音频输入通道为MIC时有效)：0- 无意义，1- 不供电，2- 供电
- `byEnableAEC`：是否启用全局的回声消除：0- 不启用，1- 启用
- `byRes1`：保留，置为0
- `byEnableFBC`：是否启用FBC(啸叫抑制)：0- 不启用，1- 启用
- `wVolumeEx`：音量大小扩展，具体索引代表含义以能力集为准
- `byRes`：保留，置为0

## See Also

设备支持的音频输入输出口音量调节参数能力以设备返回的能力集为准,对应的能力集：为IP_VIEW_DEV_ABILITY能力集的节点。

通道号有效，表示音频输出口号。

## See Also

录播主机：NET_DVR_GetDVRConfig     NET_DVR_SetDVRConfig

庭审主机：NET_DVR_GetDVRConfig     NET_DVR_SetDVRConfig

楼宇可视对讲：NET_DVR_GetDVRConfig     NET_DVR_SetDVRConfig

## 相关链接

- [RecordingHostAbility](../XMLs/DEVICE_ABILITY_INFO_RECORDHOST.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_RECORD.md)
- [TrialHostAbility](../XMLs/DEVICE_ABILITY_INFO_TRIAL.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_INQUEST.md)
- [AudioVideoCompressInfo](../XMLs/DEVICE_ENCODE_ALL_ABILITY_V20.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_RECORD.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_RECORD.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_TRIAL.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_TRIAL.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_INTERCOM.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_INTERCOM.md)
