# NET_DVR_ACS_EVENT_DETAIL

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_ACS_EVENT_DETAIL.html](https://open.hikvision.com/hardware/structures/NET_DVR_ACS_EVENT_DETAIL.html)

门禁主机报警事件细节结构体。

## 语法

```c
struct{
  DWORD                     dwSize;
  BYTE                      byCardNo[ACS_CARD_NO_LEN];
  BYTE                      byCardType;
  BYTE                      byWhiteListNo;
  BYTE                      byReportChannel;
  BYTE                      byCardReaderKind;
  DWORD                     dwCardReaderNo;
  DWORD                     dwDoorNo;
  DWORD                     dwVerifyNo;
  DWORD                     dwAlarmInNo; 
  DWORD                     dwAlarmOutNo; 
  DWORD                     dwCaseSensorNo; 
  DWORD                     dwRs485No; 
  DWORD                     dwMultiCardGroupNo; 
  WORD                      wAccessChannel; 
  BYTE                      byDeviceNo;
  BYTE                      byDistractControlNo;
  DWORD                     dwEmployeeNo; 
  WORD                      wLocalControllerID; 
  BYTE                      byInternetAccess;
  BYTE                      byType;
  BYTE                      byMACAddr[MACADDR_LEN];
  BYTE                      bySwipeCardType;
  BYTE                      byRes2;
  DWORD                     dwSerialNo;
  BYTE                      byChannelControllerID;
  BYTE                      byChannelControllerLampID;
  BYTE                      byChannelControllerIRAdaptorID;
  BYTE                      byChannelControllerIREmitterID;
  BYTE                      byRes[108];
}NET_DVR_ACS_EVENT_DETAIL,*LPNET_DVR_ACS_EVENT_DETAIL;
```

## Members

- `dwSize`：结构体大小
- `byCardNo`：卡号（mac地址），为0无效
- `byCardType`：卡类型，1-普通卡，2-残疾人卡，3-黑名单卡，4-巡更卡，5-胁迫卡，6-超级卡，7-来宾卡，8-解除卡，为0无效
- `byWhiteListNo`：白名单单号,1-8，为0无效
- `byReportChannel`：报告上传通道，1-布防上传，2-中心组1上传，3-中心组2上传，为0无效
- `byCardReaderKind`：读卡器属于哪一类，0-无效，1-IC读卡器，2-身份证读卡器，3-二维码读卡器,4-指纹头
- `dwCardReaderNo`：读卡器编号，为0无效
- `dwDoorNo`：门编号（楼层编号），为0无效
- `dwVerifyNo`：多重卡认证序号，为0无效
- `dwAlarmInNo`：报警输入号，为0无效
- `dwAlarmOutNo`：报警输出号，为0无效
- `dwCaseSensorNo`：事件触发器编号
- `dwRs485No`：RS485通道号，为0无效
- `dwMultiCardGroupNo`：群组编号
- `wAccessChannel`：人员通道号
- `byDistractControlNo`：分控器编号，为0无效
- `dwEmployeeNo`：工号，为0无效
- `dwEmployeeNo`：工号，为0无效
- `wLocalControllerID`：就地控制器编号，0-门禁主机，1-64代表就地控制器
- `wLocalControllerID`：就地控制器编号，0-门禁主机，1-64代表就地控制器
- `byInternetAccess`：网口ID：（1-上行网口1,2-上行网口2,3-下行网口1）
- `byType`：防区类型，0:即时防区,1-24小时防区,2-延时防区 ,3-内部防区，4-钥匙防区 5-火警防区 6-周界防区 7-24小时无声防区  8-24小时辅助防区，9-24小时震动防区,10-门禁紧急开门防区，11-门禁紧急关门防区 0xff-无
- `byMACAddr`：物理地址，为0无效
- `bySwipeCardType`：刷卡类型，0-无效，1-二维码
- `byRes`：保留，置为0
- `dwSerialNo`：事件流水号，为0无效
- `byChannelControllerID`：通道控制器ID，为0无效，1-主通道控制器，2-从通道控制器
- `byChannelControllerLampID`：通道控制器灯板ID，为0无效（有效范围1-255）
- `byChannelControllerIRAdaptorID`：通道控制器红外转接板ID，为0无效（有效范围1-255）
- `byChannelControllerIREmitterID`：通道控制器红外对射ID，为0无效（有效范围1-255）
- `byRes`：保留，置为0

## Remarks

对应的能力集：为ACS_ABILITY能力集的节点；详细信息为GET /ISAPI/AccessControl/GetAcsEvent/capabilities。

## See Also

NET_DVR_StartRemoteConfig   NET_DVR_StopRemoteConfig

## 相关链接

- [NET_DVR_StartRemoteConfig](../definitions/NET_DVR_StartRemoteConfig_ACS_Event.md)
- [NET_DVR_StopRemoteConfig](../definitions/NET_DVR_StopRemoteConfig_ACS_Event.md)
