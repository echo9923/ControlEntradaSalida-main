# Parameters


- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwCommand`：[in] 配置命令，不同的功能对应不同的命令号(dwCommand)，lpInBuffer等参数也对应不同的内容，如下表所示：

	

		
dwCommand宏定义

        
宏定义值

        
含义

		
lpInBuffer

        
cbStateCallback

	

	

		
NET_DVR_FIND_NAS_DIRECTORY

		
6161

        
查找NAS目录

		
NET_DVR_NET_DISK_SERACH_PARAM

        
NULL
- `lpInBuffer`：[in] 输入参数，具体内容跟配置命令相关，详见列表
- `dwInBufferLen`：[in] 输入缓冲的大小
- `cbStateCallback`：[in] 状态回调函数
- `pUserData`：[in] 用户数据
