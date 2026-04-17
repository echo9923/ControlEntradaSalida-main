# Remarks


日志文件路径必须是绝对路径，且以"\\"结尾，例如"C:\\SdkLog\\"，建议用户先手动创建文件。若未指定文件路径，则采用默认路径"C:\\SdkLog\\"。

可多次调用该接口创建新的日志文件，更改目录时到下一次写文件时才会使用新的目录写文件。

bAutoDel为TRUE时表示覆盖模式，日志文件个数超过SDK限制个数时将会自动删除超出的文件。SDK限制个数默认为10个，可以调用接口NET_DVR_SetSDKLocalCfg(配置类型：NET_DVR_LOCAL_CFG_TYPE_LOG)进行修改配置。
