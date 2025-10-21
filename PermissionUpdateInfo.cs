using System;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 表示通过外部渠道触发的权限更新指令。
    /// </summary>
    public class PermissionUpdateInfo
    {
        public string EmployeeId { get; }

        public int PermissionCode { get; }

        public PermissionUpdateInfo(string employeeId, int permissionCode)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("员工工号不能为空。", nameof(employeeId));
            }

            EmployeeId = employeeId.Trim();
            PermissionCode = permissionCode;
        }
    }
}
