# 3.13 JS_SetWindowControlCallback 设置消息回调接口


JS_SetWindowControlCallback 用于设置视频 web 插件消息回调，所有视频插件的消息都通过设置的回调通知前端，前端调用示例如下：

// 设置消息回调，oWebControl 是 jsWebControl-1.0.0.min.js 中 WebControl 的一个实例

```javascript
oWebControl.JS_SetWindowControlCallback({
cbIntegrationCallBack: function(oData){ // oData 是封装的视频 web 插件回调消息的消息体
console.log(JSON.stringify(oData)); // 打印消息体至控制台
```

回调的消息体为 json报文，数据格式如下：

```javascript
uuid: "xxx-xxx-xxx-xxx", // 消息体唯一标识
sequence: "", // 序号
cmd: "window.integrationCallBack", // 命令
responseMsg: {
// 此处为视频 web 插件消息 json 报文字符串
```

其中 responseMsg 对应的 value 是视频 web 插件返回的 json 封装的消息，只需解析 responseMsg 即可。目前支持的视频 web插件消息有窗口选中消息、预览或回放播放消息、抓图结果消息和预览紧急录像或回放录像剪辑结果消息。这四类消息遵循统一的消息格式，如下：

{  
type: 1, // 消息类型，取值详见 3.12.\*  
msg:  
{  
wndId: 1, // 窗口序号，从 1 开始  
result: 0x0100, // 0x0100-正在播放 0x0200-空闲  
cameraIndexcode: "58e90452772a4d9da7c7ba4cef26dbf0", // 监控点编号  
expand: "" // 扩展字段  
}  
}
