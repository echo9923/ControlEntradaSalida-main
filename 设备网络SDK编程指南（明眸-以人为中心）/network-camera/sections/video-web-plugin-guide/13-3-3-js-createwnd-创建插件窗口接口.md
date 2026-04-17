# 3.3 JS_CreateWnd 创建插件窗口接口


接口定义

JS_CreateWnd(szId, iWidth, iHeight, options)

功能说明

创建插件窗口，并且插件窗口始终置顶。若不想插件置顶，显示其它控件，详见 3.6和3.7中的接口描述。

使用条件

插件实例化成功后启动插件服务

入参

<table><tr><td colspan="1" rowspan="1">名称</td><td colspan="1" rowspan="1">描述</td><td colspan="1" rowspan="1">是否必填</td><td colspan="1" rowspan="1">备注</td></tr><tr><td colspan="1" rowspan="1">szId</td><td colspan="1" rowspan="1">元素ID</td><td colspan="1" rowspan="1">是</td><td colspan="1" rowspan="1">该元素ID 标识的的窗口作为插件的父窗口</td></tr><tr><td colspan="1" rowspan="1">iWidth</td><td colspan="1" rowspan="1">元素ID 标识窗口的宽度</td><td colspan="1" rowspan="1">是</td><td colspan="1" rowspan="1">使用元素ID 标识的窗口的宽度使插件窗口与DIV窗口重叠</td></tr><tr><td colspan="1" rowspan="1">iHeight</td><td colspan="1" rowspan="1">元素ID 标识窗口的高度</td><td colspan="1" rowspan="1">是</td><td colspan="1" rowspan="1">使用元素ID 标识的窗口的高度使插件窗口与DIV 窗口重叠</td></tr><tr><td>options</td><td>可选参数对象</td><td>否</td><td>详见示例。非iframe 对接场景无需填充此参数，iframe 对接 需填充此参数。iframe对接场景该参数使用请参照</td></tr></table>

Promise。接口成功说明创建插件窗口成功，失败则说明创建插件窗口失败

使用示例

```javascript
oWebControl.JS_CreateWnd("playWnd", 600, 400, {
cbSetDocTitle: function (uuid){ // uuid 为插件提供的 UUID
}
}).then(function(){ // oWebControl 为 WebControl 的对象
// 创建插件窗口成功
},function(){
// 创建插件窗口失败
});
 特殊使用场景说明
无
```
