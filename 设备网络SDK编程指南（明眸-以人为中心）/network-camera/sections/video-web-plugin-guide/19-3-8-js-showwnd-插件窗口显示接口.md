# 3.8 JS_ShowWnd 插件窗口显示接口


接口定义

JS_ShowWnd()

功能说明

显示插件窗口

使用条件

插件窗口隐藏后可调此接口来显示

入参

无

返回值

无

使用示例

oWebControl.JS_ShowWnd(); // oWebControl 为 WebControl 的对象

特殊使用场景说明

一个浏览器页面使用多个 DIV窗口加载了多个插件窗口时，插件窗口全屏后其它窗口会处于全屏窗口上，针对此场景如下处理：

 JS_SetWindowControlCallback 设置的消息回调中监听窗口全屏事件

 监听到窗口全屏事件时调JS_HideWnd插件窗口隐藏接口对除接收到全屏事件的插件窗口外的其它窗口隐藏

 监听到窗口退出全屏事件时调 JS_ShowWnd 插件窗口显示窗口对接收到退出全屏事件的插件窗口外的其它窗口显示
