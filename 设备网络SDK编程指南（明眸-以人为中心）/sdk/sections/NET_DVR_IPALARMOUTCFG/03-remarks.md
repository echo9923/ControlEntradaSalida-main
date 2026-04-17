# Remarks


1.IP报警输出资源只能获取，设备从IP设备资源获取对应的报警参数后进行紧凑排列，然后传给设备。

2.IP报警输出资源的下标索引值（0到MAX_IP_ALARMOUT -1）加上MAX_ANALOG_ALARMOUT对应的是报警输出相关参数（报警输出配置结构、联动触发报警输出等）的下标索引值（MAX_ANALOG_ALARMOUT到MAX_ALARMOUT_V30-1）。
