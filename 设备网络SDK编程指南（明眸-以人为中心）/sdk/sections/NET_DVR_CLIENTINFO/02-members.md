# Members


- `lChannel`：通道号，1~32表示模拟通道1~32，9000系列混合型DVR和NVR等设备的IP通道从33开始。
- `lLinkMode`：最高位(31)为0表示主码流，为1表示子码流；0～30位表示连接方式：0－TCP方式，1－UDP方式，2－多播方式

   例如子码流TCP连接，则lLinkMode=0x80000000
- `hPlayWnd`：播放窗口的句柄，为NULL表示不显示图像
- `sMultiCastIP`：多播组地址
