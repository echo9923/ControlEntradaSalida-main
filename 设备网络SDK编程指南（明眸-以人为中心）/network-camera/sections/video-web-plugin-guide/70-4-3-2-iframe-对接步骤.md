# 4.3.2 iframe 对接步骤


对于父页面，对接步骤如下（请结合 demo_for_iframe.html，搜索“步骤”关键字）：

1、 demo_for_iframe.html 中 iframe 标签指定待嵌入页面，并禁用滚动条。详情请参考demo_for_iframe.html 中步骤 1。

2、 子页面的 onload 事件中向嵌入的页面通过消息的形式设置一些子页面的初始值。详情请参考 demo_for_iframe.html 中步骤 2。

3、 监听子页面的消息。详情请参考 demo_for_iframe.html 中步骤 3。

4、监听本页面的 resize 事件，在事件响应中更新子页面一些值，使子页面触发插件窗口位置更新。详情请参考 demo_for_iframe.html 中步骤 4。

5、监听本页面的 scroll 事件，在事件响应中更新子页面一些值，使子页面触发插件窗口位置更新。详情请参考 demo_for_iframe.html 中步骤 5。

对于子页面，对接步骤如下（请结合 demo_embedded_for_iframe，搜索“步骤”关键字）：

1、 监听父页面的消息，并对消息作出响应。详情请参考 demo_embedded_for_iframe 中步骤 1。

2、 创建插件窗口（JS_CreateWnd）时指定 cbSetDocTitle 回调，并在回调中通知父页面更新其标题为回调给出的 uuid）。详情请参考 demo_embedded_for_iframe 中步骤 2。

3、创 建 插 件 成 功 后 通 知 父 页 面 将 其 标 题 更 新 回 去 。 详 情 请 参 考demo_embedded_for_iframe 中步骤 3。

4、 创建插件成功后通知父页面更新滚动条偏移量（为兼容嵌入子页面时父页面滚动条已有偏移量情况下插件窗口与 DIV 窗口无法贴合问题，父页面收到此消息需要将滚动条偏移量通知给子页面，子页面监听到此消息后更新插件窗口位置）。详情请参考demo_embedded_for_iframe 中步骤 4。
