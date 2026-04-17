# NET_DVR_Cleanup

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_Cleanup.html](https://open.hikvision.com/hardware/definitions/NET_DVR_Cleanup.html)

释放SDK资源，在程序结束之前调用。

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

以下是该接口可能返回的错误值

## Remarks

在调用NET_DVR_Cleanup的时候，不能同时调用其他任何SDK接口。NET_DVR_Init和NET_DVR_Cleanup需要配对使用，即程序里面调用多少次NET_DVR_Init，退出时就需要调用多少次NET_DVR_Cleanup。

## See Also

NET_DVR_Init

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_Init](NET_DVR_Init.md)
