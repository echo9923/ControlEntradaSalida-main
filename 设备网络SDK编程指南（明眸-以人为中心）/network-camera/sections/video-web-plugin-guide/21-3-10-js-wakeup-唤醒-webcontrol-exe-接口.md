# 3.10 JS_WakeUp 唤醒 WebControl.exe 接口


接口定义

JS_WakeUp(szProtocal)

功能说明

当 WebControl.exe 未启动时唤醒 WebControl.exe。若 WebControl.exe 已启动则忽略

使用条件

VideoWebPlugin.exe 已正确安装，若 Webcontrol.exe 进程异常退出或者 Webcontrol 进程连接失败后，可以使用该接口启动 Webcontrol 进程。Webcontrol 的静态方法，无需实例化，直接调用即可，

详见4.2.3启动插件

入参
<table><tr><td rowspan=1 colspan=1>名称</td><td rowspan=1 colspan=1>描述</td><td rowspan=1 colspan=1>是否必填</td><td rowspan=1 colspan=1>备注</td></tr><tr><td rowspan=1 colspan=1>szProtocal</td><td rowspan=1 colspan=1>唤醒协议</td><td rowspan=1 colspan=1>是</td><td rowspan=1 colspan=1>由 VideoWebPlugin.exe 安装时写入注册的唤醒协议，固定为&quot;VideoWebPlugin://&quot;</td></tr></table>

返回值

无

使用示例

WebControl.JS_WakeUp("VideoWebPlugin://") //详见 3.1 创建插件实例

特殊使用场景说明

无
