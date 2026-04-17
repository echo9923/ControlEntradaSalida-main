# Parameters


- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwAbilityType`：[in] 能力类型，具体定义见下表：

宏定义

宏定义值

含义

DEVICE_SOFTHARDWARE_ABILITY

0x001

设备软硬件能力

DEVICE_NETWORK_ABILITY

0x002

设备无线网络能力

DEVICE_ENCODE_ALL_ABILITY_V20

0x008

设备所有编码能力

IPC_FRONT_PARAMETER_V20

0x009

设备前端参数

DEVICE_RAID_ABILITY

0x007

设备RAID能力

DEVICE_ALARM_ABILITY

0x00a

设备报警能力

DEVICE_DYNCHAN_ABILITY

0x00b

设备数字通道能力

DEVICE_USER_ABILITY

0x00c

设备用户管理参数能力

DEVICE_NETAPP_ABILITY

0x00d

设备网络应用参数能力

DEVICE_VIDEOPIC_ABILITY

0x00e

设备图像参数能力

DEVICE_JPEG_CAP_ABILITY

0x00f

设备JPEG抓图能力

DEVICE_SERIAL_ABILITY

0x010

设备RS232和RS485串口能力

DEVICE_ABILITY_INFO

0x011

设备通用能力类型，具体能力根据发送的能力节点来区分

STREAM_ABILITY

0x012

设备流能力集

MATRIXDECODER_ABILITY

0x200

多路解码器显示、解码能力

DECODER_ABILITY

0x261

解码器XML能力集

SNAPCAMERA_ABILITY

0x300

智能交通摄像机能力集

PIC_CAPTURE_ABILITY

0x402

抓图图片分辨率能力集
- `pInBuf`：[in] 输入缓冲区指针（按照设备规定的能力参数的描述方式组合，可以是XML文本或结构体形式，详见“Remarks”说明）
- `dwInLength`：[in] 输入缓冲区的长度
- `pOutBuf`：[out] 输出缓冲区指针（按照设备规定的能力集的描述方式，可以是XML文本或结构体形式，详见“Remarks”说明）
- `dwOutLength`：[in] 接收数据的缓冲区的长度
