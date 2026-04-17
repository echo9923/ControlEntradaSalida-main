# Parameters


- `nLogLevel`：[in] 日志的等级（默认为0）：0-表示关闭日志，1-表示只输出ERROR错误日志，2-输出ERROR错误信息和DEBUG调试信息，3-输出ERROR错误信息、DEBUG调试信息和INFO普通信息等所有信息
- `strLogDir`：[in] 日志文件的路径，windows默认值为"C:\\SdkLog\\"；linux默认值"/home/sdklog/"
- `bAutoDel`：[in] 是否删除超出的文件数，默认值为TRUE
