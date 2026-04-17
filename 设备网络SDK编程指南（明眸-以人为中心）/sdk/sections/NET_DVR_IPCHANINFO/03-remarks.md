# Remarks


iDevID为设备ID号，iDevID = byIPIDHigh*256 + byIPID。通过iDevID值查找具体的设备信息struIPDevInfo（结构体NET_DVR_IPPARACFG_V40的数组参数），与设备信息数组下标（iDevInfoIndex）换算关系为：iDevID = iDevInfoIndex + iGroupNO*64 +1。
