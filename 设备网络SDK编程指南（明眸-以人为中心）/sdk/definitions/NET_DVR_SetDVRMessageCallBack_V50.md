# NET_DVR_SetDVRMessageCallBack_V50

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_SetDVRMessageCallBack_V50.html](https://open.hikvision.com/hardware/definitions/NET_DVR_SetDVRMessageCallBack_V50.html)

注册报警信息回调函数。

## Parameters

- `iIndex`：[in] 回调函数索引，取值范围：[0,15]
- `fMessageCallBack`：[in] 回调函数
- `pUser`：[in] 用户数据

## Callback Function

```text
typedef void (CALLBACK *MSGCallBack)(
  LONG               lCommand,
  NET_DVR_ALARMER    *pAlarmer,
  char               *pAlarmInfo,
  DWORD              dwBufLen,
  void               *pUser
);
```

typedef void (CALLBACK *MSGCallBack)(
  LONG               lCommand,
  NET_DVR_ALARMER    *pAlarmer,
  char               *pAlarmInfo,
  DWORD              dwBufLen,
  void               *pUser
);

## Callback Function Parameters

- `lCommand`：[out] 上传的消息类型，不同的报警信息对应不同的类型，通过类型区分是什么报警信息，详见“Remarks”中列表
- `pAlarmer`：[out] 报警设备信息，包括设备序列号、IP地址、登录IUserID句柄等
- `pAlarmInfo`：[out] 报警信息，通过lCommand值判断pAlarmer对应的结构体，详见“Remarks”中列表
- `dwBufLen`：[out] 报警信息缓存大小
- `pUser`：[out] 用户数据

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

V30、V31的接口设置报警回调函数是全局唯一的，注册多次会覆盖之前的，只有最后设置的回调函数有效，所有设备报警信息都是在同一个回调函数中返回的，通过报警设备信息（pAlarmInfo）区分是哪台设备；通过V50接口设置报警回调函数，支持设置多路不同的回调函数，最大支持16路，通过索引进行区分，所有的设备报警信息会同时在设置的各个回调函数里面返回，返回相同的数据，同样需要通过报警设备信息（pAlarmInfo）区分是哪台设备。

该接口中回调函数的第一个参数（lCommand）和第三个参数（pAlarmInfo）是密切关联的，其关系见下表：

消息类型（lCommand）

宏定义值

上传内容

pAlarmInfo对应的结构体

智能报警

COMM_ALARM_RULE

0x1102

行为分析信息

NET_VCA_RULE_ALARM

COMM_ALARM_PDC

0x1103

客流量统计报警信息

NET_DVR_PDC_ALRAM_INFO

COMM_RULE_INFO_UPLOAD

0x1107

事件数据信息

NET_DVR_RULE_INFO_ALARM

COMM_ALARM_FACE

0x1106

人脸检测识别报警信息

NET_DVR_FACEDETECT_ALARM

COMM_UPLOAD_FACESNAP_RESULT

0x1112

人脸抓拍结果信息

NET_VCA_FACESNAP_RESULT

COMM_FACECAPTURE_STATISTICS_RESULT

0x112a

人脸抓拍人员统计信息

NET_DVR_FACECAPTURE_STATISTICS_RESULT

COMM_SNAP_MATCH_ALARM

0x2902

人脸黑名单比对结果信息

NET_VCA_FACESNAP_MATCH_ALARM

COMM_ALARM_FACE_DETECTION

0x4010

人脸侦测报警信息

NET_DVR_FACE_DETECTION

COMM_ALARM_TARGET_LEFT_REGION

0x4011

教师离开讲台报警

NET_DVR_TARGET_LEFT_REGION_ALARM

COMM_PEOPLE_DETECTION_UPLOAD

0x4014

人员侦测信息

NET_DVR_PEOPLE_DETECTION_RESULT

COMM_VCA_ALARM

0x4993

智能检测通用报警(Json数据结构)

人体目标识别报警Json数据

人员密度报警Json数据

人员排队时长检测报警JSON数据

人员排队人数检测报警JSON数据

安全帽检测报警JSON数据

录制状态报警信息上传JSON数据

资源上传云存储状态报警信息上传JSON数据

COMM_SIGN_ABNORMAL_ALARM

0x6120

体征异常报警(Json数据结构)

EVENT_JSON

COMM_HFPD_ALARM

0x6121

高频人员检测报警(Json数据结构)

EVENT_JSON

COMM_ALARM_VQD_EX

0x1116

VQD报警信息

NET_DVR_VQD_ALARM

COMM_ALARM_VQD

0x6000

VQD诊断报警信息

NET_DVR_VQD_DIAGNOSE_INFO

COMM_SCENECHANGE_DETECTION_UPLOAD

0x1130

场景变更报警信息

NET_DVR_SCENECHANGE_DETECTION_RESULT

COMM_CROSSLINE_ALARM

0x1131

压线报警信息

NET_DVR_CROSSLINE_ALARM

COMM_ALARM_AUDIOEXCEPTION

0x1150

声音报警信息

NET_DVR_AUDIOEXCEPTION_ALARM

COMM_ALARM_DEFOCUS

0x1151

虚焦报警信息

NET_DVR_DEFOCUS_ALARM

COMM_SWITCH_LAMP_ALARM

0x6002

开关灯检测报警信息

NET_DVR_SWITCH_LAMP_ALARM

COMM_UPLOAD_HEATMAP_RESULT

0x4008

热度图报警信息

NET_DVR_HEATMAP_RESULT

COMM_FIREDETECTION_ALARM

0x4991

火点检测报警信息

NET_DVR_FIREDETECTION_ALARM

COMM_THERMOMETRY_DIFF_ALARM

0x5211

温差报警信息

NET_DVR_THERMOMETRY_DIFF_ALARM

COMM_THERMOMETRY_ALARM

0x5212

温度报警信息

NET_DVR_THERMOMETRY_ALARM

COMM_ALARM_SHIPSDETECTION

0x4521

船只检测报警信息

NET_DVR_SHIPSDETECTION_ALARM

智能交通

COMM_ALARM_AID

0x1110

交通事件报警信息

NET_DVR_AID_ALARM

COMM_ALARM_TPS

0x1111

交通参数统计报警信息

NET_DVR_TPS_ALARM

COMM_ALARM_TFS

0x1113

交通取证报警信息

NET_DVR_TFS_ALARM

COMM_ALARM_TPS_V41

0x1114

交通参数统计报警信息(扩展)

NET_DVR_TPS_ALARM_V41

COMM_ALARM_AID_V41

0x1115

交通事件报警信息扩展

NET_DVR_AID_ALARM_V41

COMM_UPLOAD_PLATE_RESULT

0x2800

交通抓拍结果

NET_DVR_PLATE_RESULT

COMM_ITS_PLATE_RESULT

0x3050

交通抓拍结果(新报警信息)

NET_ITS_PLATE_RESULT

COMM_ITS_TRAFFIC_COLLECT

0x3051

交通统计数据上传

NET_ITS_TRAFFIC_COLLECT

COMM_ITS_BLACKLIST_ALARM

0x3057

车辆黑名单报警上传

NET_ITS_ECT_BLACKLIST

COMM_VEHICLE_CONTROL_LIST_DSALARM

0x3058

车辆黑白名单数据需要同步报警上传

NET_DVR_VEHICLE_CONTROL_LIST_DSALARM

COMM_VEHICLE_CONTROL_ALARM 

0x3059

黑白名单车辆报警上传

NET_DVR_VEHICLE_CONTROL_ALARM

COMM_FIRE_ALARM

0x3060

消防报警上传

NET_DVR_FIRE_ALARM

COMM_VEHICLE_RECOG_RESULT

0x3062

车辆二次识别结果上传

NET_DVR_VEHICLE_RECOG_RESULT

COMM_ALARM_SENSORINFO_UPLOAD

0x3077

传感器上传信息

NET_DVR_SENSOR_INFO_UPLOAD

COMM_ALARM_CAPTURE_UPLOAD

0x3078

抓拍图片上传

NET_DVR_CAPTURE_UPLOAD

COMM_ITS_RADARINFO

0x3079

雷达报警上传

NET_DVR_ALARM_RADARINFO

COMM_SIGNAL_LAMP_ABNORMAL

0x3080

信号灯异常检测上传

NET_DVR_SIGNALLAMP_DETCFG

COMM_ALARM_TPS_REAL_TIME

0x3081

TPS实时过车数据上传

NET_DVR_TPS_REAL_TIME_INFO

COMM_ALARM_TPS_STATISTICS

0x3082

TPS统计过车数据上传

NET_DVR_TPS_STATISTICS_INFO

COMM_ITS_ROAD_EXCEPTION

0x4500

路口设备异常报警信息

NET_ITS_ROADINFO

COMM_ITS_EXTERNAL_CONTROL_ALARM

0x4520

指示灯外控报警信息

NET_DVR_EXTERNAL_CONTROL_ALARM

COMM_ITS_GATE_FACE

0x3053

出入口人脸抓拍数据

NET_ITS_GATE_FACE

COMM_ITS_GATE_ALARMINFO

0x3061

出入口控制机数据

NET_DVR_GATE_ALARMINFO

COMM_GATE_CHARGEINFO_UPLOAD

0x3064

出入口付费信息

NET_DVR_GATE_CHARGEINFO

COMM_TME_VEHICLE_INDENTIFICATION

0x3065

出入口控制器TME车辆抓拍信息

NET_DVR_TME_VEHICLE_RESULT

COMM_GATE_CARDINFO_UPLOAD

0x3066

出入口卡片信息

NET_DVR_GATE_CARDINFO

报警主机

COMM_ALARM_ALARMHOST

0x1105

网络报警主机报警信息

NET_DVR_ALARMHOST_ALARMINFO

COMM_SENSOR_VALUE_UPLOAD

0x1120

模拟量数据实时信息

NET_DVR_SENSOR_ALARM

COMM_SENSOR_ALARM

0x1121

模拟量报警信息

NET_DVR_SENSOR_ALARM

COMM_SWITCH_ALARM

0x1122

开关量报警信息

NET_DVR_SWITCH_ALARM

COMM_ALARMHOST_EXCEPTION

0x1123

故障报警信息

NET_DVR_ALARMHOST_EXCEPTION_ALARM

COMM_ALARMHOST_SAFETYCABINSTATE

0x1125

防护舱状态信息

NET_DVR_ALARMHOST_SAFETYCABINSTATE

COMM_ALARMHOST_ALARMOUTSTATUS

0x1126

报警输出口或警号状态信息

NET_DVR_ALARMHOST_ALARMOUTSTATUS

COMM_ALARMHOST_CID_ALARM

0x1127

报警主机CID报告报警上传

NET_DVR_CID_ALARM

COMM_ALARMHOST_EXTERNAL_DEVICE_ALARM

0x1128

报警主机外接设备报警信息

NET_DVR_485_EXTERNAL_DEVICE_ALARMINFO

COMM_ALARMHOST_DATA_UPLOAD

0x1129

报警数据信息

NET_DVR_ALARMHOST_DATA_UPLOAD

COMM_ALARM_WIRELESS_INFO

0x122b

无线网络信息上传

NET_DVR_ALARMWIRELESSINFO

其他设备报警

COMM_ALARM

0x1100

移动侦测、视频丢失、遮挡、IO信号量等报警信息(V3.0以下版本支持的设备)

NET_DVR_ALARMINFO

COMM_ALARM_V30

0x4000

移动侦测、视频丢失、遮挡、IO信号量等报警信息(V3.0以上版本支持的设备)

NET_DVR_ALARMINFO_V30

COMM_ALARM_V40

0x4007

移动侦测、视频丢失、遮挡、IO信号量等报警信息，报警数据为可变长

NET_DVR_ALARMINFO_V40

COMM_IPCCFG

0x4001

混合型DVR、NVR等在IPC接入配置改变时的报警信息

NET_DVR_IPALARMINFO

COMM_IPCCFG_V31

0x4002

混合型DVR、NVR等在IPC接入配置改变时的报警信息（扩展）

NET_DVR_IPALARMINFO_V31

COMM_IPC_AUXALARM_RESULT

0x2820

PIR报警、无线报警、呼救报警信息

NET_IPC_AUXALARM_RESULT

COMM_ALARM_DEVICE

0x4004

CVR设备报警信息，由于通道值大于256而扩展

NET_DVR_ALARMINFO_DEV

COMM_ALARM_DEVICE_V40

0x4009

CVR设备报警信息扩展(增加报警信息子结构)

NET_DVR_ALARMINFO_DEV_V40

COMM_ALARM_CVR

0x4005

CVR外部报警信息

NET_DVR_CVR_ALARM

COMM_TRADEINFO

0x1500

ATM DVR交易信息

NET_DVR_TRADEINFO

COMM_ALARM_HOT_SPARE

0x4006

热备异常报警（N+1模式异常报警）信息

NET_DVR_ALARM_HOT_SPARE

COMM_ALARM_BUTTON_DOWN_EXCEPTION

0x1152

按钮按下报警信息(IP可视对讲主机)

NET_BUTTON_DOWN_EXCEPTION_ALARM

COMM_SCREEN_ALARM

0x5000

多屏控制器上传的报警信息

NET_DVR_SCREENALARMCFG

COMM_ALARM_LCD

0x5011

LCD屏幕报警信息

NET_DVR_LCD_ALARM

COMM_UPLOAD_VIDEO_INTERCOM_EVENT

0x1132

可视对讲事件记录信息

NET_DVR_VIDEO_INTERCOM_EVENT

COMM_ALARM_VIDEO_INTERCOM

0x1133

可视对讲报警信息

NET_DVR_VIDEO_INTERCOM_ALARM

COMM_ALARM_DEC_VCA

0x5010

解码器智能解码报警信息

NET_DVR_DEC_VCA_ALARM

COMM_GISINFO_UPLOAD

0x4012

GIS信息

NET_DVR_GIS_UPLOADINFO

COMM_VANDALPROOF_ALARM

0x4013

防破坏报警信息

NET_DVR_VANDALPROOF_ALARM

COMM_ALARM_STORAGE_DETECTION

0x4015

存储智能检测报警信息

NET_DVR_STORAGE_DETECTION_ALARM

COMM_CONFERENCE_CALL_ALARM

0x5012

会议终端会议呼叫报警信息

NET_DVR_CONFERENCE_CALL_ALARM

COMM_ALARM_ALARMGPS

0x1202

GPS报警信息

NET_DVR_GPSALARMINFO

COMM_ALARM_SWITCH_CONVERT

0x5004

交换机报警信息

NET_DVR_SWITCH_CONVERT_ALARM

COMM_INQUEST_ALARM

0x6005

审讯主机报警信息

NET_DVR_INQUEST_ALARM

COMM_PANORAMIC_LINKAGE_ALARM

0x5213

鹰眼全景联动到位事件信息

NET_DVR_PANORAMIC_LINKAGE

COMM_ISAPI_ALARM

0x6009

ISAPI协议报警信息

NET_DVR_ALARM_ISAPI_INFO

COMM_CLUSTER_ALARM

0x6020

集群报警信息（集群异动（扩容，缩容等）时布防CS失败异常，收到此异常，需要重新布防集群设备，否则会有CS的报警丢失。普通设备不会回调该异常，只针对集群。）

EventNotificationAlert JSON Block

## Example

```text
#include 
#include 
#include "Windows.h"
#include "HCNetSDK.h"
using namespace std;

int iNum=0;
void CALLBACK MessageCallbackNo1(LONG lCommand, NET_DVR_ALARMER *pAlarmer, char *pAlarmInfo, DWORD dwBufLen, void* pUser)
{
    int i=0;
    char filename[100];
    FILE *fSnapPic=NULL;
    FILE *fSnapPicPlate=NULL;

    //以下代码仅供参考，实际应用中不建议在该回调函数中直接处理数据保存文件
    //例如可以使用消息的方式(PostMessage)在消息响应函数里进行处理

    switch(lCommand) 
    {       
        case COMM_ALARM:
        {
            NET_DVR_ALARMINFO struAlarmInfo;
            memcpy(&struAlarmInfo, pAlarmInfo, sizeof(NET_DVR_ALARMINFO));
            switch (struAlarmInfo.dwAlarmType)
            {
                case 3: //移动侦测报警
                    for (i=0; i<16; i++)   //#define MAX_CHANNUM   16  //最大通道数
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            printf("发生移动侦测报警的通道号 %d\n", i+1);
                        }
                    }       
                    break;
                default:
                    break;
            }
            break;
        }
        case COMM_UPLOAD_PLATE_RESULT:
        {
            NET_DVR_PLATE_RESULT struPlateResult={0};
            memcpy(&struPlateResult, pAlarmInfo, sizeof(struPlateResult));
            printf("车牌号: %s\n", struPlateResult.struPlateInfo.sLicense);//车牌号

            switch(struPlateResult.struPlateInfo.byColor)//车牌颜色
            {
            case VCA_BLUE_PLATE:
                printf("车辆颜色: 蓝色\n");
                break;
            case VCA_YELLOW_PLATE:
                printf("车辆颜色: 黄色\n");
                break;
            case VCA_WHITE_PLATE:
                printf("车辆颜色: 白色\n");
                break;
            case VCA_BLACK_PLATE:
                printf("车辆颜色: 黑色\n");
                break;	
            default:
                break;
            } 
            //场景图
            if (struPlateResult.dwPicLen != 0 && struPlateResult.byResultType == 1 ) 
            {
                sprintf(filename,"testpic_%d.jpg",iNum);
                fSnapPic=fopen(filename,"wb");
                fwrite(struPlateResult.pBuffer1,struPlateResult.dwPicLen,1,fSnapPic);
                iNum++;
                fclose(fSnapPic);
            }
            //车牌图
            if (struPlateResult.dwPicPlateLen != 0 && struPlateResult.byResultType == 1) 
            {
                sprintf(filename,"testPicPlate_%d.jpg",iNum);
                fSnapPicPlate=fopen(filename,"wb");
                fwrite(struPlateResult.pBuffer1,struPlateResult.dwPicLen,1,fSnapPicPlate);
                iNum++;
                fclose(fSnapPicPlate);
            }	
            //其他信息处理......
            break;
        }
        case COMM_ITS_PLATE_RESULT:
        {
            NET_ITS_PLATE_RESULT struITSPlateResult={0};
            memcpy(&struITSPlateResult, pAlarmInfo, sizeof(struITSPlateResult));

            for (i=0;i<struITSPlateResult.dwPicNum;i++)
            {
                printf("车牌号: %s\n", struITSPlateResult.struPlateInfo.sLicense);//车牌号
                switch(struITSPlateResult.struPlateInfo.byColor)//车牌颜色
                {
                case VCA_BLUE_PLATE:
                    printf("车辆颜色: 蓝色\n");
                    break;
                case VCA_YELLOW_PLATE:
                    printf("车辆颜色: 黄色\n");
                    break;
                case VCA_WHITE_PLATE:
                    printf("车辆颜色: 白色\n");
                    break;
                case VCA_BLACK_PLATE:
                    printf("车辆颜色: 黑色\n");
                    break;
                default:
                    break;
                }
                //保存场景图
                if ((struITSPlateResult.struPicInfo[i].dwDataLen != 0)&&(struITSPlateResult.struPicInfo[i].byType== 1)||(struITSPlateResult.struPicInfo[i].byType == 2))
                {
                    sprintf(filename,"testITSpic%d_%d.jpg",iNum,i);
                    fSnapPic=fopen(filename,"wb");
                    fwrite(struITSPlateResult.struPicInfo[i].pBuffer, struITSPlateResult.struPicInfo[i].dwDataLen,1,fSnapPic);
                    iNum++;
                    fclose(fSnapPic);
                }
                //车牌小图片
                if ((struITSPlateResult.struPicInfo[i].dwDataLen != 0)&&(struITSPlateResult.struPicInfo[i].byType == 0))
                {
                    sprintf(filename,"testPicPlate%d_%d.jpg",iNum,i);
                    fSnapPicPlate=fopen(filename,"wb");
                    fwrite(struITSPlateResult.struPicInfo[i].pBuffer, struITSPlateResult.struPicInfo[i].dwDataLen, 1, \ fSnapPicPlate);
                    iNum++;
                    fclose(fSnapPicPlate);
                }
                //其他信息处理......
            }
            break;
        }
    default:
        break;
    }
}

void CALLBACK MessageCallbackNo2(LONG lCommand, NET_DVR_ALARMER *pAlarmer, char *pAlarmInfo, DWORD dwBufLen, void* pUser)
{
    int i=0;
    char filename[100];
    FILE *fSnapPic=NULL;
    FILE *fSnapPicPlate=NULL;

    //以下代码仅供参考，实际应用中不建议在该回调函数中直接处理数据保存文件
    //例如可以使用消息的方式(PostMessage)在消息响应函数里进行处理

    switch(lCommand) 
    {       
        case COMM_ALARM:
        {
            NET_DVR_ALARMINFO struAlarmInfo;
            memcpy(&struAlarmInfo, pAlarmInfo, sizeof(NET_DVR_ALARMINFO));
            switch (struAlarmInfo.dwAlarmType)
            {
                case 3: //移动侦测报警
                    for (i=0; i<16; i++)   //#define MAX_CHANNUM   16  //最大通道数
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            printf("发生移动侦测报警的通道号 %d\n", i+1);
                        }
                    }       
                    break;
                default:
                    break;
            }
            break;
        }
        case COMM_UPLOAD_PLATE_RESULT:
        {
            NET_DVR_PLATE_RESULT struPlateResult={0};
            memcpy(&struPlateResult, pAlarmInfo, sizeof(struPlateResult));
            printf("车牌号: %s\n", struPlateResult.struPlateInfo.sLicense);//车牌号

            switch(struPlateResult.struPlateInfo.byColor)//车牌颜色
            {
            case VCA_BLUE_PLATE:
                printf("车辆颜色: 蓝色\n");
                break;
            case VCA_YELLOW_PLATE:
                printf("车辆颜色: 黄色\n");
                break;
            case VCA_WHITE_PLATE:
                printf("车辆颜色: 白色\n");
                break;
            case VCA_BLACK_PLATE:
                printf("车辆颜色: 黑色\n");
                break;	
            default:
                break;
            } 
            //场景图
            if (struPlateResult.dwPicLen != 0 && struPlateResult.byResultType == 1 ) 
            {
                sprintf(filename,"testpic_%d.jpg",iNum);
                fSnapPic=fopen(filename,"wb");
                fwrite(struPlateResult.pBuffer1,struPlateResult.dwPicLen,1,fSnapPic);
                iNum++;
                fclose(fSnapPic);
            }
            //车牌图
            if (struPlateResult.dwPicPlateLen != 0 && struPlateResult.byResultType == 1) 
            {
                sprintf(filename,"testPicPlate_%d.jpg",iNum);
                fSnapPicPlate=fopen(filename,"wb");
                fwrite(struPlateResult.pBuffer1,struPlateResult.dwPicLen,1,fSnapPicPlate);
                iNum++;
                fclose(fSnapPicPlate);
            }	
            //其他信息处理......
            break;
        }
        case COMM_ITS_PLATE_RESULT:
        {
            NET_ITS_PLATE_RESULT struITSPlateResult={0};
            memcpy(&struITSPlateResult, pAlarmInfo, sizeof(struITSPlateResult));

            for (i=0;i<struITSPlateResult.dwPicNum;i++)
            {
                printf("车牌号: %s\n", struITSPlateResult.struPlateInfo.sLicense);//车牌号
                switch(struITSPlateResult.struPlateInfo.byColor)//车牌颜色
                {
                case VCA_BLUE_PLATE:
                    printf("车辆颜色: 蓝色\n");
                    break;
                case VCA_YELLOW_PLATE:
                    printf("车辆颜色: 黄色\n");
                    break;
                case VCA_WHITE_PLATE:
                    printf("车辆颜色: 白色\n");
                    break;
                case VCA_BLACK_PLATE:
                    printf("车辆颜色: 黑色\n");
                    break;
                default:
                    break;
                }
                //保存场景图
                if ((struITSPlateResult.struPicInfo[i].dwDataLen != 0)&&(struITSPlateResult.struPicInfo[i].byType== 1)||(struITSPlateResult.struPicInfo[i].byType == 2))
                {
                    sprintf(filename,"testITSpic%d_%d.jpg",iNum,i);
                    fSnapPic=fopen(filename,"wb");
                    fwrite(struITSPlateResult.struPicInfo[i].pBuffer, struITSPlateResult.struPicInfo[i].dwDataLen,1,fSnapPic);
                    iNum++;
                    fclose(fSnapPic);
                }
                //车牌小图片
                if ((struITSPlateResult.struPicInfo[i].dwDataLen != 0)&&(struITSPlateResult.struPicInfo[i].byType == 0))
                {
                    sprintf(filename,"testPicPlate%d_%d.jpg",iNum,i);
                    fSnapPicPlate=fopen(filename,"wb");
                    fwrite(struITSPlateResult.struPicInfo[i].pBuffer, struITSPlateResult.struPicInfo[i].dwDataLen, 1, \ fSnapPicPlate);
                    iNum++;
                    fclose(fSnapPicPlate);
                }
                //其他信息处理......
            }
            break;
        }
    default:
        break;
    }
}

void main() {

  //---------------------------------------
  // 初始化
  NET_DVR_Init();
  //设置连接时间与重连时间
  NET_DVR_SetConnectTime(2000, 1);
  NET_DVR_SetReconnect(10000, true);

  //---------------------------------------
  // 注册设备
  LONG lUserID;
  NET_DVR_DEVICEINFO_V30 struDeviceInfo;
  lUserID = NET_DVR_Login_V30("172.0.0.100", 8000, "admin", "12345", &struDeviceInfo);
  if (lUserID < 0)
  {
       printf("Login error, %d\n", NET_DVR_GetLastError());
       NET_DVR_Cleanup(); 
       return;
  }
  
  //设置报警回调函数
  NET_DVR_SetDVRMessageCallBack_V50(0, MessageCallbackNo1, NULL);
  NET_DVR_SetDVRMessageCallBack_V50(1, MessageCallbackNo2, NULL);
  
  //启用布防
  NET_DVR_SETUPALARM_PARAM struSetupParam={0};
  struSetupParam.dwSize=sizeof(NET_DVR_SETUPALARM_PARAM);

  //上传报警信息类型: 0- 老报警信息(NET_DVR_PLATE_RESULT), 1- 新报警信息(NET_ITS_PLATE_RESULT)
  struSetupParam.byAlarmInfoType=1;
  //布防等级:二级布防，针对智能交通设备
  struSetupParam.byLevel=1;

  LONG lHandle = NET_DVR_SetupAlarmChan_V41(lUserID,&struSetupParam);
  if (lHandle < 0)
  {
      printf("NET_DVR_SetupAlarmChan_V41 error, %d\n", NET_DVR_GetLastError());
      NET_DVR_Logout(lUserID);
      NET_DVR_Cleanup(); 
      return;
  }
  
  Sleep(20000);
  //撤销布防上传通道
  if (!NET_DVR_CloseAlarmChan_V30(lHandle))
  {
      printf("NET_DVR_CloseAlarmChan_V30 error, %d\n", NET_DVR_GetLastError());
      NET_DVR_Logout(lUserID);
      NET_DVR_Cleanup(); 
      return;
  }
  
  //注销用户
  NET_DVR_Logout(lUserID);
  //释放SDK资源
  NET_DVR_Cleanup();
  return;
}
```

以布防方式设置回调接收报警信息为例

## See Also

NET_DVR_SetupAlarmChan_V41

NET_DVR_StartListen_V30

## Reference Interface

该接口扩展源于

NET_DVR_SetDVRMessageCallBack_V30

## 相关链接

- [NET_DVR_ALARMER](../structures/NET_DVR_ALARMER.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_VCA_RULE_ALARM](../structures/NET_VCA_RULE_ALARM.md)
- [NET_DVR_PDC_ALRAM_INFO](../structures/NET_DVR_PDC_ALRAM_INFO.md)
- [NET_DVR_RULE_INFO_ALARM](../structures/NET_DVR_RULE_INFO_ALARM.md)
- [NET_DVR_FACEDETECT_ALARM](../structures/NET_DVR_FACEDETECT_ALARM.md)
- [NET_VCA_FACESNAP_RESULT](../structures/NET_VCA_FACESNAP_RESULT.md)
- [NET_DVR_FACECAPTURE_STATISTICS_RESULT](../structures/NET_DVR_FACECAPTURE_STATISTICS_RESULT.md)
- [NET_VCA_FACESNAP_MATCH_ALARM](../structures/NET_VCA_FACESNAP_MATCH_ALARM.md)
- [NET_DVR_FACE_DETECTION](../structures/NET_DVR_FACE_DETECTION.md)
- [NET_DVR_TARGET_LEFT_REGION_ALARM](../structures/NET_DVR_TARGET_LEFT_REGION_ALARM.md)
- [NET_DVR_PEOPLE_DETECTION_RESULT](../structures/NET_DVR_PEOPLE_DETECTION_RESULT.md)
- [人体目标识别报警Json数据](../JSONs/EVENT_JSON_Human.md)
- [人员密度报警Json数据](../JSONs/EVENT_JSON_Density.md)
- [人员排队时长检测报警JSON数据](../ISAPI文档/personQueueDetection/39.md)
- [人员排队人数检测报警JSON数据](../ISAPI文档/personQueueDetection/40.md)
- [安全帽检测报警JSON数据](../JSONs/EVENT_JSON_safetyHelmet.md)
- [录制状态报警信息上传JSON数据](../ISAPI文档/medical/43.md)
- [资源上传云存储状态报警信息上传JSON数据](../ISAPI文档/medical/44.md)
- [EVENT_JSON](../JSONs/EVENT_JSON_signInstrument.md)
- [EVENT_JSON](../JSONs/EVENT_JSON_HFPDalertStream.md)
- [NET_DVR_VQD_ALARM](../structures/NET_DVR_VQD_ALARM.md)
- [NET_DVR_VQD_DIAGNOSE_INFO](../structures/NET_DVR_VQD_DIAGNOSE_INFO.md)
- [NET_DVR_SCENECHANGE_DETECTION_RESULT](../structures/NET_DVR_SCENECHANGE_DETECTION_RESULT.md)
- [NET_DVR_CROSSLINE_ALARM](../structures/NET_DVR_CROSSLINE_ALARM.md)
- [NET_DVR_AUDIOEXCEPTION_ALARM](../structures/NET_DVR_AUDIOEXCEPTION_ALARM.md)
- [NET_DVR_DEFOCUS_ALARM](../structures/NET_DVR_DEFOCUS_ALARM.md)
- [NET_DVR_SWITCH_LAMP_ALARM](../structures/NET_DVR_SWITCH_LAMP_ALARM.md)
- [NET_DVR_HEATMAP_RESULT](../structures/NET_DVR_HEATMAP_RESULT.md)
- [NET_DVR_FIREDETECTION_ALARM](../structures/NET_DVR_FIREDETECTION_ALARM.md)
- [NET_DVR_THERMOMETRY_DIFF_ALARM](../structures/NET_DVR_THERMOMETRY_DIFF_ALARM.md)
- [NET_DVR_THERMOMETRY_ALARM](../structures/NET_DVR_THERMOMETRY_ALARM.md)
- [NET_DVR_SHIPSDETECTION_ALARM](../structures/NET_DVR_SHIPSDETECTION_ALARM.md)
- [NET_DVR_AID_ALARM](../structures/NET_DVR_AID_ALARM.md)
- [NET_DVR_TPS_ALARM](../structures/NET_DVR_TPS_ALARM.md)
- [NET_DVR_TFS_ALARM](../structures/NET_DVR_TFS_ALARM.md)
- [NET_DVR_TPS_ALARM_V41](../structures/NET_DVR_TPS_ALARM_V41.md)
- [NET_DVR_AID_ALARM_V41](../structures/NET_DVR_AID_ALARM_V41.md)
- [NET_DVR_PLATE_RESULT](../structures/NET_DVR_PLATE_RESULT.md)
- [NET_ITS_PLATE_RESULT](../structures/NET_ITS_PLATE_RESULT.md)
- [NET_ITS_TRAFFIC_COLLECT](../structures/NET_ITS_TRAFFIC_COLLECT.md)
- [NET_ITS_ECT_BLACKLIST](../structures/NET_ITS_ECT_BLACKLIST.md)
- [NET_DVR_VEHICLE_CONTROL_LIST_DSALARM](../structures/NET_DVR_VEHICLE_CONTROL_LIST_DSALARM.md)
- [NET_DVR_VEHICLE_CONTROL_ALARM](../structures/NET_DVR_VEHICLE_CONTROL_ALARM.md)
- [NET_DVR_FIRE_ALARM](../structures/NET_DVR_FIRE_ALARM.md)
- [NET_DVR_VEHICLE_RECOG_RESULT](../structures/NET_DVR_VEHICLE_RECOG_RESULT.md)
- [NET_DVR_SENSOR_INFO_UPLOAD](../structures/NET_DVR_SENSOR_INFO_UPLOAD.md)
- [NET_DVR_CAPTURE_UPLOAD](../structures/NET_DVR_CAPTURE_UPLOAD.md)
- [NET_DVR_ALARM_RADARINFO](../structures/NET_DVR_ALARM_RADARINFO.md)
- [NET_DVR_SIGNALLAMP_DETCFG](../structures/NET_DVR_SIGNALLAMP_DETCFG.md)
- [NET_DVR_TPS_REAL_TIME_INFO](../structures/NET_DVR_TPS_REAL_TIME_INFO.md)
- [NET_DVR_TPS_STATISTICS_INFO](../structures/NET_DVR_TPS_STATISTICS_INFO.md)
- [NET_ITS_ROADINFO](../structures/NET_ITS_ROADINFO.md)
- [NET_DVR_EXTERNAL_CONTROL_ALARM](../structures/NET_DVR_EXTERNAL_CONTROL_ALARM.md)
- [NET_ITS_GATE_FACE](../structures/NET_ITS_GATE_FACE.md)
- [NET_DVR_GATE_ALARMINFO](../structures/NET_DVR_GATE_ALARMINFO.md)
- [NET_DVR_GATE_CHARGEINFO](../structures/NET_DVR_GATE_CHARGEINFO.md)
- [NET_DVR_TME_VEHICLE_RESULT](../structures/NET_DVR_TME_VEHICLE_RESULT.md)
- [NET_DVR_GATE_CARDINFO](../structures/NET_DVR_GATE_CARDINFO.md)
- [NET_DVR_ALARMHOST_ALARMINFO](../structures/NET_DVR_ALARMHOST_ALARMINFO.md)
- [NET_DVR_SENSOR_ALARM](../structures/NET_DVR_SENSOR_ALARM.md)
- [NET_DVR_SWITCH_ALARM](../structures/NET_DVR_SWITCH_ALARM.md)
- [NET_DVR_ALARMHOST_EXCEPTION_ALARM](../structures/NET_DVR_ALARMHOST_EXCEPTION_ALARM.md)
- [NET_DVR_ALARMHOST_SAFETYCABINSTATE](../structures/NET_DVR_ALARMHOST_SAFETYCABINSTATE.md)
- [NET_DVR_ALARMHOST_ALARMOUTSTATUS](../structures/NET_DVR_ALARMHOST_ALARMOUTSTATUS.md)
- [NET_DVR_CID_ALARM](../structures/NET_DVR_CID_ALARM.md)
- [NET_DVR_485_EXTERNAL_DEVICE_ALARMINFO](../structures/NET_DVR_485_EXTERNAL_DEVICE_ALARMINFO.md)
- [NET_DVR_ALARMHOST_DATA_UPLOAD](../structures/NET_DVR_ALARMHOST_DATA_UPLOAD.md)
- [NET_DVR_ALARMWIRELESSINFO](../structures/NET_DVR_ALARMWIRELESSINFO.md)
- [NET_DVR_ALARMINFO](../structures/NET_DVR_ALARMINFO.md)
- [NET_DVR_ALARMINFO_V30](../structures/NET_DVR_ALARMINFO_V30.md)
- [NET_DVR_ALARMINFO_V40](../structures/NET_DVR_ALARMINFO_V40.md)
- [NET_DVR_IPALARMINFO](../structures/NET_DVR_IPALARMINFO.md)
- [NET_DVR_IPALARMINFO_V31](../structures/NET_DVR_IPALARMINFO_V31.md)
- [NET_IPC_AUXALARM_RESULT](../structures/NET_IPC_AUXALARM_RESULT.md)
- [NET_DVR_ALARMINFO_DEV](../structures/NET_DVR_ALARMINFO_DEV.md)
- [NET_DVR_ALARMINFO_DEV_V40](../structures/NET_DVR_ALARMINFO_DEV_V40.md)
- [NET_DVR_CVR_ALARM](../structures/NET_DVR_CVR_ALARM.md)
- [NET_DVR_TRADEINFO](../structures/NET_DVR_TRADEINFO.md)
- [NET_DVR_ALARM_HOT_SPARE](../structures/NET_DVR_ALARM_HOT_SPARE.md)
- [NET_BUTTON_DOWN_EXCEPTION_ALARM](../structures/NET_BUTTON_DOWN_EXCEPTION_ALARM.md)
- [NET_DVR_SCREENALARMCFG](../structures/NET_DVR_SCREENALARMCFG.md)
- [NET_DVR_LCD_ALARM](../structures/NET_DVR_LCD_ALARM.md)
- [NET_DVR_VIDEO_INTERCOM_EVENT](../structures/NET_DVR_VIDEO_INTERCOM_EVENT.md)
- [NET_DVR_VIDEO_INTERCOM_ALARM](../structures/NET_DVR_VIDEO_INTERCOM_ALARM.md)
- [NET_DVR_DEC_VCA_ALARM](../structures/NET_DVR_DEC_VCA_ALARM.md)
- [NET_DVR_GIS_UPLOADINFO](../structures/NET_DVR_GIS_UPLOADINFO.md)
- [NET_DVR_VANDALPROOF_ALARM](../structures/NET_DVR_VANDALPROOF_ALARM.md)
- [NET_DVR_STORAGE_DETECTION_ALARM](../structures/NET_DVR_STORAGE_DETECTION_ALARM.md)
- [NET_DVR_CONFERENCE_CALL_ALARM](../structures/NET_DVR_CONFERENCE_CALL_ALARM.md)
- [NET_DVR_GPSALARMINFO](../structures/NET_DVR_GPSALARMINFO.md)
- [NET_DVR_SWITCH_CONVERT_ALARM](../structures/NET_DVR_SWITCH_CONVERT_ALARM.md)
- [NET_DVR_INQUEST_ALARM](../structures/NET_DVR_INQUEST_ALARM.md)
- [NET_DVR_PANORAMIC_LINKAGE](../structures/NET_DVR_PANORAMIC_LINKAGE.md)
- [NET_DVR_ALARM_ISAPI_INFO](../structures/NET_DVR_ALARM_ISAPI_INFO.md)
- [EventNotificationAlert JSON Block](../JSONs/EventNotificationAlert JSON Block.md)
- [NET_DVR_SetupAlarmChan_V41](NET_DVR_SetupAlarmChan_V41.md)
- [NET_DVR_StartListen_V30](../definitions/NET_DVR_StartListen_V30.md)
- [NET_DVR_SetDVRMessageCallBack_V30](NET_DVR_SetDVRMessageCallBack_V30.md)
