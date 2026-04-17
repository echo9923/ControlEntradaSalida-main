# NET_DVR_CARD_CFG_V50

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CARD_CFG_V50.html](https://open.hikvision.com/hardware/structures/NET_DVR_CARD_CFG_V50.html)

卡参数配置结构体。

## 语法

```c
struct{
  DWORD                       dwSize;
  DWORD                       dwModifyParamType;
  BYTE                        byCardNo[ACS_CARD_NO_LEN];
  BYTE                        byCardValid;
  BYTE                        byCardType;
  BYTE                        byLeaderCard;
  BYTE                        byUserType;;
  BYTE                        byDoorRight[MAX_DOOR_NUM];
  NET_DVR_VALID_PERIOD_CFG    struValid;
  BYTE                        byBelongGroup[MAX_GROUP_NUM];
  BYTE                        byCardPassword[CARD_PASSWORD_LEN];
  WORD                        wCardRightPlan[MAX_DOOR_NUM][MAX_CARD_RIGHT_PLAN_NUM];
  DWORD                       dwMaxSwipeTime;
  DWORD                       dwSwipeTime;
  WORD                        wRoomNumber;
  SHORT                       wFloorNumber;
  DWORD                       dwEmployeeNo;
  BYTE                        byName[NAME_LEN];
  WORD                        wDepartmentNo;
  WORD                        wSchedulePlanNo;
  BYTE                        bySchedulePlanType;
  BYTE                        byRes2[3];
  DWORD                       dwLockID;
  BYTE                        byLockCode[MAX_LOCK_CODE_LEN];
  BYTE                        byRoomCode[MAX_DOOR_CODE_LEN];
  DWORD                       dwCardRight;
  DWORD                       dwPlanTemplate;
  DWORD                       dwCardUserId;
  BYTE                        byCardModelType;
  BYTE                        byRes3[83];
}NET_DVR_CARD_CFG_V50,*LPNET_DVR_CARD_CFG_V50;
```

## Members

- `dwSize`：结构体大小
- `dwModifyParamType`：需要修改的卡参数（设置卡参数时有效），按位表示，每位代表一种参数，值：0- 不修改，1- 需要修改

宏定义

宏定义值

含义

CARD_PARAM_CARD_VALID

0x00000001

卡是否有效参数

CARD_PARAM_VALID

0x00000002

有效期参数

CARD_PARAM_CARD_TYPE

0x00000004

卡类型参数

CARD_PARAM_DOOR_RIGHT

0x00000008

门权限参数

CARD_PARAM_LEADER_CARD

0x00000010

首卡参数

CARD_PARAM_SWIPE_NUM

0x00000020

最大刷卡次数参数

CARD_PARAM_GROUP

0x00000040

所属群组参数

CARD_PARAM_PASSWORD

0x00000080

卡密码参数

CARD_PARAM_RIGHT_PLAN

0x00000100

卡权限计划参数

CARD_PARAM_SWIPED_NUM

0x00000200

已刷卡次数

CARD_PARAM_SWIPED_NUM

0x00000200

已刷卡次数

CARD_PARAM_EMPLOYEE_NO

0x00000400

工号

CARD_PARAM_NAME

0x00000800

姓名

CARD_PARAM_DEPARTMENT_NO

0x00001000

部门编号

CARD_SCHEDULE_PLAN_NO

0x00002000

排班计划编号

CARD_SCHEDULE_PLAN_TYPE

0x00004000

排班计划类型

CARD_USER_TYPE

0x00040000

用户类型
- `byCardNo`：卡号，特殊卡号定义如下：

0xFFFFFFFFFFFFFFFF：非法卡号

0xFFFFFFFFFFFFFFFE：胁迫码

0xFFFFFFFFFFFFFFFD：超级码

0xFFFFFFFFFFFFFFFC~0xFFFFFFFFFFFFFFF1：预留的特殊卡

0xFFFFFFFFFFFFFFF0：最大合法卡号
- `byCardValid`：卡是否有效：0- 无效，1- 有效（用于删除卡，设置时置为0进行删除，获取时此字段始终为1）
- `byCardType`：卡类型：1- 普通卡（默认），2- 残疾人卡，3- 黑名单卡，4- 巡更卡，5- 胁迫卡，6- 超级卡，7- 来宾卡，8- 解除卡，9- 员工卡，10- 应急卡，11- 应急管理卡（用于授权临时卡权限，本身不能开门），默认普通卡
- `byLeaderCard`：是否为首卡：1- 是，0- 否
- `byUserType`：用户类型：0 – 普通用户1- 管理员用户
- `byDoorRight`：门权限（梯控的楼层权限、锁权限），按字节表示，1-为有权限，0-为无权限，从低位到高位依次表示对门（或者梯控楼层、锁）1-N是否有权限
- `struValid`：有效期参数（有效时间跨度为1970年1月1日0点0分0秒~2037年12月31日23点59分59秒）
- `byBelongGroup`：所属群组，按字节表示，1-属于，0-不属于，从低位到高位表示是否从属群组1~N
- `byCardPassword`：卡密码
- `wCardRightPlan`：卡权限计划，取值为计划模板编号，同个门（锁）不同计划模板采用权限或的方式处理
- `dwMaxSwipeTime`：最大刷卡次数，0为无次数限制
- `dwSwipeTime`：已刷卡次数
- `wRoomNumber`：房间号
- `wFloorNumber`：层号
- `dwEmployeeNo`：工号（用户ID）
- `byName`：姓名
- `wDepartmentNo`：部门编号
- `wSchedulePlanNo`：排班计划编号
- `bySchedulePlanType`：排班计划类型：0- 无意义，1- 个人，2- 部门
- `byRes2`：保留，置为0
- `dwLockID`：锁ID
- `byLockCode`：锁代码
- `byRoomCode`：房间代码

按位表示，0-无权限，1-有权限

第0位表示：弱电报警

第1位表示：开门提示音

第2位表示：限制客卡

第3位表示：通道

第4位表示：反锁开门

第5位表示：巡更功能
- `dwCardRight`：卡权限
- `dwPlanTemplate`：计划模板(每天)各时间段是否启用，按位表示，0--不启用，1-启用
- `dwCardUserId`：持卡人ID
- `byCardModelType`：0-空，1- MIFARE S50，2- MIFARE S70，3- FM1208 CPU卡，4- FM1216 CPU卡，5-国密CPU卡，6-身份证，7- NFC
- `byRes2`：保留，置为0

## Remarks

结构体里面的工号、姓名、部门编号、排班计划编号、排班计划类型，对于考勤一体机（DS-K1T803F、DS-K1A801F）为必填项，对于其他门禁设备不是必须项。

卡参数能力，对应门禁主机能力集（接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY）中节点。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_SendRemoteConfig

## 相关链接

- [NET_DVR_VALID_PERIOD_CFG](../structures/NET_DVR_VALID_PERIOD_CFG.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS.md)
- [NET_DVR_SendRemoteConfig](../definitions/NET_DVR_SendRemoteConfig_ACS.md)
