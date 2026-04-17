# Members


- `byEnable`：是否启用该有效期：0- 不启用，1- 启用
- `byBeginTimeFlag`：是否限制起始时间的标志，0-不限制，1-限制
- `byEnableTimeFlag`：是否限制终止时间的标志，0-不限制，1-限制
- `byTimeDurationNo`：有效期索引,从0开始（时间段通过SDK设置给锁，后续在制卡时，只需要传递有效期索引即可，以减少数据量
- `struBeginTime`：有效期起始时间
- `struEndTime`：有效期结束时间
- `byRes2`：保留，置为0
