# Members


- `dwSize`：结构体大小
- `dwMajor`：报警主类型，具体定义见“Remarks”说明
- `dwMinor`：报警次类型，次类型含义根据主类型不同而不同，具体定义见“Remarks”说明
- `struTime`：报警时间
- `sNetUser`：网络操作的用户名
- `struRemoteHostAddr`：远程主机地址
- `struAcsEventInfo`：报警信息详细参数
- `dwPicDataLen`：图片数据大小，不为0是表示后面带数据
- `pPicData`：图片数据缓冲区
- `byRes`：保留，置为0
