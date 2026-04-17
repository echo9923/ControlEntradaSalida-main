# Members


- `dwSize`：[in] 结构体大小
- `lpOutBuffer`：[out] 输出参数缓冲区，XML格式，请求信令为“GET”类型时应用层需要事先分配足够大的内存
- `dwOutBufferSize`：[in] 输出参数缓冲区大小(内存大小)
- `dwReturnedXMLSize`：[out] 实际输出的XML内容大小
- `lpStatusBuffer`：[out] 返回的状态参数(XML格式：ResponseStatus)，获取命令成功时不会赋值，如果不需要，可以置NULL
- `dwStatusSize`：[in] 状态缓冲区大小(内存大小)
- `byRes`：[out] 保留，置为0
