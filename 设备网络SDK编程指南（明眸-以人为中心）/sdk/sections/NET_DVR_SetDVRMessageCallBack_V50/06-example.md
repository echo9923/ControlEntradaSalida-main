# Example


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
