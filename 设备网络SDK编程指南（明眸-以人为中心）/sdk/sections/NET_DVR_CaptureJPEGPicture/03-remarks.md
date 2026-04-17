# Remarks


该接口用于设备的单帧数据捕获，并保存成JPEG图片文件。JPEG抓图功能或者抓图分辨率需要设备支持，如果不支持接口返回失败，错误号23或者29。

对于DVR、NVR设备，参数wPicQuality支持的分辨率值可通过NET_DVR_GetDeviceAbility获取能力集类型PIC_CAPTURE_ABILITY获取(NET_DVR_COMPRESSIONCFG_ABILITY)得到。

对接网络摄像机、门禁主机等设备，设备是否支持JPEG抓图功能或者支持的参数能力，可以通过设备能力集进行判断，对应设备JPEG抓图能力集(JpegCaptureAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：DEVICE_JPEG_CAP_ABILITY，节点：。

wPicSize设为2抓取的图片分辨率是4CIF还是D1由设备决定，一般为4CIF(P制:704*576/N制:704*480)。
