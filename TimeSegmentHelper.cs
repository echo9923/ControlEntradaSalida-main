using System;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 时段管理辅助类 - 提供时间校验、格式化和操作功能
    /// </summary>
    public static class TimeSegmentHelper
    {
        /// <summary>
        /// 校验时间段是否有效（结束时间必须大于开始时间）
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>时间段是否有效</returns>
        public static bool ValidateTimeSegment(DateTime startTime, DateTime endTime)
        {
            return endTime > startTime;
        }

        /// <summary>
        /// 检查新时段是否与现有时段冲突
        /// </summary>
        /// <param name="newStart">新时段开始时间</param>
        /// <param name="newEnd">新时段结束时间</param>
        /// <param name="existingSegments">现有时段列表</param>
        /// <param name="excludeIndex">排除检查的时段索引（用于编辑时段）</param>
        /// <returns>是否存在冲突</returns>
        public static bool CheckTimeConflict(DateTime newStart, DateTime newEnd, 
            HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[] existingSegments, int excludeIndex = -1)
        {
            for (int i = 0; i < HCNetSDK.MAX_TIMESEGMENT_V30; i++)
            {
                if (i == excludeIndex || existingSegments[i].byEnable == 0) 
                    continue;

                var existStart = TimeFromHCStruct(existingSegments[i].struTimeSegment.struBeginTime);
                var existEnd = TimeFromHCStruct(existingSegments[i].struTimeSegment.struEndTime);

                // 检查时间段是否重叠
                if (!(newEnd <= existStart || newStart >= existEnd))
                {
                    return true; // 存在冲突
                }
            }
            return false; // 无冲突
        }

        /// <summary>
        /// 从HCNetSDK时间结构转换为DateTime
        /// </summary>
        public static DateTime TimeFromHCStruct(HCNetSDK.NET_DVR_SIMPLE_DAYTIME hcTime)
        {
            var hour = hcTime.byHour == 24 ? 0 : hcTime.byHour;
            var minute = hcTime.byHour == 24 ? 0 : hcTime.byMinute;
            
            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 
                hour, minute, 0);
        }

        /// <summary>
        /// 格式化时间为HH:mm格式
        /// </summary>
        public static string FormatTime(HCNetSDK.NET_DVR_SIMPLE_DAYTIME time)
        {
            var hour = time.byHour == 24 ? 0 : time.byHour;
            var minute = time.byHour == 24 ? 0 : time.byMinute;
            return string.Format("{0:D2}:{1:D2}", hour, minute);
        }

        /// <summary>
        /// 提供默认的工作日时段模板
        /// </summary>
        public static void ApplyWorkdayTemplate(HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[] segments, int dayOffset)
        {
            // 清除现有时段
            for (int i = 0; i < HCNetSDK.MAX_TIMESEGMENT_V30; i++)
            {
                segments[dayOffset * HCNetSDK.MAX_TIMESEGMENT_V30 + i].byEnable = 0;
            }

            // 设置工作日模板：09:00-12:00, 13:30-18:00
            var morningSegment = segments[dayOffset * HCNetSDK.MAX_TIMESEGMENT_V30];
            morningSegment.byEnable = 1;
            morningSegment.byDoorStatus = 3; // 常闭
            morningSegment.struTimeSegment.struBeginTime.byHour = 9;
            morningSegment.struTimeSegment.struBeginTime.byMinute = 0;
            morningSegment.struTimeSegment.struBeginTime.bySecond = 0;
            morningSegment.struTimeSegment.struEndTime.byHour = 12;
            morningSegment.struTimeSegment.struEndTime.byMinute = 0;
            morningSegment.struTimeSegment.struEndTime.bySecond = 0;

            var afternoonSegment = segments[dayOffset * HCNetSDK.MAX_TIMESEGMENT_V30 + 1];
            afternoonSegment.byEnable = 1;
            afternoonSegment.byDoorStatus = 3; // 常闭
            afternoonSegment.struTimeSegment.struBeginTime.byHour = 13;
            afternoonSegment.struTimeSegment.struBeginTime.byMinute = 30;
            afternoonSegment.struTimeSegment.struBeginTime.bySecond = 0;
            afternoonSegment.struTimeSegment.struEndTime.byHour = 18;
            afternoonSegment.struTimeSegment.struEndTime.byMinute = 0;
            afternoonSegment.struTimeSegment.struEndTime.bySecond = 0;
        }

        /// <summary>
        /// 显示友好的错误提示信息
        /// </summary>
        public static void ShowTimeConflictMessage(int conflictIndex)
        {
            MessageBox.Show($"时间段冲突！新时段与第 {conflictIndex + 1} 个时段存在重叠。\n\n请调整时间避免冲突。", 
                "时间冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 显示时间无效的提示信息
        /// </summary>
        public static void ShowInvalidTimeMessage()
        {
            MessageBox.Show("时间段无效！结束时间必须晚于开始时间。", 
                "时间错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}