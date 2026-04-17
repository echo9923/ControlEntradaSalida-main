# Parameters


- `lUserID`：[in] NET_DVR_Login_V40等登录接口的返回值
- `dwUpgradeType`：[in] 升级类型，具体定义如下：

enum _ENUM_UPGRADE_TYPE{
  ENUM_UPGRADE_DVR           = 0, //普通设备升级
  ENUM_UPGRADE_ADAPTER       = 1, //DVR适配器升级
  ENUM_UPGRADE_VCALIB        = 2, //智能库升级
  ENUM_UPGRADE_OPTICAL       = 3, //光端机升级
  ENUM_UPGRADE_ACS           = 4, //门禁系统升级
  ENUM_UPGRADE_AUXILIARY_DEV = 5  //辅助设备升级
}ENUM_UPGRADE_TYPE
- `sFileName`：[in]  升级的文件路径（包括文件名）。路径长度和操作系统有关，sdk不做限制，windows默认路径长度小于等于256字节（包括文件名在内）。
- `pInbuffer`：[in]  升级条件缓冲区，不同的升级类型对应不同的升级条件，具体如下表所示
- `dwBufferLen`：[in]  缓冲区大小
