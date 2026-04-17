# 3.12 JS_RequestInterface 通用请求响应接口


Js_RequestInterface 是通用请求接口，用于完成各种功能，功能参数由其 json 报文参数决定。json报文参数格式：

funcName: "funcName", // 功能标识，详见下表  
argument: "argument" // 功能标识的参数，如果无参数可不传

例如，对申请 RSA 公钥的请求 json 报文如下：

 通用请求接口返回 json 报文，只需要关注 responseMsg 信息，其格式如下：

版权所有©杭州海康威视数字技术股份有限公司2020

code: 0, // 错误码 0-成功 1-失败  
msg: "invalid param", // 错误描述，仅当 errorCode 非0 时才有错误描述  
data: "" // 返回的数据，如 RSA公钥

例如，对申请 RSA公钥请求的响应，报文如下：

```textproto
{
code: 0, // 错误码 0-成功 1-失败
data: "{
rsaPubKey: "MIBISFNUKEMGGNJSIGWMFIGGIMWUEIGOMWIUFIEMGKOMGJ" // RSA 公钥
}"
}
```
