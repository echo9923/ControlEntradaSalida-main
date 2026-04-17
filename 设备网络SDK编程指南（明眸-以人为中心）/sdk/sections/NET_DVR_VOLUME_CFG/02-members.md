# Members


- `dwSize`：结构体大小
- `wVolume`：音量大小，数组0表示音频输出，数组1表示音频编码，具体索引代表含义以能力集为准
- `byPhantomPowerSupply`：是否使用幻象电源供电(音频输入通道为MIC时有效)：0- 无意义，1- 不供电，2- 供电
- `byEnableAEC`：是否启用全局的回声消除：0- 不启用，1- 启用
- `byRes1`：保留，置为0
- `byEnableFBC`：是否启用FBC(啸叫抑制)：0- 不启用，1- 启用
- `wVolumeEx`：音量大小扩展，具体索引代表含义以能力集为准
- `byRes`：保留，置为0
