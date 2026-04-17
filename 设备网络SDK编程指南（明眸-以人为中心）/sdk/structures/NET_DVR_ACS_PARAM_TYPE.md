# NET_DVR_ACS_PARAM_TYPE

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ACS_PARAM_TYPE.html](https://open.hikvision.com/hardware/structures/NET_DVR_ACS_PARAM_TYPE.html)

门禁主机参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  DWORD    dwParamType;
  WORD    wLocalControllerID;
  BYTE     byRes[30];
}NET_DVR_ACS_PARAM_TYPE,*LPNET_DVR_ACS_PARAM_TYPE;
```

## Members

- `dwSize`：结构体大小
- `dwParamType`：参数类型，按位表示，每位代表一种参数，值：0- 不处理，1- 处理

宏定义

宏定义值

含义

ACS_PARAM_DOOR_STATUS_WEEK_PLAN

0x00000001

门状态周计划参数

ACS_PARAM_VERIFY_WEEK_PALN

0x00000002

读卡器周计划参数

ACS_PARAM_CARD_RIGHT_WEEK_PLAN

0x00000004

卡权限周计划参数

ACS_PARAM_DOOR_STATUS_HOLIDAY_PLAN

0x00000008

门状态假日计划参数

ACS_PARAM_VERIFY_HOLIDAY_PALN

0x00000010

读卡器假日计划参数

ACS_PARAM_CARD_RIGHT_HOLIDAY_PLAN

0x00000020

卡权限假日计划参数

ACS_PARAM_DOOR_STATUS_HOLIDAY_GROUP

0x00000040

门状态假日组参数

ACS_PARAM_VERIFY_HOLIDAY_GROUP

0x00000080

读卡器验证方式假日组参数

ACS_PARAM_CARD_RIGHT_HOLIDAY_GROUP

0x00000100

卡权限假日组参数

ACS_PARAM_DOOR_STATUS_PLAN_TEMPLATE

0x00000200

门状态计划模板参数

ACS_PARAM_VERIFY_PALN_TEMPLATE

0x00000400

读卡器验证方式计划模板参数

ACS_PARAM_CARD_RIGHT_PALN_TEMPLATE

0x00000800

卡权限计划模板参数

ACS_PARAM_CARD

0x00001000

卡参数

ACS_PARAM_GROUP

0x00002000

群组参数

ACS_PARAM_ANTI_SNEAK_CFG

0x00004000

反潜回参数

ACS_PAPAM_EVENT_CARD_LINKAGE

0x00008000

事件及卡号联动参数

ACS_PAPAM_CARD_PASSWD_CFG

0x00010000

密码开门使能参数

ACS_PARAM_PERSON_STATISTICS_CFG

0x00020000

人数统计参数

ACS_PARAM_BLACK_LIST_PICTURE

0x00040000

黑名单图片参数

ACS_PARAM_ID_BLACK_LIST

0x00080000

身份证黑名单参数

ACS_PARAM_EXAM_INFO

0x00100000

考试信息参数

ACS_PARAM_EXAMINEE_INFO

0x00200000

考生信息参数

ACS_PARAM_FAILED_FACE_INFO

0x00400000

升级设备人脸建模失败记录
- `wLocalControllerID`：就地控制器序号[1,255],0代表门禁主机
- `byRes`：保留，置为0

## Remarks

清空门禁主机参数控制能力，对应门禁主机能力集（接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY）中节点。

## See Also

NET_DVR_RemoteControl

## 相关链接

- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_RemoteControl](../definitions/NET_DVR_RemoteControl_ACS.md)
