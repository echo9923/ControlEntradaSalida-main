# 3.6 JS_RepairPartWindow 扣除插件窗口还原接口


接口定义

JS_RepairPartWindow(iLeft, iTop, iWidth, iHeight)

功能说明

还原扣除部分窗口后的插件窗口

使用条件

和 3.5 中的接口配合使用，当需要完全显示插件窗口时，使用该接口显示已经隐藏的部分插件窗口。

入参
<table><tr><td rowspan=1 colspan=1>名称</td><td rowspan=1 colspan=1>描述</td><td rowspan=1 colspan=1>是否必填</td><td rowspan=1 colspan=1>备注</td></tr><tr><td rowspan=1 colspan=1>iLeft</td><td rowspan=1 colspan=1>扣除窗口的顶点距离插件窗口左边距</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1></td></tr><tr><td rowspan=1 colspan=1>iTop</td><td rowspan=1 colspan=1>扣除窗口的顶点距离插件窗口上边距</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1></td></tr><tr><td rowspan=1 colspan=1>iWidth</td><td rowspan=1 colspan=1>扣除窗口的宽度</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1></td></tr><tr><td rowspan=1 colspan=1>iHeight</td><td rowspan=1 colspan=1>扣除窗口的高度</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1></td></tr></table>

返回值

无

使用示例

oWebControl.JS_RepairPartWindow(0, 0, 100, 100); // oWebControl 为 WebControl 的对象

特殊使用场景说明

无
