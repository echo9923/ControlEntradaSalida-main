# NET_DVR_DEVICECFG_V40

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_DEVICECFG_V40.html](https://open.hikvision.com/hardware/structures/NET_DVR_DEVICECFG_V40.html)

设备参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  BYTE     sDVRName[NAME_LEN];
  DWORD    dwDVRID;
  DWORD    dwRecycleRecord;
  BYTE     sSerialNumber[SERIALNO_LEN];
  DWORD    dwSoftwareVersion;
  DWORD    dwSoftwareBuildDate;
  DWORD    dwDSPSoftwareVersion;
  DWORD    dwDSPSoftwareBuildDate;
  DWORD    dwPanelVersion;
  DWORD    dwHardwareVersion;
  BYTE     byAlarmInPortNum;
  BYTE     byAlarmOutPortNum;
  BYTE     byRS232Num;
  BYTE     byRS485Num;
  BYTE     byNetworkPortNum;
  BYTE     byDiskCtrlNum;
  BYTE     byDiskNum;
  BYTE     byDVRType;
  BYTE     byChanNum;
  BYTE     byStartChan;
  BYTE     byDecordChans;
  BYTE     byVGANum;
  BYTE     byUSBNum;
  BYTE     byAuxoutNum;
  BYTE     byAudioNum;
  BYTE     byIPChanNum;
  BYTE     byZeroChanNum;
  BYTE     bySupport;
  BYTE     byEsataUseage;
  BYTE     byIPCPlug;
  BYTE     byStorageMode;
  BYTE     bySupport1;
  WORD     wDevType;
  BYTE     byDevTypeName[DEV_TYPE_NAME_LEN];
  BYTE     bySupport2;
  BYTE     byAnalogAlarmInPortNum;
  BYTE     byStartAlarmInNo;
  BYTE     byStartAlarmOutNo;
  BYTE     byStartIPAlarmInNo;
  BYTE     byStartIPAlarmOutNo;
  BYTE     byHighIPChanNum;
  BYTE     byEnableRemotePowerOn;
  WORD     wDevClass;
  BYTE     byRes2[6];
}NET_DVR_DEVICECFG_V40,*LPNET_DVR_DEVICECFG_V40;
```

## Members

- `dwSize`：结构体大小
- `sDVRName`：设备名称
- `dwDVRID`：设备ID号，用于遥控器，v1.4的设备号范围为(0~99), v1.5及以上版本的设备号为(1~255)
- `dwRecycleRecord`：是否循环录像：0－不是，1－是
- `sSerialNumber`：（只读，不可修改）设备序列号
- `dwSoftwareVersion`：（只读，不可修改）软件版本号：

V3.0以上版本支持的设备最高8位为主版本号，次高8位为次版本号，低16位为修复版本号，例如：0x05050000表示V5.5.0；

V3.0以下版本支持的设备高16位表示主版本，低16位表示次版本
- `dwSoftwareBuildDate`：（只读，不可修改）软件生成日期，高16位表示年份（需要加2000），次8位表示月份，最后8位表示日期，例如：0x0011090e表示build20170914
- `dwDSPSoftwareVersion`：（只读，不可修改）DSP软件版本，高16位是主版本，低16位是次版本
- `dwDSPSoftwareBuildDate`：（只读，不可修改）DSP软件生成日期，高16位表示年份（需要加2000），次8位表示月份，最后8位表示日期
- `dwPanelVersion`：（只读，不可修改）前面板版本，高16位是主版本，低16位是次版本
- `dwHardwareVersion`：（只读，不可修改）硬件版本，高16位是主版本，低16位是次版本
- `byAlarmInPortNum`：（只读，不可修改）设备模拟报警输入个数
- `byAlarmOutPortNum`：（只读，不可修改）设备模拟报警输出个数
- `byRS232Num`：（只读，不可修改）设备232串口个数
- `byRS485Num`：（只读，不可修改）设备485串口个数
- `byNetworkPortNum`：（只读，不可修改）网络口个数
- `byDiskCtrlNum`：（只读，不可修改）硬盘控制器个数
- `byDiskNum`：（只读，不可修改）硬盘个数
- `byDVRType`：（只读，不可修改）设备类型，详见下文列表
- `byChanNum`：（只读，不可修改）设备模拟通道个数
- `byStartChan`：（只读，不可修改）模拟通道的起始通道号
- `byDecordChans`：（只读，不可修改）设备解码路数
- `byVGANum`：（只读，不可修改）VGA口的个数
- `byUSBNum`：（只读，不可修改）USB口的个数
- `byAuxoutNum`：（只读，不可修改）辅口的个数
- `byAudioNum`：（只读，不可修改）语音口的个数
- `byIPChanNum`：（只读，不可修改）最大数字通道低8位，高8位见byHighIPChanNum
- `byZeroChanNum`：（只读，不可修改）零通道编码个数
- `bySupport`：（只读，不可修改）能力，位与结果为0表示不支持，1表示支持

   bySupport & 0x1，表示是否支持智能搜索

   bySupport & 0x2，表示是否支持备份

   bySupport & 0x4，表示是否支持压缩参数能力获取

   bySupport & 0x8，表示是否支持双网卡

   bySupport & 0x10，表示支持远程SADP

   bySupport & 0x20，表示支持Raid卡功能

   bySupport & 0x40，表示支持IPSAN搜索

   bySupport & 0x80，表示支持rtp over rtsp
- `byEsataUseage`：Esata的默认用途，0-默认备份，1-默认录像
- `byIPCPlug`：0-关闭即插即用，1-打开即插即用
- `byStorageMode`：存储模式：0-盘组模式，1-磁盘配额，2-抽帧模式
- `bySupport1`：（只读，不可修改）能力集扩充，位与结果：0表示不支持，1表示支持

   bySupport1 & 0x1, 表示是否支持snmp v30

   bySupport1 & 0x2, 支持区分回放和下载
- `wDevType`：（只读，不可修改）设备型号，见下文列表
- `byDevTypeName`：（只读，不可修改）设备型号名称
- `bySupport2`：（只读，不可修改）能力集扩充，位与结果：0表示不支持，1表示支持

   bySupport2 & 0x1, 表示是否支持是否支持扩展的OSD字符叠加(终端和抓拍机扩展区分)
- `byAnalogAlarmInPortNum`：（只读，不可修改）模拟报警输入个数
- `byStartAlarmInNo`：（只读，不可修改）模拟报警输入起始号
- `byStartAlarmOutNo`：（只读，不可修改）模拟报警输出起始号
- `byStartIPAlarmInNo`：（只读，不可修改）IP报警输入起始号，0表示参数无效
- `byStartIPAlarmOutNo`：（只读，不可修改）IP报警输出起始号，0表示参数无效
- `byHighIPChanNum`：（只读，不可修改）最大数字通道高8位，低8位见byIPChanNum
- `byEnableRemotePowerOn`：是否启用在设备休眠的状态下远程开机功能：0- 不启用，1- 启用
- `wDevClass`：设备大类，判断设备是属于哪个产品线：0-保留，1~50表示DVR，51~100表示DVS，101~150表示NVR，151~200表示IPC，65534表示其他类型
- `byRes2`：保留，置为0

## Remarks

byDVRType和wDevType取值定义如下所示：

## See Also

NET_DVR_GetDVRConfig  NET_DVR_SetDVRConfig

## Reference Structure

该结构扩展源于

NET_DVR_DEVICECFG

## 相关链接

- [NET_DVR_HDGROUP_CFG](../structures/NET_DVR_HDGROUP_CFG.md)
- [NET_DVR_DISK_QUOTA_CFG](../structures/NET_DVR_DISK_QUOTA_CFG.md)
- [NET_DVR_DRAWFRAME_DISK_QUOTA_CFG](../structures/NET_DVR_DRAWFRAME_DISK_QUOTA_CFG.md)
- [NET_DVR_GetDVRConfig](../definitions/NET_DVR_GetDVRConfig_BASIC.md)
- [NET_DVR_SetDVRConfig](../definitions/NET_DVR_SetDVRConfig_BASIC.md)
- [NET_DVR_DEVICECFG](../structures/NET_DVR_DEVICECFG.md)
