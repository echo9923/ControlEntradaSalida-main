# 3.9 JS_DestroyWnd 插件窗口销毁接口


接口定义

功能说明

销毁插件窗口

使用条件

当不需要视频播放时，通过此接口来销毁插件窗口

入参

无

返回值

Promise。接口成功说明销毁插件窗口成功，失败则说明销毁插件窗口失败

使用示例

```javascript
oWebControl.JS_DestroyWnd().then(function(){ // oWebControl 为 WebControl 的对象
// 销毁插件窗口成功
},function(){
// 销毁插件窗口失败
```

特殊使用场景说明

浏览器页面需随时启用和禁用视频播放的场景，可通过 JS_DestroyWnd 销毁插件窗口来禁用视频播放，可通过JS_CreateWnd 来重新启用视频播放
