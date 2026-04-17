# Remarks


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
