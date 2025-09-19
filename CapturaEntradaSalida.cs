using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    //实时接收、显示和记录出入事件 - 异步优化版本
    //严格按照海康威视SDK编程指南实现轻量级事件处理机制
    public partial class CapturaEntradaSalida : Form
    {

        private readonly AccessEventService eventService = AccessEventService.Instance;


        // 初始化窗体
        public CapturaEntradaSalida()
        {
            InitializeComponent();
        }



        private ListViewItem CreateListViewItem(AccessLogEvent eventData, bool allowPlaceholderForMissingName)
        {
            if (eventData == null)
            {
                return null;
            }

            bool isPersonRelated = AccessEventFormatter.IsPersonRelatedEvent(eventData.EventType);
            string displayEmployeeNumber = isPersonRelated ? (eventData.EmployeeNumber ?? string.Empty) : string.Empty;
            string displayEmployeeName = string.Empty;

            if (isPersonRelated)
            {
                if (!string.IsNullOrWhiteSpace(eventData.EmployeeName))
                {
                    displayEmployeeName = eventData.EmployeeName;
                }
                else if (allowPlaceholderForMissingName)
                {
                    displayEmployeeName = "查询中...";
                }
            }

            string eventTypeDisplay = string.IsNullOrWhiteSpace(eventData.EventTypeDisplay)
                ? AccessEventFormatter.TranslateEventType(eventData.EventType)
                : eventData.EventTypeDisplay;

            ListViewItem item = new ListViewItem(eventData.SequenceNumber.ToString());
            item.SubItems.Add(displayEmployeeNumber);
            item.SubItems.Add(displayEmployeeName);
            item.SubItems.Add(eventData.DeviceNumber.ToString());
            item.SubItems.Add(eventData.DeviceName ?? string.Empty);
            item.SubItems.Add(eventTypeDisplay);
            item.SubItems.Add(eventData.EventTime.ToString("yyyy-MM-dd HH:mm:ss"));
            item.SubItems.Add(eventData.RemoteHostAddress ?? string.Empty);

            return item;
        }

        private void UpdateUIImmediately(AccessLogEvent eventData)
        {
            try
            {
                SafeUIUpdater.UpdateUI(this.listViewEventos, () =>
                {
                    ListViewItem item = CreateListViewItem(eventData, true);
                    if (item == null)
                    {
                        return;
                    }

                    // 将最新事件插入列表顶部
                    this.listViewEventos.Items.Insert(0, item);

                    if (this.listViewEventos.Items.Count > 0)
                    {
                        this.listViewEventos.EnsureVisible(0);
                    }

                    const int maxItems = 1000;
                    if (this.listViewEventos.Items.Count > maxItems)
                    {
                        for (int i = this.listViewEventos.Items.Count - 1; i >= maxItems; i--)
                        {
                            this.listViewEventos.Items.RemoveAt(i);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UI 更新异常: {ex.Message}");
            }
        }

        private void PopulateHistoryEvents(List<AccessLogEvent> historyEvents)
        {
            if (historyEvents == null || historyEvents.Count == 0)
            {
                return;
            }

            SafeUIUpdater.UpdateUI(this.listViewEventos, () =>
            {
                this.listViewEventos.BeginUpdate();
                try
                {
                    this.listViewEventos.Items.Clear();
                    foreach (AccessLogEvent eventData in historyEvents)
                    {
                        ListViewItem item = CreateListViewItem(eventData, false);
                        if (item != null)
                        {
                            this.listViewEventos.Items.Add(item);
                        }
                    }

                    if (this.listViewEventos.Items.Count > 0)
                    {
                        this.listViewEventos.EnsureVisible(0);
                    }
                }
                finally
                {
                    this.listViewEventos.EndUpdate();
                }
            });
        }

        private async void GestionEventos_Load(object sender, EventArgs e)
        {
            try
            {
                eventService.AccessEventReceived += OnAccessEventReceived;

                long maxSequence = await LoadRecentHistoryAsync();
                if (maxSequence > 0)
                {
                    eventService.AlignSequenceNumber(maxSequence);
                }

                await eventService.EnsureStartedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"窗体初始化异常: {ex.Message}", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnAccessEventReceived(object sender, AccessEventReceivedEventArgs e)
        {
            if (e == null || e.AccessEvent == null)
            {
                return;
            }

            UpdateUIImmediately(e.AccessEvent);
        }
        
        /// <summary>
        /// 加载最近的门禁历史记录
        /// </summary>
        private async Task<long> LoadRecentHistoryAsync()
        {
            long maxSequence = 0;

            try
            {
                Common cmn = new Common();
                string connectionString = cmn.obtenerCadenaConexion();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Console.WriteLine("[WARNING] 未找到数据库连接字符串，跳过历史记录加载");
                    return 0;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT sequence_number, employee_number, employee_name, device_number, device_name, event_type, event_time, remote_host_address
FROM access_logs
ORDER BY sequence_number DESC
LIMIT 100";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        List<AccessLogEvent> historyEvents = new List<AccessLogEvent>();

                        int sequenceOrdinal = reader.GetOrdinal("sequence_number");
                        int employeeNumberOrdinal = reader.GetOrdinal("employee_number");
                        int employeeNameOrdinal = reader.GetOrdinal("employee_name");
                        int deviceNumberOrdinal = reader.GetOrdinal("device_number");
                        int deviceNameOrdinal = reader.GetOrdinal("device_name");
                        int eventTypeOrdinal = reader.GetOrdinal("event_type");
                        int eventTimeOrdinal = reader.GetOrdinal("event_time");
                        int remoteHostOrdinal = reader.GetOrdinal("remote_host_address");

                        while (await reader.ReadAsync())
                        {
                            long sequenceNumber = reader.IsDBNull(sequenceOrdinal) ? 0L : reader.GetInt64(sequenceOrdinal);
                            if (sequenceNumber > maxSequence)
                            {
                                maxSequence = sequenceNumber;
                            }

                            string eventTypeCode = reader.IsDBNull(eventTypeOrdinal) ? string.Empty : reader.GetString(eventTypeOrdinal);

                            AccessLogEvent eventData = new AccessLogEvent
                            {
                                SequenceNumber = sequenceNumber,
                                EmployeeNumber = reader.IsDBNull(employeeNumberOrdinal) ? string.Empty : reader.GetString(employeeNumberOrdinal),
                                EmployeeName = reader.IsDBNull(employeeNameOrdinal) ? string.Empty : reader.GetString(employeeNameOrdinal),
                                DeviceNumber = reader.IsDBNull(deviceNumberOrdinal) ? 0 : reader.GetInt32(deviceNumberOrdinal),
                                DeviceName = reader.IsDBNull(deviceNameOrdinal) ? string.Empty : reader.GetString(deviceNameOrdinal),
                                EventType = eventTypeCode,
                                EventTypeDisplay = AccessEventFormatter.TranslateEventType(eventTypeCode),
                                EventTime = reader.IsDBNull(eventTimeOrdinal) ? DateTime.Now : reader.GetDateTime(eventTimeOrdinal),
                                RemoteHostAddress = reader.IsDBNull(remoteHostOrdinal) ? string.Empty : reader.GetString(remoteHostOrdinal)
                            };

                            historyEvents.Add(eventData);
                        }

                        if (historyEvents.Count > 0)
                        {
                            PopulateHistoryEvents(historyEvents);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] 加载历史记录失败: {ex.Message}");
            }

            return maxSequence;
        }



        /// <summary>
        /// 获取异步处理统计信息
        /// </summary>
        public string GetAsyncProcessingStats()
        {
            return eventService.GetStatistics();
        }

        //设备状态变化事件处理


        //窗体关闭前关闭报警通道 NET_DVR_CloseAlarmChan() 并停止异步处理器
        private void GestionEventos_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                eventService.AccessEventReceived -= OnAccessEventReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 窗体关闭处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止异步数据库写入器
        /// </summary>


        //
        private void listViewEventos_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }


}


























