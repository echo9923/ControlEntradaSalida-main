# Members


- `dwSize`：结构体大小
- `dwFaceTemplate1Size`：人脸模板1数据大小，等于0时表示无人脸模板1数据
- `pFaceTemplate1Buffer`：人脸模板1数据缓存（不大于2.5k）
- `dwFaceTemplate2Size`：人脸模板2数据大小，等于0时表示无人脸模板2数据
- `pFaceTemplate2Buffer`：人脸模板2数据缓存（不大于2.5K）
- `dwFacePicSize`：人脸图片数据大小，等于0时表示无人脸图片数据
- `pFacePicBuffer`：人脸图片数据缓存
- `byFaceQuality1`：模板1对应的人脸质量，取值范围：1~100
- `byFaceQuality2`：模板2对应的人脸质量，取值范围：1~100
- `byCaptureProgress`：采集进度，目前只有两种进度值：0- 未采集到人脸，100- 采集到人脸（只有在进度为100时，才解析人脸信息）
- `byRes`：保留，置为0
