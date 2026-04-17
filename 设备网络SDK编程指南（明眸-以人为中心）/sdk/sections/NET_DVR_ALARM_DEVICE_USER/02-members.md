# Members


- `dwSize`：结构体大小
- `sUserName`：用户名
- `sPassword`：密码
- `struUserIP`：用户IP地址(为0时表示允许任何地址)
- `byMACAddr`：物理地址
- `byUserType`：用户类型：0- 普通用户，1- 管理员用户
- `byAlarmOnRight`：布防权限
- `byAlarmOffRight`：撤防权限
- `byBypassRight`：旁路权限
- `byOtherRight`：其他权限，参数取值为1表示使能：

byOtherRight[0]：日志权限

byOtherRight[1]：重启关机

byOtherRight[2]：参数设置权限

byOtherRight[3]：参数获取权限

byOtherRight[4]：恢复默认参数权限

byOtherRight[5]：警号输出权限

byOtherRight[6]：PTZ 控制权限

byOtherRight[7]：远程升级权限
 
byOtherRight[8]：报警输出控制

byOtherRight[9]：串口控制

byOtherRight[10]：门禁控制

byOtherRight[11]：语音对讲

byOtherRight[12]：远程控制本地输出

byOtherRight[13]：硬盘配置

byOtherRight[14]：格式化硬盘

byOtherRight[15]：模拟量控制
- `byNetPreviewRight`：远程可以预览的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `byNetRecordRight`：远程可以录像的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `byNetPlaybackRight`：远程可以回放的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `byNetPTZRight`：远程可以PTZ的通道，按位表示各通道（bit0：通道1，bit1：通道2，依次类推）：1- 有权限，0- 无权限
- `sOriginalPassword`：原始密码
- `byRes2`：保留
