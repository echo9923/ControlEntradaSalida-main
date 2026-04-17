# 4.2.2 创建 WebControl 实例


要使用视频WEB插件，首先需要创建WebControl实例，示例代码如下：

```javascript
var oWebControl = new WebControl({ // 创建 WebControl 实例
szPluginContainer: "playWnd", // 指定 DIV 窗口标识
iServicePortStart: 15900, // 指定起止端口号，建议使用该值
iServicePortEnd: 15909,
// 用于 IE10 使用 ActiveX 的 clsid
szClassId:"23BF3B0A-2C56-4D97-9C03-0CB103AA8F11",
cbConnectSuccess: function () {
// 创建 WebControl 实例成功
},
cbConnectError: function () {
// 创建 WebControl 实例失败
},
cbConnectClose: function (bNormalClose) {
// 插件使用过程中发生的断开与插件服务连接的回调
// bNormalClose = false 时表示异常断开
// bNormalClose = true 时表示正常断开
}
});
```
