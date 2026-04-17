# 3.4 JS_Resize 调整插件窗口大小、位置接口


接口定义

JS_Resize(iWidth, iHeight)

功能说明

插件窗口无法感知DIV 窗口的大小、位置变化，通过此接口来调整插件窗口大小与位置

使用条件

创建插件顶层窗口后，前端 DIV窗口resize、页面 scroll事件触发时都需调此接口来调整插件窗

口大小与位置

入参

<table><tr><td rowspan=1 colspan=1>名称</td><td rowspan=1 colspan=1>描述</td><td rowspan=1 colspan=1>是否必填</td><td rowspan=1 colspan=1>备注</td></tr><tr><td rowspan=1 colspan=1>iWidth</td><td rowspan=1 colspan=1>DIV窗口宽度</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1></td></tr><tr><td rowspan=1 colspan=1>iHeight</td><td rowspan=1 colspan=1>DIV窗口高度</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1></td></tr></table>

返回值

无

使用示例

oWebControl.JS_Resize(700, 500); // oWebControl 为 WebControl 的对象

特殊使用场景说明

无
