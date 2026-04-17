# NET_DVR_STDXMLConfig

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_STDXMLConfig.html](https://open.hikvision.com/hardware/definitions/NET_DVR_STDXMLConfig.html)

ISAPI协议命令透传。

## 语法

```c
BOOL NET_DVR_STDXMLConfig(
  LONG                         lUserID,
  NET_DVR_XML_CONFIG_INPUT     *lpInputParam,
  NET_DVR_XML_CONFIG_OUTPUT    *lpOutputParam
);
```

## Parameters

- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `lpInputParam`：[in] 输入参数
- `lpOutputParam`：[in&out] 输出参数

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

通过该接口可以直接透传ISAPI协议命令，实现参数配置、能力集获取等功能。调用该接口需要设备支持ISAPI协议（PUT、GET、POST、DELETE等命令），发送的命令内容请参考ISAPI协议文档。

## See Also

NET_DVR_Login_V40

## 相关链接

- [NET_DVR_XML_CONFIG_INPUT](../structures/NET_DVR_XML_CONFIG_INPUT_ISAPI.md)
- [NET_DVR_XML_CONFIG_OUTPUT](../structures/NET_DVR_XML_CONFIG_OUTPUT_ISAPI.md)
- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Login_V40](../definitions/NET_DVR_Login_V40.md)
