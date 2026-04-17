# NET_DVR_SetLogToFile

- 来源：[https://open.hikvision.com/hardware/definitions/NET_DVR_SetLogToFile.html](https://open.hikvision.com/hardware/definitions/NET_DVR_SetLogToFile.html)

启用写日志文件。

## Parameters

- `nLogLevel`：[in] 日志的等级（默认为0）：0-表示关闭日志，1-表示只输出ERROR错误日志，2-输出ERROR错误信息和DEBUG调试信息，3-输出ERROR错误信息、DEBUG调试信息和INFO普通信息等所有信息
- `strLogDir`：[in] 日志文件的路径，windows默认值为"C:\\SdkLog\\"；linux默认值"/home/sdklog/"
- `bAutoDel`：[in] 是否删除超出的文件数，默认值为TRUE

## Return Values

TRUE表示成功，FALSE表示失败。接口返回失败请调用NET_DVR_GetLastError获取错误码，通过错误码判断出错原因。

## Remarks

日志文件路径必须是绝对路径，且以"\\"结尾，例如"C:\\SdkLog\\"，建议用户先手动创建文件。若未指定文件路径，则采用默认路径"C:\\SdkLog\\"。

可多次调用该接口创建新的日志文件，更改目录时到下一次写文件时才会使用新的目录写文件。

bAutoDel为TRUE时表示覆盖模式，日志文件个数超过SDK限制个数时将会自动删除超出的文件。SDK限制个数默认为10个，可以调用接口NET_DVR_SetSDKLocalCfg(配置类型：NET_DVR_LOCAL_CFG_TYPE_LOG)进行修改配置。

## 相关链接

- [NET_DVR_GetLastError](../definitions/NET_DVR_GetLastError.md)
- [NET_DVR_SetSDKLocalCfg](NET_DVR_SetSDKLocalCfg.md)
