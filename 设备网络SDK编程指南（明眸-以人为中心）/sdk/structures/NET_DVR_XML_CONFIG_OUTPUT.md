# NET_DVR_XML_CONFIG_OUTPUT

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_XML_CONFIG_OUTPUT.html](https://open.hikvision.com/hardware/structures/NET_DVR_XML_CONFIG_OUTPUT.html)

透传接口输出参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  void     *lpOutBuffer;
  DWORD    dwOutBufferSize;
  DWORD    dwReturnedXMLSize;
  void     *lpStatusBuffer;
  DWORD    dwStatusSize;
  BYTE     byRes[32];
}NET_DVR_XML_CONFIG_OUTPUT,*LPNET_DVR_XML_CONFIG_OUTPUT;
```

## Members

- `dwSize`：[in] 结构体大小
- `lpOutBuffer`：[out] 输出参数缓冲区，XML格式，请求信令为“GET”类型时应用层需要事先分配足够大的内存
- `dwOutBufferSize`：[in] 输出参数缓冲区大小(内存大小)
- `dwReturnedXMLSize`：[out] 实际输出的XML内容大小
- `lpStatusBuffer`：[out] 返回的状态参数(XML格式：ResponseStatus)，获取命令成功时不会赋值，如果不需要，可以置NULL
- `dwStatusSize`：[in] 状态缓冲区大小(内存大小)
- `byRes`：[out] 保留，置为0

## Remarks

对于不同的协议功能（NET_DVR_XML_CONFIG_INPUT结构体中的lpRequestUrl输入的URL命令），lpOutBuffer对应不同的内容，详见NET_DVR_STDXMLConfig接口中"Remarks"说明。

NET_DVR_STDXMLConfig接口是直接透传的ISAPI协议命令，输出参数信息的详细内容可以参考ISAPI协议文档。

## 相关链接

- [ResponseStatus](../XMLs/DEVICE_RESPONSE_STATUS.md)
