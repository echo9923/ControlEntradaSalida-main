# NET_DVR_CLIENTINFO

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_CLIENTINFO.html](https://open.hikvision.com/hardware/structures/NET_DVR_CLIENTINFO.html)

预览参数结构体。

## 语法

```c
struct{
  LONG    lChannel;
  LONG    lLinkMode;
  HWND    hPlayWnd;
  char    *sMultiCastIP;
}NET_DVR_CLIENTINFO, *LPNET_DVR_CLIENTINFO;
```

## Members

- `lChannel`：通道号，1~32表示模拟通道1~32，9000系列混合型DVR和NVR等设备的IP通道从33开始。
- `lLinkMode`：最高位(31)为0表示主码流，为1表示子码流；0～30位表示连接方式：0－TCP方式，1－UDP方式，2－多播方式

   例如子码流TCP连接，则lLinkMode=0x80000000
- `hPlayWnd`：播放窗口的句柄，为NULL表示不显示图像
- `sMultiCastIP`：多播组地址

## Remarks

该结构中的hPlayWnd参数若设置为NULL，则SDK仍取流，但不进行解码显示，所以仍可以录像。

通过登录接口返回byMainProto和bySubProto可以判断设备支持的应用层取流协议：值为2，默认使用私有协议传输RTP码流；值为1，默认使用RTSP协议；值为0，使用私有协议传输PS码流。

Linux下

对于4.1或者以上的版本的SDK，HWND表示播放窗口的句柄，定义为：

typedef unsigned int HWND;

如果使用Qt进行界面开发，示例如下：

NET_DVR_CLIENTINFO tmpclientinfo;

tmpclientinfo.hPlayWnd = (HWND)m_framePlayWnd->GetPlayWndId();

对于4.1以前的版本的SDK，HWND定义如下：

typedef struct __PLAYRECT

{
  
         
    int  x;          //显示框左上角横坐标  
               
                      
    int  y;          //显示框左上角纵坐标    
                    
                           
    int  uWidth;  //显示框宽度      
                 
                        
    int  uHeight;  //显示框高度 
                       
}PLAYRECT;

typedef PLAYRECT HWND;

NET_DVR_CLIENTINFO结构中的hPlayWnd = {0}则SDK仍取流，不进行解码显示，所以仍可以录像，但是不能设置hPlayWnd = 0(即NULL)，否则非法结构地址会导致调用hPlayWnd.x等去判断的时候崩溃。

## See Also

NET_DVR_RealPlay_V30

## 相关链接

- [NET_DVR_RealPlay_V30](../definitions/NET_DVR_RealPlay_V30.md)
