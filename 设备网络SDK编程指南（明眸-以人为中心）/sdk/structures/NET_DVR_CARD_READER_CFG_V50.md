# NET_DVR_CARD_READER_CFG_V50

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CARD_READER_CFG_V50.html](https://open.hikvision.com/hardware/structures/NET_DVR_CARD_READER_CFG_V50.html)

读卡器参数配置结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byEnable;
  BYTE     byCardReaderType;
  BYTE     byOkLedPolarity;
  BYTE     byErrorLedPolarity;
  BYTE     byBuzzerPolarity;
  BYTE     bySwipeInterval;
  BYTE     byPressTimeout;
  BYTE     byEnableFailAlarm;
  BYTE     byMaxReadCardFailNum;
  BYTE     byEnableTamperCheck;
  BYTE     byOfflineCheckTime;
  BYTE     byFingerPrintCheckLevel;
  BYTE     byUseLocalController;
  BYTE     byRes1;
  WORD     wLocalControllerID;
  WORD     wLocalControllerReaderID;
  WORD     wCardReaderChannel;
  BYTE     byFingerPrintImageQuality;
  BYTE     byFingerPrintContrastTimeOut;
  BYTE     byFingerPrintRecogizeInterval;
  BYTE     byFingerPrintMatchFastMode;
  BYTE     byFingerPrintModuleSensitive;
  BYTE     byFingerPrintModuleLightCondition;
  BYTE     byFaceMatchThresholdN;
  BYTE     byFaceQuality;
  BYTE     byFaceRecogizeTimeOut;
  BYTE     byFaceRecogizeInterval;
  WORD     wCardReaderFunction;
  BYTE     byCardReaderDescription[CARD_READER_DESCRIPTION];
  WORD     wFaceImageSensitometry;
  BYTE     byLivingBodyDetect;
  BYTE     byFaceMatchThreshold1;
  WORD     wBuzzerTime;
  BYTE     byFaceMatch1SecurityLevel;
  BYTE     byFaceMatchNSecurityLevel;
  BYTE     byEnvirMode;//人脸识别环境模式，0-无效，1-室内，2-其他；
  BYTE     byLiveDetLevelSet;//活体检测阈值等级设置，0-无效，1-低，2-中，3-高；
  BYTE     byLiveDetAntiAttackCntLimit;//活体检测防攻击次数， 0-无效，1-255次（客户端、设备统一次数限制，根据能力级限制）；
  BYTE     byEnableLiveDetAntiAttack;//活体检测防攻击使能，0-无效，1-不启用，2-启用
  DWORD    dwFingerPrintCapacity;//只读，指纹容量
  DWORD    dwFingerPrintNum;//只读，已存在指纹数量
  BYTE     byEnableFingerPrintNum;//只读，指纹容量使能：0-不使能，1-使能（只有当该字段为1-使能时，dwFingerPrintCapacity和dwFingerPrintNum才有效）
  BYTE     byRes[239];
}NET_DVR_CARD_READER_CFG_V50,*LPNET_DVR_CARD_READER_CFG_V50;
```

## Members

- `dwSize`：结构体大小
- `byEnable`：是否使能：0- 不启用，1- 启用
- `byCardReaderType`：读卡器类型：1- DS-K110XM/MK/C/CK，2- DS-K192AM/AMP，3- DS-K192BM/BMP，4- DS-K182AM/AMP，5- DS-K182BM/BMP，6- DS-K182AMF/ACF，7- 韦根或485不在线，8- DS-K1101M/MK，9- DS-K1101C/CK，10- DS-K1102M/MK/M-A，11- DS-K1102C/CK，12- DS-K1103M/MK，13- DS-K1103C/CK，14- DS-K1104M/MK，15- DS-K1104C/CK，16- DS-K1102S/SK/S-A，17- DS-K1102G/GK，18- DS-K1100S-B，19- DS-K1102EM/EMK，20- DS-K1102E/EK，21- DS-K1200EF，22- DS-K1200MF，23- DS-K1200CF，24- DS-K1300EF，25- DS-K1300MF，26- DS-K1300CF，27- DS-K1105E，28- DS-K1105M，29- DS-K1105C，30- DS-K182AMF，31- DS-K196AMF，32- DS-K194AMP，33- DS-K1T200EF/EF-C/MF-MF-C/CF/CF-C，34- DS-K1T300EF/EF-C/MF-MF-C/CF/CF-C
- `byOkLedPolarity`：OK LED极性：0- 阴极，1- 阳极
- `byErrorLedPolarity`：Error LED极性：0- 阴极，1- 阳极
- `byBuzzerPolarity`：蜂鸣器极性：0- 阴极，1- 阳极
- `bySwipeInterval`：重复刷卡间隔时间，单位：秒
- `byPressTimeout`：按键超时时间，单位：秒，取值范围：1~255
- `byEnableFailAlarm`：是否启用读卡失败超次报警：0- 不启用，1- 启用
- `byMaxReadCardFailNum`：最大读卡失败次数，取值范围：1~10
- `byEnableTamperCheck`：是否启用防拆检测：0- 不启用，1- 启用
- `byOfflineCheckTime`：掉线检测时间，单位：秒，取值范围：0~255
- `byFingerPrintCheckLevel`：指纹识别等级：1- 1/10误认率，2- 1/100误认率，3- 1/1000误认率，4- 1/10000误认率，5- 1/100000误认率，6- 1/1000000误认率，7- 1/10000000误认率，8- 1/100000000误认率，9- 3/100误认率，10- 3/1000误认率，11- 3/10000误认率，12- 3/100000误认率，13- 3/1000000误认率，14- 3/10000000误认率，15- 3/100000000误认率，16- Automatic Normal，17- Automatic Secure，18- Automatic More Secure
- `byUseLocalController`：只读，是否连接在就地控制器上，0-否，1-是
- `byRes1`：保留，置为0
- `wLocalControllerID`：只读，就地控制器序号，byUseLocalController=1时有效，0代表未注册，序号取值范围：1~255
- `wLocalControllerReaderID`：只读，就地控制器的读卡器ID，byUseLocalController=1时有效，0代表未注册
- `wCardReaderChannel`：只读，读卡器通信通道号，byUseLocalController=1时有效，取值：0- 韦根或离线，1- RS485A，2- RS485B
- `byFingerPrintImageQuality`：指纹图像质量：0- 无效，1- 低质量(V1)，2- 中等质量(V1)，3- 高质量(V1)，4- 最高质量(V1)，5- 低质量(V2)，6- 中等质量(V2)，7- 高质量(V2)，8- 最高质量(V2)
- `byFingerPrintContrastTimeOut`：指纹对比超时时间，0表示无效，1~20分别表示1s~20s，0xff表示无限大
- `byFingerPrintRecogizeInterval`：指纹连续识别间隔，0表示无效，1~10分别表示1s~10s，0xff表示无延迟
- `byFingerPrintMatchFastMode`：指纹匹配快速模式，0表示无效，1~5分别表示快速模式1~快速模式5，0xff表示自动
- `byFingerPrintModuleSensitive`：指纹模组灵敏度，0表示无效，1~8分别表示灵敏度级别1~灵敏度级别8
- `byFingerPrintModuleLightCondition`：指纹模组光线条件：0- 无效，1- 室外，2- 室内
- `byFaceMatchThresholdN`：人脸比对阀值，取值范围：0~100
- `byFaceQuality`：人脸质量，取值范围：0~100
- `byFaceRecogizeTimeOut`：人脸识别超时时间，1~20分别表示1s~20s，0xff表示无限大
- `byFaceRecogizeInterval`：人脸连续识别间隔，0表示无效，1~10分别表示1s~10s，0xff表示无延迟
- `wCardReaderFunction`：只读，读卡器种类，按位表示：第1位- 指纹，第二位- 人脸，第三位- 指静脉

值：0- 不是，1- 是
- `byCardReaderDescription`：读卡器描述
- `wFaceImageSensitometry`：只读，人脸图像曝光度，取值范围：0~65535
- `byLivingBodyDetect`：真人检测：0- 无效，1- 不启用，2- 启用
- `byFaceMatchThreshold1`：人脸1:1匹配阀值，取值范围：0~100
- `wBuzzerTime`：蜂鸣时间，范围0s-5999s（0-代表长鸣）
- `byFaceMatch1SecurityLevel`：人脸1:1识别安全等级，0-无效，1-一般，2-较强，3-极强
- `byFaceMatchNSecurityLevel`：人脸1:N识别安全等级，0-无效，1-一般，2-较强，3-极强
- `byEnvirMode`：人脸识别环境模式，0-无效，1-室内，2-其他
- `byLiveDetLevelSet`：活体检测阈值等级设置，0-无效，1-低，2-中，3-高
- `byLiveDetAntiAttackCntLimit`：活体检测防攻击次数， 0-无效，1-255次（客户端、设备统一次数限制，根据能力级限制）
- `byEnableLiveDetAntiAttack`：活体检测防攻击使能，0-无效，1-不启用，2-启用
- `dwFingerPrintCapacity`：只读，指纹容量
- `dwFingerPrintNum`：只读，已存在指纹数量
- `byEnableFingerPrintNum`：只读，指纹容量使能：0-不使能，1-使能（只有当该字段为1-使能时，dwFingerPrintCapacity和dwFingerPrintNum才有效）
- `byRes`：保留，置为0

## Remarks

设备是否支持读卡器参数配置或者支持的参数能力，可以通过设备能力集进行判断，对应门禁主机能力集(AcsAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：ACS_ABILITY，节点：。

## See Also

NET_DVR_GetDVRConfig   NET_DVR_SetDVRConfig

## 相关链接

- [AcsAbility](../XMLs/ACS_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility_ACS.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_ACS.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_ACS.md)
