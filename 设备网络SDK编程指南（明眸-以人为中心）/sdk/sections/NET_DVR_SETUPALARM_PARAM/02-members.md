# Members


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
