# Members


- `byEnable`：IP通道在线状态，是一个只读的属性；0表示HDVR或者NVR设备的数字通道连接对应的IP设备失败，该通道不在线；1表示连接成功，该通道在线
- `byIPID`：IP设备ID的低8位，byIPID = iDevID % 256
- `byChannel`：IP设备的通道号，例如设备A（HDVR或者NVR设备）的IP通道01，对应的是设备B里的通道04，则byChannel=4。
- `byIPIDHigh`：IP设备ID的高8位，byIPIDHigh = iDevID /256
- `byRes`：保留，置为0
