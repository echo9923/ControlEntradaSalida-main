# Remarks


1.IP报警输入资源只能获取，设备从IP设备资源获取对应的报警参数后进行紧凑排列，然后传给网络SDK。

2.IP报警输入资源的下标索引值（0到MAX_IP_ALARMIN -1）加上MAX_ANALOG_ALARMIN对应的是报警输入相关参数（报警输入配置结构等）的下标索引值（MAX_ANALOG_ALARMIN到MAX_ALARMIN_V30-1）。
