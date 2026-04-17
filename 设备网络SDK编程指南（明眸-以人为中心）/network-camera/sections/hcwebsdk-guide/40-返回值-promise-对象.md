# 返回值：Promise 对象


说明： 该接口为按时间回放接口，开发包目前只支持按时间回放，不支持按文件回放，不过用户可以搜索出录像，然后按照录像的开始时间和结束时间来回放。时间必须严格按照说明所示格式输入。oTransCodeParam 是一个 json 对象，格式如下：

{   
TransFrameRate: "16",   
TransResolution: "2",   
TransBitrate: "23"   
}
