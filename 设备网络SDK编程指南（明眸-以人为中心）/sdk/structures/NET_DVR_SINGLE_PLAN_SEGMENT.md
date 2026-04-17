# NET_DVR_SINGLE_PLAN_SEGMENT

- 来源：[https://open.hikvision.com/hardware/structures/NET_DVR_SINGLE_PLAN_SEGMENT.html](https://open.hikvision.com/hardware/structures/NET_DVR_SINGLE_PLAN_SEGMENT.html)

计划参数结构体。

## 语法

```c
struct{
  BYTE                    byEnable;
  BYTE                    byDoorStatus;
  BYTE                    byVerifyMode;
  BYTE                    byRes[5];
  NET_DVR_TIME_SEGMENT    struTimeSegment;
}NET_DVR_SINGLE_PLAN_SEGMENT,*LPNET_DVR_SINGLE_PLAN_SEGMENT;
```

## Members

- `byEnable`：是否使能：0- 否，1- 是
- `byDoorStatus`：门状态模式（门状态或者梯控计划参数配置时使用）：0- 无效，1- 休眠，2- 常开状态（梯控的自由状态），3- 常闭状态（梯控的禁用状态）
- `byVerifyMode`：验证方式：0- 无效，1- 休眠，2- 刷卡+密码(读卡器验证方式计划使用)，3- 刷卡(读卡器验证方式计划使用)，4- 刷卡或密码(读卡器验证方式计划使用)，5- 指纹，6- 指纹+密码，7- 指纹或刷卡，8- 指纹+刷卡，9- 指纹+刷卡+密码（无先后顺序），10- 人脸或指纹或刷卡或密码，11- 人脸+指纹，12- 人脸+密码，13- 人脸+刷卡，14- 人脸，15- 工号+密码，16- 指纹或密码，17- 工号+指纹，18- 工号+指纹+密码，19- 人脸+指纹+刷卡，20- 人脸+密码+指纹，21- 工号+人脸，22- 人脸或人脸+刷卡
- `byRes`：保留，置为0
- `struTimeSegment`：计划时间段，包括开始时间和结束时间

## See Also

NET_DVR_WEEK_PLAN_CFG   NET_DVR_HOLIDAY_PLAN_CFG

## 相关链接

- [NET_DVR_TIME_SEGMENT](../structures/NET_DVR_TIME_SEGMENT.md)
- [NET_DVR_WEEK_PLAN_CFG](../structures/NET_DVR_WEEK_PLAN_CFG.md)
- [NET_DVR_HOLIDAY_PLAN_CFG](../structures/NET_DVR_HOLIDAY_PLAN_CFG.md)
