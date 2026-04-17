# 3.11 JS_SetDocOffset 设置 iframe 偏移量接口


接口定义

功能说明

iframe对接时，通过此接口来设置DIV 窗口与文档的偏移量，使达到插件窗口与DIV 窗口贴合的效果

iframe 对接场景。

<table><tr><td rowspan=1 colspan=1>名称</td><td rowspan=1 colspan=1>描述</td><td rowspan=1 colspan=1>是否必填</td><td rowspan=1 colspan=1>备注</td></tr><tr><td rowspan=1 colspan=1>offset</td><td rowspan=1 colspan=1>当iframe嵌套时，需要传入该 iframe 相对最顶层窗口的位置</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1>形式：{left: 100,top: 100}</td></tr></table>

返回值

WebControl.JS_SetDocOffset ({  
left: 100,  
top: 100  
}) //详见 3.1 创建插件实例

特殊使用场景说明

iframe 对接场景
