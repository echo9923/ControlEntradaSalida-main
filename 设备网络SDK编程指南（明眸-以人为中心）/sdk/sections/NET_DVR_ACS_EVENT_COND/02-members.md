# Members


- `dwSize`：结构体大小
- `dwMajor`：报警主类型，参考事件上传宏定义，0-全部
- `dwMinor`：报警次类型，参考事件上传宏定义，0-全部
- `struStartTime`：开始时间
- `struEndTime`：结束时间
- `byCardNo`：卡号（为空时默认全部）
- `byName`：持卡人姓名（为空时默认全部）
- `byPicEnable`：是否带图片，0-不带图片，1-带图片
- `byRes2`：保留，置为0
- `dwBeginSerialNo`：起始流水号（起始流水号与结束流水号都为0默认全部）
- `dwEndSerialNo`：结束流水号（起始流水号与结束流水号都为0默认全部）
- `byRes`：保留，置为0
