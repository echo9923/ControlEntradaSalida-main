# 3.1 JS_StartService 启动插件服务接口


接口定义

JS_StartService(szType, options)

功能说明

启动插件服务

使用条件

插件实例化成功后启动插件服务

入参

<table><tr><td rowspan=1 colspan=1>名称</td><td rowspan=1 colspan=1>描述</td><td rowspan=1 colspan=1>是否必填</td><td rowspan=1 colspan=1>备注</td></tr><tr><td rowspan=1 colspan=1>sZType</td><td rowspan=1 colspan=1>服务类型</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1>请固定填充&quot;window&quot;</td></tr><tr><td rowspan=1 colspan=1>options</td><td rowspan=1 colspan=1>可选参数对象</td><td rowspan=1 colspan=1>否</td><td rowspan=1 colspan=1>请固定填充{dllPath: &quot;/VideoPluginConnect.ll&quot;}</td></tr></table>

返回值

Promise。接口成功说明服务启动成功，失败则说明服务启动失败

使用示例

```javascript
oWebControl.JS_StartService("window", { // oWebControl 为 WebControl 的对象
dllPath: "./VideoPluginConnect.dll"
}).then(function(){
```

```javascript
// 服务启动成功
},function(){
// 服务启动失败
});
 特殊使用场景说明
无
```
