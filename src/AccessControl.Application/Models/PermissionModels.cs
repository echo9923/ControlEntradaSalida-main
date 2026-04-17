using System;

namespace ControlEntradaSalida.Application.Models
{
    public sealed class PermissionUpdateCommandItem
    {
        public PermissionUpdateCommandItem(string employeeId, int permissionCode)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException(nameof(employeeId));
            }

            EmployeeId = employeeId.Trim();
            PermissionCode = permissionCode;
        }

        public string EmployeeId { get; }

        public int PermissionCode { get; }
    }

    public sealed class PersonSyncCommandItem
    {
        public string EmployeeId { get; set; }

        public string FullName { get; set; }

        public string Gender { get; set; }

        public bool Enabled { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public byte[] FaceImageBytes { get; set; }

        public string FaceImageFormat { get; set; }
    }

    public sealed class EnrollmentStatusQuery
    {
        public EnrollmentStatusQuery(string taskId)
        {
            TaskId = taskId;
        }

        public string TaskId { get; }
    }

    public sealed class CaptureFaceStreamCommand
    {
        public CaptureFaceStreamCommand(string employeeId)
        {
            EmployeeId = employeeId;
        }

        public string EmployeeId { get; }
    }
}
