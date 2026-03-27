using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备写操作离线补偿状态持久化。
    /// </summary>
    public sealed class DeviceOperationRetryStore
    {
        private const string TableName = "dbo.device_operation_retry_states";

        private readonly Common commonHelper;
        private readonly int commandTimeoutSeconds;

        public DeviceOperationRetryStore()
        {
            commonHelper = new Common();
            commandTimeoutSeconds = commonHelper.obtenerTiempoEsperaComando();
        }

        public List<DeviceOperationRetryState> LoadPendingStates(
            DateTime now,
            int? deviceId = null,
            bool ignoreNextRetry = false,
            bool includeExhausted = false)
        {
            List<DeviceOperationRetryState> states = new List<DeviceOperationRetryState>();

            using (SqlServerDatabase db = OpenDatabase())
            {
                string sql = $@"SELECT device_id,
                                       employee_id,
                                       permission_level,
                                       permission_pending,
                                       permission_sync_completion_blocked,
                                       person_payload,
                                       person_pending,
                                       face_payload,
                                       face_pending,
                                       delete_person_pending,
                                       delete_face_pending,
                                       attempt_count,
                                       next_retry_at,
                                       last_error,
                                       last_attempt_at,
                                       exhausted_at,
                                       created_at,
                                       updated_at
                                FROM {TableName}
                                WHERE (permission_pending = 1
                                       OR person_pending = 1
                                       OR face_pending = 1
                                       OR delete_person_pending = 1
                                       OR delete_face_pending = 1)
                                  AND (@includeExhausted = 1 OR exhausted_at IS NULL)
                                  AND (@deviceId IS NULL OR device_id = @deviceId)
                                  AND (
                                        @ignoreNextRetry = 1
                                        OR next_retry_at IS NULL
                                        OR next_retry_at <= @now
                                      )
                                ORDER BY device_id ASC, updated_at ASC";

                using (SqlCommand cmd = db.CreateCommand(sql))
                {
                    cmd.Parameters.AddWithValue("@deviceId", (object)deviceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@includeExhausted", includeExhausted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@ignoreNextRetry", ignoreNextRetry ? 1 : 0);
                    cmd.Parameters.AddWithValue("@now", now);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            states.Add(MapState(reader));
                        }
                    }
                }
            }

            return states;
        }

        public DeviceOperationRetryState GetState(int deviceId, string employeeId)
        {
            if (deviceId <= 0 || string.IsNullOrWhiteSpace(employeeId))
            {
                return null;
            }

            using (SqlServerDatabase db = OpenDatabase())
            {
                using (SqlCommand cmd = db.CreateCommand($@"SELECT TOP (1) device_id,
                                                                    employee_id,
                                                                    permission_level,
                                                                    permission_pending,
                                                                    permission_sync_completion_blocked,
                                                                    person_payload,
                                                                    person_pending,
                                                                    face_payload,
                                                                    face_pending,
                                                                    delete_person_pending,
                                                                    delete_face_pending,
                                                                    attempt_count,
                                                                    next_retry_at,
                                                                    last_error,
                                                                    last_attempt_at,
                                                                    exhausted_at,
                                                                    created_at,
                                                                    updated_at
                                                             FROM {TableName}
                                                             WHERE device_id = @deviceId
                                                               AND employee_id = @employeeId"))
                {
                    cmd.Parameters.AddWithValue("@deviceId", deviceId);
                    cmd.Parameters.AddWithValue("@employeeId", employeeId.Trim());

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapState(reader) : null;
                    }
                }
            }
        }

        public bool HasPendingPermission(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return false;
            }

            using (SqlServerDatabase db = OpenDatabase())
            {
                return HasPendingPermission(db, null, employeeId.Trim());
            }
        }

        public void AllowPermissionSyncCompletion(string employeeId, int permissionLevel)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("\u5458\u5DE5\u5DE5\u53F7\u4E0D\u80FD\u4E3A\u7A7A\u3002", nameof(employeeId));
            }

            string normalizedEmployeeId = employeeId.Trim();

            using (SqlServerDatabase db = OpenDatabase())
            using (SqlCommand cmd = db.CreateCommand($@"UPDATE {TableName}
                                                       SET permission_sync_completion_blocked = 0,
                                                           updated_at = SYSDATETIME()
                                                       WHERE employee_id = @employeeId
                                                         AND permission_pending = 1
                                                         AND exhausted_at IS NULL"))
            {
                cmd.Parameters.AddWithValue("@employeeId", normalizedEmployeeId);
                cmd.ExecuteNonQuery();

                if (!HasPendingPermission(db, null, normalizedEmployeeId)
                    && !UpdateSyncedLevel(db, null, normalizedEmployeeId, permissionLevel))
                {
                    throw new InvalidOperationException($"\u66f4\u65b0\u5458\u5de5 {normalizedEmployeeId} \u7684\u6743\u9650\u540c\u6b65\u6807\u8bb0\u5931\u8d25\u3002");
                }
            }
        }

        public void RemoveState(int deviceId, string employeeId)
        {
            if (deviceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("员工工号不能为空。", nameof(employeeId));
            }

            string normalizedEmployeeId = employeeId.Trim();

            using (SqlServerDatabase db = OpenDatabase())
            using (SqlTransaction transaction = db.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                DeleteState(db, transaction, deviceId, normalizedEmployeeId);
                transaction.Commit();
            }
        }

        public void CleanupExpiredFailures(int failureRetentionDays)
        {
            int retentionDays = Math.Max(1, failureRetentionDays);
            DateTime cutoff = DateTime.Now.AddDays(-retentionDays);

            using (SqlServerDatabase db = OpenDatabase())
            using (SqlCommand cmd = db.CreateCommand($@"DELETE FROM {TableName}
                                                       WHERE exhausted_at IS NOT NULL
                                                         AND exhausted_at <= @cutoff"))
            {
                cmd.Parameters.AddWithValue("@cutoff", cutoff);
                cmd.ExecuteNonQuery();
            }
        }

        public void QueuePermissionRetry(int deviceId, string employeeId, int permissionLevel, string message)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.PermissionPending = true;
                state.PermissionLevel = permissionLevel;
                state.PermissionSyncCompletionBlocked = true;
                state.DeleteFacePending = false;
                state.DeletePersonPending = false;
                ResetRetryState(state, message);
            });
        }

        public void QueuePersonRetry(int deviceId, PersonSyncRequest person, string message)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            string personPayload = SerializePersonPayload(person);
            string facePayload = person.HasFace ? SerializeFacePayload(person) : null;

            UpdateState(deviceId, person.EmployeeId, state =>
            {
                DeviceOperationRetryStateBehavior.ApplyQueuedPersonRetry(state,
                    personPayload,
                    person.HasFace,
                    facePayload);
                ResetRetryState(state, message);
            });
        }

        public void QueueFaceRetry(int deviceId, PersonSyncRequest person, string message)
        {
            if (person == null || !person.HasFace)
            {
                return;
            }

            UpdateState(deviceId, person.EmployeeId, state =>
            {
                state.FacePending = true;
                state.FacePayload = SerializeFacePayload(person);
                state.DeleteFacePending = false;
                ResetRetryState(state, message);
            });
        }

        public void QueueDeleteFaceRetry(int deviceId, string employeeId, string message)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                DeviceOperationRetryStateBehavior.ApplyQueuedDeleteFaceRetry(state);
                ResetRetryState(state, message);
            });
        }

        public void QueueDeletePersonRetry(int deviceId, string employeeId, string message, bool deleteFacePending = true)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.PermissionPending = false;
                state.PermissionLevel = null;
                state.PermissionSyncCompletionBlocked = false;
                state.PersonPending = false;
                state.PersonPayload = null;
                state.FacePending = false;
                state.FacePayload = null;
                state.DeleteFacePending = deleteFacePending;
                state.DeletePersonPending = true;
                ResetRetryState(state, message);
            });
        }

        public void MarkPermissionApplied(int deviceId, string employeeId)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.PermissionPending = false;
                state.PermissionLevel = null;
                state.PermissionSyncCompletionBlocked = false;
                state.DeletePersonPending = false;
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void MarkPersonApplied(int deviceId, string employeeId)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.PersonPending = false;
                state.PersonPayload = null;
                state.DeletePersonPending = false;
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void MarkPersonAppliedAndClearFaceRetry(int deviceId, string employeeId)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                DeviceOperationRetryStateBehavior.ApplyPersonSuccessAndClearFaceRetry(state);
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void MarkPersonAndFaceApplied(int deviceId, string employeeId)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.PersonPending = false;
                state.PersonPayload = null;
                state.FacePending = false;
                state.FacePayload = null;
                state.DeletePersonPending = false;
                state.DeleteFacePending = false;
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void MarkFaceApplied(int deviceId, string employeeId)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.FacePending = false;
                state.FacePayload = null;
                state.DeleteFacePending = false;
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void MarkDeleteFaceApplied(int deviceId, string employeeId, bool clearDeletePersonPending = false)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                DeviceOperationRetryStateBehavior.ApplyDeleteFaceSuccess(state, clearDeletePersonPending);
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void MarkDeletePersonApplied(int deviceId, string employeeId)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                state.PermissionPending = false;
                state.PermissionLevel = null;
                state.PermissionSyncCompletionBlocked = false;
                state.PersonPending = false;
                state.PersonPayload = null;
                state.FacePending = false;
                state.FacePayload = null;
                state.DeleteFacePending = false;
                state.DeletePersonPending = false;
                ResetPendingStateAfterSuccess(state);
            });
        }

        public void ScheduleRetry(int deviceId, string employeeId, string errorMessage, int retryIntervalSeconds, int maxRetryAttempts)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                DateTime now = DateTime.Now;
                int attempts = state.AttemptCount + 1;
                state.AttemptCount = attempts;
                state.LastAttemptAt = now;
                state.LastError = errorMessage;

                if (attempts >= Math.Max(1, maxRetryAttempts))
                {
                    state.ExhaustedAt = now;
                    state.NextRetryAt = null;
                    return;
                }

                state.ExhaustedAt = null;
                state.NextRetryAt = now.AddSeconds(Math.Max(1, retryIntervalSeconds));
            });
        }

        public void MarkTerminalFailure(int deviceId, string employeeId, string errorMessage, int maxRetryAttempts)
        {
            UpdateState(deviceId, employeeId, state =>
            {
                DateTime now = DateTime.Now;
                state.AttemptCount = Math.Max(Math.Max(1, maxRetryAttempts), state.AttemptCount + 1);
                state.LastAttemptAt = now;
                state.LastError = errorMessage;
                state.ExhaustedAt = now;
                state.NextRetryAt = null;
            });
        }

        internal PermissionRetryCommitResult CompletePermissionRetry(int deviceId, string employeeId, int permissionLevel)
        {
            if (deviceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("?????????", nameof(employeeId));
            }

            string normalizedEmployeeId = employeeId.Trim();

            using (SqlServerDatabase db = OpenDatabase())
            using (SqlTransaction transaction = db.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    DeviceOperationRetryState state = LoadState(db, transaction, deviceId, normalizedEmployeeId);
                    if (state == null || !state.PermissionPending)
                    {
                        transaction.Commit();
                        return PermissionRetryCommitResult.Completed;
                    }

                    bool canCompleteSync = !state.PermissionSyncCompletionBlocked;

                    state.PermissionPending = false;
                    state.PermissionLevel = null;
                    state.PermissionSyncCompletionBlocked = false;
                    state.DeletePersonPending = false;
                    ResetPendingStateAfterSuccess(state);

                    if (state.HasPendingOperations)
                    {
                        SaveState(db, transaction, state);
                    }
                    else
                    {
                        DeleteState(db, transaction, deviceId, normalizedEmployeeId);
                    }

                    if (canCompleteSync && !HasPendingPermission(db, transaction, normalizedEmployeeId))
                    {
                        if (!UpdateSyncedLevel(db, transaction, normalizedEmployeeId, permissionLevel))
                        {
                            transaction.Rollback();
                            return PermissionRetryCommitResult.RetryableFailure($"???? {normalizedEmployeeId} ??????????");
                        }
                    }

                    transaction.Commit();
                    return PermissionRetryCommitResult.Completed;
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                    }

                    return PermissionRetryCommitResult.RetryableFailure($"???? {normalizedEmployeeId} ??????????{ex.Message}");
                }
            }
        }

        private void UpdateState(int deviceId, string employeeId, Action<DeviceOperationRetryState> mutate)
        {
            if (deviceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("员工工号不能为空。", nameof(employeeId));
            }

            if (mutate == null)
            {
                throw new ArgumentNullException(nameof(mutate));
            }

            string normalizedEmployeeId = employeeId.Trim();

            using (SqlServerDatabase db = OpenDatabase())
            using (SqlTransaction transaction = db.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                DeviceOperationRetryState state = LoadState(db, transaction, deviceId, normalizedEmployeeId)
                    ?? new DeviceOperationRetryState
                    {
                        DeviceId = deviceId,
                        EmployeeId = normalizedEmployeeId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        NextRetryAt = DateTime.Now
                    };

                mutate(state);
                state.EmployeeId = normalizedEmployeeId;
                state.UpdatedAt = DateTime.Now;

                if (state.HasPendingOperations)
                {
                    if (!state.ExhaustedAt.HasValue && !state.NextRetryAt.HasValue)
                    {
                        state.NextRetryAt = DateTime.Now;
                    }

                    SaveState(db, transaction, state);
                }
                else
                {
                    DeleteState(db, transaction, deviceId, normalizedEmployeeId);
                }

                transaction.Commit();
            }
        }

        private static void ResetRetryState(DeviceOperationRetryState state, string message)
        {
            state.AttemptCount = 0;
            state.NextRetryAt = DateTime.Now;
            state.LastError = message;
            state.LastAttemptAt = null;
            state.ExhaustedAt = null;
        }

        private static void ResetPendingStateAfterSuccess(DeviceOperationRetryState state)
        {
            state.AttemptCount = 0;
            state.NextRetryAt = state.HasPendingOperations ? (DateTime?)DateTime.Now : null;
            state.LastError = null;
            state.LastAttemptAt = DateTime.Now;
            state.ExhaustedAt = null;
        }

        private static string SerializePersonPayload(PersonSyncRequest request)
        {
            PersonRetryPayload payload = new PersonRetryPayload
            {
                EmployeeId = request.EmployeeId,
                FullName = request.FullName,
                Gender = request.Gender,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                Enabled = request.Enabled
            };

            return JsonConvert.SerializeObject(payload);
        }

        private static string SerializeFacePayload(PersonSyncRequest request)
        {
            FaceRetryPayload payload = new FaceRetryPayload
            {
                EmployeeId = request.EmployeeId,
                FaceImageBytes = request.FaceImageBytes,
                FaceImageFormat = request.FaceImageFormat
            };

            return JsonConvert.SerializeObject(payload);
        }

        private DeviceOperationRetryState LoadState(SqlServerDatabase db, SqlTransaction transaction, int deviceId, string employeeId)
        {
            using (SqlCommand cmd = db.CreateCommand($@"SELECT TOP (1) device_id,
                                                           employee_id,
                                                           permission_level,
                                                           permission_pending,
                                                           permission_sync_completion_blocked,
                                                           person_payload,
                                                           person_pending,
                                                           face_payload,
                                                           face_pending,
                                                           delete_person_pending,
                                                           delete_face_pending,
                                                           attempt_count,
                                                           next_retry_at,
                                                           last_error,
                                                           last_attempt_at,
                                                           exhausted_at,
                                                           created_at,
                                                           updated_at
                                                    FROM {TableName} WITH (UPDLOCK, HOLDLOCK)
                                                    WHERE device_id = @deviceId
                                                      AND employee_id = @employeeId"))
            {
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@deviceId", deviceId);
                cmd.Parameters.AddWithValue("@employeeId", employeeId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapState(reader) : null;
                }
            }
        }

        private static DeviceOperationRetryState MapState(SqlDataReader reader)
        {
            return new DeviceOperationRetryState
            {
                DeviceId = Convert.ToInt32(reader["device_id"]),
                EmployeeId = reader["employee_id"].ToString(),
                PermissionLevel = reader["permission_level"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["permission_level"]),
                PermissionPending = Convert.ToBoolean(reader["permission_pending"]),
                PermissionSyncCompletionBlocked = Convert.ToBoolean(reader["permission_sync_completion_blocked"]),
                PersonPayload = reader["person_payload"] == DBNull.Value ? null : reader["person_payload"].ToString(),
                PersonPending = Convert.ToBoolean(reader["person_pending"]),
                FacePayload = reader["face_payload"] == DBNull.Value ? null : reader["face_payload"].ToString(),
                FacePending = Convert.ToBoolean(reader["face_pending"]),
                DeletePersonPending = Convert.ToBoolean(reader["delete_person_pending"]),
                DeleteFacePending = Convert.ToBoolean(reader["delete_face_pending"]),
                AttemptCount = Convert.ToInt32(reader["attempt_count"]),
                NextRetryAt = reader["next_retry_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["next_retry_at"]),
                LastError = reader["last_error"] == DBNull.Value ? null : reader["last_error"].ToString(),
                LastAttemptAt = reader["last_attempt_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["last_attempt_at"]),
                ExhaustedAt = reader["exhausted_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["exhausted_at"]),
                CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = reader["updated_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["updated_at"]),
                ExistsInDatabase = true
            };
        }

        private void SaveState(SqlServerDatabase db, SqlTransaction transaction, DeviceOperationRetryState state)
        {
            if (state.ExistsInDatabase)
            {
                using (SqlCommand cmd = db.CreateCommand($@"UPDATE {TableName}
                                                         SET permission_level = @permissionLevel,
                                                             permission_pending = @permissionPending,
                                                             permission_sync_completion_blocked = @permissionSyncCompletionBlocked,
                                                             person_payload = @personPayload,
                                                             person_pending = @personPending,
                                                             face_payload = @facePayload,
                                                             face_pending = @facePending,
                                                             delete_person_pending = @deletePersonPending,
                                                             delete_face_pending = @deleteFacePending,
                                                             attempt_count = @attemptCount,
                                                             next_retry_at = @nextRetryAt,
                                                             last_error = @lastError,
                                                             last_attempt_at = @lastAttemptAt,
                                                             exhausted_at = @exhaustedAt,
                                                             updated_at = @updatedAt
                                                         WHERE device_id = @deviceId
                                                           AND employee_id = @employeeId"))
                {
                    cmd.Transaction = transaction;
                    BindStateParameters(cmd, state, includeCreatedAt: false);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using (SqlCommand cmd = db.CreateCommand($@"INSERT INTO {TableName} (
                                                             device_id,
                                                             employee_id,
                                                             permission_level,
                                                             permission_pending,
                                                             permission_sync_completion_blocked,
                                                             person_payload,
                                                             person_pending,
                                                             face_payload,
                                                             face_pending,
                                                             delete_person_pending,
                                                             delete_face_pending,
                                                             attempt_count,
                                                             next_retry_at,
                                                             last_error,
                                                             last_attempt_at,
                                                             exhausted_at,
                                                             created_at,
                                                             updated_at)
                                                         VALUES (
                                                             @deviceId,
                                                             @employeeId,
                                                             @permissionLevel,
                                                             @permissionPending,
                                                             @permissionSyncCompletionBlocked,
                                                             @personPayload,
                                                             @personPending,
                                                             @facePayload,
                                                             @facePending,
                                                             @deletePersonPending,
                                                             @deleteFacePending,
                                                             @attemptCount,
                                                             @nextRetryAt,
                                                             @lastError,
                                                             @lastAttemptAt,
                                                             @exhaustedAt,
                                                             @createdAt,
                                                             @updatedAt)"))
                {
                    cmd.Transaction = transaction;
                    BindStateParameters(cmd, state, includeCreatedAt: true);
                    cmd.ExecuteNonQuery();
                }

                state.ExistsInDatabase = true;
            }
        }

        private static void BindStateParameters(SqlCommand cmd, DeviceOperationRetryState state, bool includeCreatedAt)
        {
            cmd.Parameters.AddWithValue("@deviceId", state.DeviceId);
            cmd.Parameters.AddWithValue("@employeeId", state.EmployeeId);
            cmd.Parameters.AddWithValue("@permissionLevel", (object)state.PermissionLevel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@permissionPending", state.PermissionPending);
            cmd.Parameters.AddWithValue("@permissionSyncCompletionBlocked", state.PermissionSyncCompletionBlocked);
            cmd.Parameters.AddWithValue("@personPayload", (object)state.PersonPayload ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@personPending", state.PersonPending);
            cmd.Parameters.AddWithValue("@facePayload", (object)state.FacePayload ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@facePending", state.FacePending);
            cmd.Parameters.AddWithValue("@deletePersonPending", state.DeletePersonPending);
            cmd.Parameters.AddWithValue("@deleteFacePending", state.DeleteFacePending);
            cmd.Parameters.AddWithValue("@attemptCount", state.AttemptCount);
            cmd.Parameters.AddWithValue("@nextRetryAt", (object)state.NextRetryAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lastError", (object)state.LastError ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lastAttemptAt", (object)state.LastAttemptAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@exhaustedAt", (object)state.ExhaustedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedAt", state.UpdatedAt);

            if (includeCreatedAt)
            {
                cmd.Parameters.AddWithValue("@createdAt", state.CreatedAt);
            }
        }

        private void DeleteState(SqlServerDatabase db, SqlTransaction transaction, int deviceId, string employeeId)
        {
            using (SqlCommand cmd = db.CreateCommand($@"DELETE FROM {TableName}
                                                     WHERE device_id = @deviceId
                                                       AND employee_id = @employeeId"))
            {
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@deviceId", deviceId);
                cmd.Parameters.AddWithValue("@employeeId", employeeId);
                cmd.ExecuteNonQuery();
            }
        }

        private bool HasPendingPermission(SqlServerDatabase db, SqlTransaction transaction, string employeeId)
        {
            using (SqlCommand cmd = db.CreateCommand($@"SELECT TOP (1) 1
                                                       FROM {TableName} WITH (UPDLOCK, HOLDLOCK)
                                                       WHERE employee_id = @employeeId
                                                         AND permission_pending = 1
                                                         AND exhausted_at IS NULL"))
            {
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@employeeId", employeeId);
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        private bool UpdateSyncedLevel(SqlServerDatabase db, SqlTransaction transaction, string employeeId, int permissionLevel)
        {
            using (SqlCommand cmd = db.CreateCommand(@"UPDATE system_users
                                                       SET last_synced_level = @level,
                                                           last_synced_at = SYSDATETIME()
                                                       WHERE username = @username
                                                         AND deleted = 0"))
            {
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@level", permissionLevel);
                cmd.Parameters.AddWithValue("@username", employeeId);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private SqlServerDatabase OpenDatabase()
        {
            string connectionString = commonHelper.obtenerCadenaConexion();
            SqlServerDatabase db = new SqlServerDatabase(commandTimeoutSeconds);
            db.Connect(connectionString);

            if (db.Connection == null)
            {
                db.Dispose();
                throw new InvalidOperationException("无法连接数据库，设备补偿状态存储不可用。");
            }

            return db;
        }
    }

    public sealed class DeviceOperationRetryState
    {
        public int DeviceId { get; set; }

        public string EmployeeId { get; set; }

        public int? PermissionLevel { get; set; }

        public bool PermissionPending { get; set; }

        public bool PermissionSyncCompletionBlocked { get; set; }

        public string PersonPayload { get; set; }

        public bool PersonPending { get; set; }

        public string FacePayload { get; set; }

        public bool FacePending { get; set; }

        public bool DeletePersonPending { get; set; }

        public bool DeleteFacePending { get; set; }

        public int AttemptCount { get; set; }

        public DateTime? NextRetryAt { get; set; }

        public string LastError { get; set; }

        public DateTime? LastAttemptAt { get; set; }

        public DateTime? ExhaustedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        internal bool ExistsInDatabase { get; set; }

        public bool HasPendingOperations => PermissionPending
            || PersonPending
            || FacePending
            || DeletePersonPending
            || DeleteFacePending;

        public PersonSyncRequest CreatePersonRequest()
        {
            if (string.IsNullOrWhiteSpace(PersonPayload))
            {
                return null;
            }

            PersonRetryPayload payload = JsonConvert.DeserializeObject<PersonRetryPayload>(PersonPayload);
            if (payload == null)
            {
                return null;
            }

            return new PersonSyncRequest
            {
                EmployeeId = payload.EmployeeId,
                FullName = payload.FullName,
                Gender = payload.Gender,
                ValidFrom = payload.ValidFrom,
                ValidTo = payload.ValidTo,
                Enabled = payload.Enabled,
                FaceImageBytes = null,
                FaceImageFormat = null
            };
        }

        public PersonSyncRequest CreateFaceRequest()
        {
            if (string.IsNullOrWhiteSpace(FacePayload))
            {
                return null;
            }

            FaceRetryPayload payload = JsonConvert.DeserializeObject<FaceRetryPayload>(FacePayload);
            if (payload == null)
            {
                return null;
            }

            return new PersonSyncRequest
            {
                EmployeeId = payload.EmployeeId,
                FaceImageBytes = payload.FaceImageBytes,
                FaceImageFormat = payload.FaceImageFormat
            };
        }
    }

    internal sealed class PersonRetryPayload
    {
        public string EmployeeId { get; set; }

        public string FullName { get; set; }

        public string Gender { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public bool Enabled { get; set; }
    }

    internal sealed class FaceRetryPayload
    {
        public string EmployeeId { get; set; }

        public byte[] FaceImageBytes { get; set; }

        public string FaceImageFormat { get; set; }
    }

    internal sealed class PermissionRetryCommitResult
    {
        public static PermissionRetryCommitResult Completed { get; } = new PermissionRetryCommitResult
        {
            Success = true
        };

        public bool Success { get; private set; }

        public string ErrorMessage { get; private set; }

        public static PermissionRetryCommitResult RetryableFailure(string errorMessage)
        {
            return new PermissionRetryCommitResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
