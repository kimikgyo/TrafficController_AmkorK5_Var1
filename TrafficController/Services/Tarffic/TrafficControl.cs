using Common.Models.Missions;

namespace TrafficController.Services
{
    public partial class TrafficService
    {
        private void TrafficControl()
        {
            PendingToExecuting();                        // 1) PENDING → EXECUTING 승격
            completedToRemove();                        // 2) COMPLETE 중 Area 밖에 있는 것 삭제
            var groupByArea = GroupMissionsByArea();    // 3) Area 기준 그룹핑
            ExecutingToCompleted(groupByArea);          // 4) Area별 순서대로 EXECUTING을 COMPLETE로 승격
        }

        private void PendingToExecuting()
        {
            // ------------------------------------------------------------
            // [0] Repository 존재 여부 확인
            // ------------------------------------------------------------
            if (_repository == null)
            {
                EventLogger.Warn("[Traffic][PendingToExecuting] _repository is null. Aborting process.");
                return;
            }

            try
            {
                // --------------------------------------------------------
                // [1] 모든 미션 조회
                // --------------------------------------------------------
                var missions = _repository.Missions.GetAll();
                if (missions == null)
                {
                    EventLogger.Warn("[Traffic][PendingToExecuting] Mission list is null. Aborting.");
                    return;
                }

                // --------------------------------------------------------
                // [2] PENDING 상태인 미션만 필터링
                // --------------------------------------------------------
                var pendingMissions = missions.Where(m => m != null && m.state == nameof(MissionState.PENDING)).ToList();

                // --------------------------------------------------------
                // [3] 각 PENDING 미션을 EXECUTING 으로 변경
                // --------------------------------------------------------
                foreach (var mission in pendingMissions)
                {
                    // mission 자체 방어
                    if (mission == null)
                    {
                        EventLogger.Warn("[Traffic][PendingToExecuting] Mission instance is null. Skipping.");
                        continue;
                    }

                    // 미션 상태 변경
                    updateStateMission(mission, nameof(MissionState.EXECUTING), true);
                }
            }
            catch (Exception ex)
            {
                // 예외 발생 시 공통 예외 로거 호출
                main.LogExceptionMessage(ex);
            }
        }

        private void ExecutingToCompleted(Dictionary<string, List<Mission>> groupByArea)
        {
            //EventLogger.Info("[Traffic][ExecutingToCompleted] Start");

            // ------------------------------------------------------------
            // [0] 입력값 / 저장소 방어 코드
            // ------------------------------------------------------------
            if (groupByArea == null || groupByArea.Count == 0)
            {
                EventLogger.Info("[Traffic][ExecutingToCompleted] groupByArea is null or empty. Nothing to process.");
                return;
            }

            if (_repository == null)
            {
                EventLogger.Error("[Traffic][ExecutingToCompleted] _repository is null. Aborting process.");
                return;
            }

            try
            {
                // --------------------------------------------------------
                // [1] Area(구역) 별로 순회
                // --------------------------------------------------------
                foreach (var kv in groupByArea)
                {
                    string areaKey = kv.Key;                  // Area Key
                    List<Mission> areaMissionList = kv.Value; // 해당 Area 의 미션 목록

                    // Area 내부에 미션이 없으면 스킵
                    if (areaMissionList == null || areaMissionList.Count == 0)
                    {
                        EventLogger.Info($"[Traffic][ExecutingToCompleted] No missions found for area. areaKey={areaKey}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [1-1] 이미 COMPLETED 미션이 있는지 확인
                    //       (한 Area엔 COMPLETED는 1개만 존재해야 함)
                    // ----------------------------------------------------
                    bool hasCompleted = false;

                    foreach (var m in areaMissionList)
                    {
                        if (m == null) continue;

                        if (m.state == nameof(MissionState.COMPLETED))
                        {
                            hasCompleted = true;
                            break;
                        }
                    }

                    if (hasCompleted)
                    {
                        EventLogger.Info($"[Traffic][ExecutingToCompleted] COMPLETED mission already exists. Skip promotion. areaKey={areaKey}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [1-2] EXECUTING 상태 미션 필터링
                    //       updatedAt 기준 오름차순 정렬(오래된 순)
                    // ----------------------------------------------------
                    var executingList = areaMissionList.Where(m => m != null && m.state == nameof(MissionState.EXECUTING)).OrderBy(m => m.updatedAt).ToList();

                    if (executingList.Count == 0)
                    {
                        EventLogger.Info($"[Traffic][ExecutingToCompleted] No EXECUTING missions to promote. areaKey={areaKey}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [1-3] 가장 오래된 EXECUTING 미션 선택 (FirstOrDefault)
                    // ----------------------------------------------------
                    var mission = executingList.FirstOrDefault();

                    if (mission == null)
                    {
                        // 리스트가 1개 이상 있어도 null 이 들어있을 가능성 대비
                        EventLogger.Warn($"[Traffic][ExecutingToCompleted] FirstOrDefault returned null. Skip area. areaKey={areaKey}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [1-4] 해당 EXECUTING 미션을 COMPLETED 로 승격
                    // ----------------------------------------------------
                    EventLogger.Info($"[Traffic][ExecutingToCompleted] Promoting EXECUTING → COMPLETED. guid={mission.guid}, areaKey={areaKey}");

                    updateStateMission(mission, nameof(MissionState.COMPLETED), true);

                    EventLogger.Info($"[Traffic][ExecutingToCompleted] Promotion done. guid={mission.guid}, areaKey={areaKey}");
                }

                //EventLogger.Info("[Traffic][ExecutingToCompleted] Completed");
            }
            catch (Exception ex)
            {
                // 전체 프로세스에서 발생하는 예외 처리 (try/catch 단 1개)
                EventLogger.Error($"[Traffic][ExecutingToCompleted] Exception occurred - Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 미션을 AreaKey(LinkedArea) 기준으로 그룹핑하여
        /// Dictionary<string, List<Mission>> 형태로 반환한다.
        ///
        /// key   : AreaKey (= LinkedArea value)
        /// value : 그 Area에 속한 Mission 리스트
        /// </summary>
        private Dictionary<string, List<Mission>> GroupMissionsByArea()
        {
            // 결과용 딕셔너리 (AreaKey 별 미션 리스트)
            var groupByArea = new Dictionary<string, List<Mission>>();

            // ------------------------------------------------------------
            // [0] Repository null 여부 체크
            // ------------------------------------------------------------
            if (_repository == null)
            {
                EventLogger.Error("[Traffic][GroupMissionsByArea] _repository is null. Aborting process.");
                return groupByArea;
            }

            try
            {
                // --------------------------------------------------------
                // [1] 모든 미션 조회
                // --------------------------------------------------------
                var missions = _repository.Missions.GetAll();
                if (missions == null)
                {
                    EventLogger.Warn("[Traffic][GroupMissionsByArea] Mission list is null. Return empty result.");
                    return groupByArea;
                }

                // --------------------------------------------------------
                // [2] 각 미션을 순회하면서 LINKEDAREA 기준으로 그룹핑
                // --------------------------------------------------------
                foreach (var mission in missions)
                {
                    // 미션 null 방어
                    if (mission == null)
                    {
                        EventLogger.Warn("[Traffic][GroupMissionsByArea] Mission instance is null. Skipping.");
                        continue;
                    }

                    // 파라미터 null / empty 방어
                    if (mission.parameters == null || mission.parameters.Count == 0)
                    {
                        // Traffic 과 관련 없는 일반 미션으로 간주하고 스킵
                        // (로그 레벨은 Info: 정상 플로우이지만 참고용)
                        EventLogger.Info($"[Traffic][GroupMissionsByArea] Mission has no parameters. Skip traffic grouping. guid={mission.guid}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [2-1] LINKEDAREA 파라미터 조회
                    // ----------------------------------------------------
                    var linkedAreaParam = _repository.Missions.FindParameterByKey(mission.parameters, "LINKEDAREA");
                    if (linkedAreaParam == null || string.IsNullOrWhiteSpace(linkedAreaParam.value))
                    {
                        // LINKEDAREA 가 없으면 트래픽 대상이 아니므로 스킵
                        EventLogger.Info($"[Traffic][GroupMissionsByArea] LINKEDAREA not found or empty. Skip mission. guid={mission.guid}");
                        continue;
                    }

                    string areaKey = linkedAreaParam.value;

                    // ----------------------------------------------------
                    // [2-2] AreaKey 기준으로 딕셔너리 그룹핑
                    // ----------------------------------------------------
                    List<Mission> listForArea;

                    // 해당 AreaKey 가 처음이면 새 리스트 생성
                    if (!groupByArea.TryGetValue(areaKey, out listForArea))
                    {
                        listForArea = new List<Mission>();
                        groupByArea[areaKey] = listForArea;
                    }

                    // AreaKey 에 해당하는 리스트에 미션 추가
                    listForArea.Add(mission);
                }

                // --------------------------------------------------------
                // [3] 그룹핑 결과 로그
                // --------------------------------------------------------
                EventLogger.Info($"[Traffic][GroupMissionsByArea] Grouping completed. Area count={groupByArea.Count}");

                return groupByArea;
            }
            catch (Exception ex)
            {
                // 예외 발생 시 공통 예외 로거 호출
                main.LogExceptionMessage(ex);
                return groupByArea;
            }
        }

        private void completedToRemove()
        {
            // ------------------------------------------------------------
            // [0] Repository null 여부 체크
            // ------------------------------------------------------------
            if (_repository == null)
            {
                EventLogger.Error("[Traffic][CompletedToRemove] _repository is null. Aborting process.");
                return;
            }

            try
            {
                // --------------------------------------------------------
                // [1] 모든 미션 조회
                // --------------------------------------------------------
                var missions = _repository.Missions.GetAll();
                if (missions == null)
                {
                    EventLogger.Warn("[Traffic][CompletedToRemove] Mission list is null. Aborting.");
                    return;
                }

                // --------------------------------------------------------
                // [2] COMPLETED 상태인 미션만 필터링
                // --------------------------------------------------------
                var completedMissions = missions.Where(m => m != null && m.state == nameof(MissionState.COMPLETED)).ToList();

                //EventLogger.Info($"[Traffic][CompletedToRemove] Completed mission count: {completedMissions.Count}");

                // --------------------------------------------------------
                // [3] 각 COMPLETED 미션 처리
                // --------------------------------------------------------
                foreach (var mission in completedMissions)
                {
                    // mission null 방어
                    if (mission == null)
                    {
                        EventLogger.Warn("[Traffic][CompletedToRemove] Mission instance is null. Skipping.");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [3-1] 파라미터 존재 여부 체크
                    // ----------------------------------------------------
                    if (mission.parameters == null || mission.parameters.Count == 0)
                    {
                        // 파라미터 없으면 Traffic 미션이 아니므로 스킵
                        EventLogger.Info($"[Traffic][CompletedToRemove] Mission has no parameters. Skip traffic check. guid={mission.guid}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [3-2] LINKEDAREA 파라미터 조회
                    // ----------------------------------------------------
                    var linkedAreaParam = _repository.Missions.FindParameterByKey(mission.parameters, "LINKEDAREA");
                    if (linkedAreaParam == null || string.IsNullOrWhiteSpace(linkedAreaParam.value))
                    {
                        // Traffic 미션이 아니므로 스킵
                        EventLogger.Info($"[Traffic][CompletedToRemove] LINKEDAREA not found or empty. Skip mission. guid={mission.guid}");
                        continue;
                    }

                    string areaKey = linkedAreaParam.value;

                    // ----------------------------------------------------
                    // [3-3] Area 정보 조회
                    // ----------------------------------------------------
                    var area = _repository.ACSAreas.GetById(areaKey);
                    if (area == null)
                    {
                        EventLogger.Warn($"[Traffic][CompletedToRemove] Area not found. guid={mission.guid}, areaKey={areaKey}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [3-4] 배정된 Worker 여부 체크
                    // ----------------------------------------------------
                    if (string.IsNullOrWhiteSpace(mission.assignedWorkerId))
                    {
                        // Worker 정보가 없으므로 정책상 삭제
                        EventLogger.Warn($"[Traffic][CompletedToRemove] assignedWorkerId is empty. Remove mission. guid={mission.guid}");

                        _repository.Missions.Remove(mission);
                        //EventLogger.Info($"[Traffic][CompletedToRemove] Mission removed (no worker). guid={mission.guid}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [3-5] Worker 정보 조회
                    // ----------------------------------------------------
                    var worker = _repository.Workers.GetById(mission.assignedWorkerId);
                    if (worker == null)
                    {
                        // Worker 정보를 찾을 수 없으므로 정책상 삭제
                        EventLogger.Warn($"[Traffic][CompletedToRemove] Worker not found. workerId={mission.assignedWorkerId}, guid={mission.guid}");

                        _repository.Missions.Remove(mission);
                        //EventLogger.Info($"[Traffic][CompletedToRemove] Mission removed (worker not found). guid={mission.guid}");
                        continue;
                    }

                    // ----------------------------------------------------
                    // [3-6] Worker가 Area 내부에 있는지 여부 체크
                    // ----------------------------------------------------
                    bool inside = _repository.ACSAreas.IsInsideArea(worker.position_X, worker.position_Y, area);

                    if (!inside)
                    {
                        // Area 밖으로 나갔으므로 안전하게 삭제
                        mission.finishedAt = DateTime.Now;
                        _repository.Missions.Update(mission);
                        _repository.Missions.Remove(mission);

                        //EventLogger.Info($"[Traffic][CompletedToRemove] Worker is outside area. Mission removed. guid={mission.guid}, workerId={mission.assignedWorkerId}");
                    }
                    else
                    {
                        // Area 내부에 있으므로 유지
                        EventLogger.Info($"[Traffic][CompletedToRemove] Worker still inside area. Keep mission. guid={mission.guid}, workerId={mission.assignedWorkerId}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 공통 예외 처리 로거
                main.LogExceptionMessage(ex);
            }
        }
    }
}