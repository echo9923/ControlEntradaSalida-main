# NET_DVR_SETUPALARM_PARAM

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_SETUPALARM_PARAM.html](https://open.hikvision.com/hardware/structures/NET_DVR_SETUPALARM_PARAM.html)

报警布防参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     byLevel;
  BYTE     byAlarmInfoType;
  BYTE     byRetAlarmTypeV40;
  BYTE     byRetDevInfoVersion;
  BYTE     byRetVQDAlarmType;
  BYTE     byFaceAlarmDetection;
  BYTE     bySupport;
  BYTE     byBrokenNetHttp;
  WORD     wTaskNo;
  BYTE     byDeployType;
  BYTE     byRes1[3];
  BYTE     byAlarmTypeURL;
  BYTE     byCustomCtrl;
}NET_DVR_SETUPALARM_PARAM, *LPNET_DVR_SETUPALARM_PARAM;
```

## Members

- `dwSize`：结构体大小
- `byLevel`：布防优先级：0- 一等级（高），1- 二等级（中），2- 三等级（低）
- `byAlarmInfoType`：智能交通报警信息上传类型：0- 老报警信息（NET_DVR_PLATE_RESULT），1- 新报警信息(NET_ITS_PLATE_RESULT)
- `byRetAlarmTypeV40`：0- 移动侦测、视频丢失、遮挡、IO信号量等报警信息以普通方式上传（报警类型：COMM_ALARM_V30，报警信息结构体：NET_DVR_ALARMINFO_V30），1- 报警信息以数据可变长方式上传（报警类型：COMM_ALARM_V40，报警信息结构体：NET_DVR_ALARMINFO_V40，设备若不支持则仍以普通方式上传）
- `byRetDevInfoVersion`：CVR上传报警信息类型(仅对接CVR时有效)：0- COMM_ALARM_DEVICE（对应报警信息结构体：NET_DVR_ALARMINFO_DEV），1- COMM_ALARM_DEVICE_V40（对应报警信息结构体：NET_DVR_ALARMINFO_DEV_V40）
- `byRetVQDAlarmType`：VQD报警上传类型(仅对接VQD诊断功能的设备有效)：0- COMM_ALARM_VQD（对应报警信息结构体：NET_DVR_VQD_DIAGNOSE_INFO），1- COMM_ALARM_VQD_EX（对应报警信息结构体：NET_DVR_VQD_ALARM，包含前端设备信息和抓拍图片）
- `byFaceAlarmDetection`：人脸报警信息类型：1- 人脸侦测报警(报警类型：COMM_ALARM_FACE_DETECTION，NET_DVR_FACE_DETECTION)，0- 人脸抓拍报警(报警类型：COMM_UPLOAD_FACESNAP_RESULT，NET_VCA_FACESNAP_RESULT)
- `bySupport`：按位表示，每一位取值表示不同的能力

bit0- 表示二级布防是否上传图片，值：0-上传，1-不上传

Bit1- 表示是否启用断网续传数据确认机制，值：0-不开启，1-开启
- `byBrokenNetHttp`：断网续传类型（设备目前只支持一个断网续传布防连接），按位表示，值：0- 不续传，1- 续传

bit0- 车牌检测（IPC）

bit1- 客流统计（IPC）

bit2- 热度图统计（IPC）

bit3- 人脸抓拍（IPC）

bit4- 人脸对比（IPC）

bit5- JSON报警透传（IPC）

例如：byBrokenNetHttp&0x1==0 表示车牌检测结果不续传
- `wTaskNo`：任务处理号
- `byDeployType`：布防类型：0-客户端布防，1-实时布防
- `byRes1`：保留，置为0
- `byAlarmTypeURL`：报警图片数据类型，按位表示：

bit0- 人脸抓拍(报警类型为COMM_UPLOAD_FACESNAP_RESULT)中图片数据上传类型：0- 二进制传输，1- URL传输

bit1- EVENT_JSON(报警类型为COMM_VCA_ALARM)中图片数据上传类型：0- 二进制传输，1- URL传输

bit2- 人脸比对(报警类型为COMM_SNAP_MATCH_ALARM)中图片数据上传类型：0- 二进制传输，1- URL传输

如果设备同时支持URL和二进制传输方式，可以布防的时候通过该参数指定上传的数据格式（二进制或者URL），选择URL传输方式时需要设备配置和启用云存储，否则仍默认以二进制数据格式传输。如果设备只支持URL方式，该参数赋值无效。
- `byCustomCtrl`：按位表示，bit0表示是否上传副驾驶人脸子图: 0- 不上传，1- 上传

## Remarks

byLevel和byAlarmInfoType针对智能交通设备（抓拍机）：一级布防最大连接数为1个，二级最大连接数为3个，三级最大连接数为5个，设备支持一级、二级、三级布防同时进行，一级布防优先上传信息；byAlarmInfoType是否支持新报警信息可从注册返回的能力获知，详见NET_DVR_DEVICEINFO_V30结构中bySupport1（表示是否支持车牌新报警信息），如果注册返回能力不支持，设备仅支持老报警信息上传。

wTaskNo针对车辆二次检测设备，用于区分不同布防链接，布防的任务处理号和任务提交的任务处理号、识别结果上传的任务处理号都是一一对应的。例如：布防链接中wTaskNo==1，任务A中wTaskNo==1，结果信息回调wTaskNo==1（该信息回调只在布防中wTaskNo == 1的链接中回调）。如果两个布防连接中wTaskNo的值相同，将返回布防链接错误。

设备是否支持断网续传数据确认机制，可以通过设备能力集进行判断，对应设备软硬件能力集(BasicCapability)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：DEVICE_SOFTHARDWARE_ABILITY，节点：。

客户端布防：门禁原有的布防方式，一般只支持1路布防，支持离线事件上传；实时布防：新增的布防方式，主要用于其他设备对门禁设备的布防，最多支持4路实时布防，不支持离线事件上传。

## See Also

NET_DVR_SetupAlarmChan_V41

## 相关链接

- [NET_DVR_PLATE_RESULT](NET_DVR_PLATE_RESULT.md)
- [NET_ITS_PLATE_RESULT](NET_ITS_PLATE_RESULT.md)
- [NET_DVR_ALARMINFO_V30](../structures/NET_DVR_ALARMINFO_V30.md)
- [NET_DVR_ALARMINFO_V40](../structures/NET_DVR_ALARMINFO_V40.md)
- [NET_DVR_ALARMINFO_DEV](../structures/NET_DVR_ALARMINFO_DEV.md)
- [NET_DVR_ALARMINFO_DEV_V40](../structures/NET_DVR_ALARMINFO_DEV_V40.md)
- [NET_DVR_VQD_DIAGNOSE_INFO](NET_DVR_VQD_DIAGNOSE_INFO.md)
- [NET_DVR_VQD_ALARM](NET_DVR_VQD_ALARM.md)
- [NET_DVR_FACE_DETECTION](NET_DVR_FACE_DETECTION.md)
- [NET_VCA_FACESNAP_RESULT](NET_VCA_FACESNAP_RESULT.md)
- [NET_DVR_DEVICEINFO_V30](NET_DVR_DEVICEINFO_V30.md)
- [BasicCapability](../XMLs/DEVICE_SOFTHARDWARE_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility.md)
- [NET_DVR_SetupAlarmChan_V41](../definitions/NET_DVR_SetupAlarmChan_V41.md)
