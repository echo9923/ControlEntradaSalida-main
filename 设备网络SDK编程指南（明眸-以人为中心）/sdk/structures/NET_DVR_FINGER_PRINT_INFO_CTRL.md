# NET_DVR_FINGER_PRINT_INFO_CTRL

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_INFO_CTRL.html](https://open.hikvision.com/hardware/structures/NET_DVR_FINGER_PRINT_INFO_CTRL.html)

指纹删除控制参数结构体。

## 语法

```c
struct{
  DWORD                            dwSize;
  BYTE                             byMode;
  BYTE                             byRes1[3];
  NET_DVR_DEL_FINGER_PRINT_MODE    struProcessMode;
  BYTE                             byRes[64];
}NET_DVR_FINGER_PRINT_INFO_CTRL, *LPNET_DVR_FINGER_PRINT_INFO_CTRL;
```

## Members

- `dwSize`：结构体大小
- `byMode`：删除方式：0- 按卡号方式删除，1- 按读卡器删除
- `byRes1`：保留，置为0
- `struProcessMode`：处理方式
- `byRes`：保留，置为0

## Remarks

设备是否支持指纹参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_RemoteControl

## 相关链接

- [NET_DVR_DEL_FINGER_PRINT_MODE](../structures/NET_DVR_DEL_FINGER_PRINT_MODE.md)
- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_RemoteControl](../definitions/NET_DVR_RemoteControl_ACS_finger.md)
