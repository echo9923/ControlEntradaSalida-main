# NET_DVR_CaptureJPEGPicture

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_CaptureJPEGPicture.html](https://open.hikvision.com/hardware/definitions/NET_DVR_CaptureJPEGPicture.html)

单帧数据捕获并保存成JPEG图。

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `lChannel`：[in] 通道号
- `lpJpegPara`：[in] JPEG图像参数
- `sPicFileName`：[in] 保存JPEG图的文件路径（包括文件名）

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

该接口用于设备的单帧数据捕获，并保存成JPEG图片文件。JPEG抓图功能或者抓图分辨率需要设备支持，如果不支持接口返回失败，错误号23或者29。

对于DVR、NVR设备，参数wPicQuality支持的分辨率值可通过NET_DVR_GetDeviceAbility获取能力集类型PIC_CAPTURE_ABILITY获取(NET_DVR_COMPRESSIONCFG_ABILITY)得到。

对接网络摄像机、门禁主机等设备，设备是否支持JPEG抓图功能或者支持的参数能力，可以通过设备能力集进行判断，对应设备JPEG抓图能力集(JpegCaptureAbility)，相关接口：NET_DVR_GetDeviceAbility，能力集类型：DEVICE_JPEG_CAP_ABILITY，节点：。

wPicSize设为2抓取的图片分辨率是4CIF还是D1由设备决定，一般为4CIF(P制:704*576/N制:704*480)。

## See Also

NET_DVR_Login_V40

## 相关链接

- [LPNET_DVR_JPEGPARA](../structures/NET_DVR_JPEGPARA.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_GetDeviceAbility](..\接口定义\NET_DVR_GetDeviceAbility.md)
- [JpegCaptureAbility](../XMLs/DEVICE_JPEG_CAP_ABILITY.md)
- [NET_DVR_GetDeviceAbility](../definitions/NET_DVR_GetDeviceAbility.md)
- [NET_DVR_Login_V40](NET_DVR_Login_V40.md)
