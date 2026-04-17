# NET_DVR_XML_CONFIG_INPUT

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_XML_CONFIG_INPUT.html](https://open.hikvision.com/hardware/structures/NET_DVR_XML_CONFIG_INPUT.html)

透传接口输入参数结构体。

## 语法

```c
struct{
  DWORD    dwSize;
  void     *lpRequestUrl;
  DWORD    dwRequestUrlLen;
  void     *lpInBuffer;
  DWORD    dwInBufferSize;
  DWORD    dwRecvTimeOut;
  BYTE     byForceEncrpt;
  BYTE     byRes[31];
}NET_DVR_XML_CONFIG_INPUT,*LPNET_DVR_XML_CONFIG_INPUT;
```

## Members

- `dwSize`：结构体大小
- `lpRequestUrl`：请求信令，字符串格式
- `dwRequestUrlLen`：请求信令长度，字符串长度
- `lpInBuffer`：输入参数缓冲区，XML格式
- `dwInBufferSize`：输入参数缓冲区大小
- `dwRecvTimeOut`：接收超时时间，单位：ms，填0则使用默认超时5s
- `byForceEncrpt`：是否强制加密（启用之后透传的XML报文将加密传输，AES128加密算法）：0- 否，1- 是
- `byRes`：保留，置为0

## Remarks

对于不同的协议功能（lpRequestUrl输入的URL命令），lpInBuffer对应不同的内容，详见NET_DVR_STDXMLConfig接口中"Remarks"说明。

NET_DVR_STDXMLConfig接口是直接透传的ISAPI协议命令，该结构体中的请求信令以及输入参数信息的详细内容可以参考ISAPI协议文档。
