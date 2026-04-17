# Members


- `dwSize`：结构体大小
- `lpRequestUrl`：请求信令，字符串格式
- `dwRequestUrlLen`：请求信令长度，字符串长度
- `lpInBuffer`：输入参数缓冲区，XML格式
- `dwInBufferSize`：输入参数缓冲区大小
- `dwRecvTimeOut`：接收超时时间，单位：ms，填0则使用默认超时5s
- `byForceEncrpt`：是否强制加密（启用之后透传的XML报文将加密传输，AES128加密算法）：0- 否，1- 是
- `byRes`：保留，置为0
