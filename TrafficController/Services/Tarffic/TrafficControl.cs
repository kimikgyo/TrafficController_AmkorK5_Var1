using Common.Models.Missions;

namespace TrafficController.Services
{
    public partial class TrafficService
    {
        private const string PARAM_KEY = "linkedZone";

        private void TrafficControl()
        {
            PendingToExecuting();                        // 1) PENDING → EXECUTING 승격
            CompletedToRemove();                        // 2) COMPLETE 중 Area 밖에 있는 것 삭제
            SkipedToRemove();
            var groupByArea = GroupMissionsByArea();    // 3) Area 기준 그룹핑
            ExecutingToCompleted(groupByArea);          // 4) Area별 순서대로 EXECUTING을 COMPLETE로 승격
        }

        /// <summary>
        /// 1) Pending → Executing (LINKEDAREA 없으면 SKIPPED)
        /// </summary>
        private void PendingToExecuting()
        {
            // ------------------------------------------------------------
            // [0] Repository 방어
            // ------------------------------------------------------------
            if (_repository == null)
            {
                EventLogger.Warn("[Traffic][PendingToExecuting] _repository is null. Aborting process.");
                return;
            }

            // --------------------------------------------------------
            // [1] 미션 조회
            // --------------------------------------------------------
            var missions = _repository.Missions.GetAll();
            if (missions == null)
            {
                EventLogger.Warn("[Traffic][PendingToExecuting] Mission list is null. Aborting.");
                return;
            }

            // --------------------------------------------------------
            // [2] PENDING만 필터
            // --------------------------------------------------------
            var pendingMissions = missions
                .Where(m => m != null && m.state == nameof(MissionState.PENDING))
                .ToList();

            if (pendingMissions.Count == 0)
                return;

            // --------------------------------------------------------
            // [3] LINKEDAREA 있으면 EXECUTING, 없으면 SKIPPED
            // --------------------------------------------------------
            foreach (var mission in pendingMissions)
            {
                if (mission == null)
                {
                    EventLogger.Warn("[Traffic][PendingToExecuting] Mission instance is null. Skipping.");
                    continue;
                }

                var linkedAreaParam = _repository.Missions.FindParameterByKey(mission.parameters, PARAM_KEY);

                if (linkedAreaParam == null || string.IsNullOrWhiteSpace(linkedAreaParam.value))
                {
                    // Traffic 대상이 아니므로 SKIPPED
                    updateStateMission(mission, nameof(MissionState.SKIPPED), false);
                    EventLogger.Info($"[Traffic][PendingToExecuting] LINKEDAREA empty. Skip mission. guid={mission.guid}, name={mission.name}");
                    continue;
                }

                updateStateMission(mission, nameof(MissionState.EXECUTING), false);
            }
        }

        /// <summary>
        /// 2) Skipped → Remove
        /// </summary>
        private void SkipedToRemove()
        {
            if (_repository == null)
            {
                EventLogger.Error("[Traffic][SkipedToRemove] _repository is null. Aborting process.");
                return;
            }

            var missions = _repository.Missions.GetAll();
            if (missions == null)
            {
                EventLogger.Warn("[Traffic][SkipedToRemove] Mission list is null. Aborting.");
                return;
            }

            var skipedMissions = missions
                .Where(m => m != null && m.state == nameof(MissionState.SKIPPED))
                .ToList();

            foreach (var skipedMission in skipedMissions)
            {
                if (skipedMission == null) continue;

                _repository.Missions.Remove(skipedMission);
                EventLogger.Info($"[Traffic][SkipedToRemove] Mission Remove missionId={skipedMission.guid}, missionName={skipedMission.name}");
            }
        }

        /// <summary>
        /// 3) GroupMissionsByArea (LINKEDAREA 기준 그룹핑)
        /// 모든 미션을 AreaKey(LinkedArea) 기준으로 그룹핑하여
        /// Dictionary<string, List<Mission>> 형태로 반환한다.
        ///
        /// key   : AreaKey (= LinkedArea value)
        /// value : 그 Area에 속한 Mission 리스트
        /// </summary>
        private Dictionary<string, List<Mission>> GroupMissionsByArea()
        {
            var groupByArea = new Dictionary<string, List<Mission>>();

            // ------------------------------------------------------------
            // [0] Repository 방어
            // ------------------------------------------------------------
            if (_repository == null)
            {
                EventLogger.Error("[Traffic][GroupMissionsByArea] _repository is null. Aborting process.");
                return groupByArea;
            }

            // --------------------------------------------------------
            // [1] 미션 조회
            // --------------------------------------------------------
            var missions = _repository.Missions.GetAll();
            if (missions == null)
            {
                EventLogger.Warn("[Traffic][GroupMissionsByArea] Mission list is null. Return empty result.");
                return groupByArea;
            }

            // --------------------------------------------------------
            // [2] LINKEDAREA 있는 미션만 그룹핑
            // --------------------------------------------------------
            foreach (var mission in missions)
            {
                if (mission == null)
                {
                    EventLogger.Warn("[Traffic][GroupMissionsByArea] Mission is null. Skipping.");
                    continue;
                }

                if (mission.parameters == null || mission.parameters.Count == 0)
                {
                    // Traffic 관련이 아니므로 스킵
                    continue;
                }

                var linkedAreaParam = _repository.Missions.FindParameterByKey(mission.parameters, PARAM_KEY);
                if (linkedAreaParam == null || string.IsNullOrWhiteSpace(linkedAreaParam.value))
                    continue;

                string areaKey = linkedAreaParam.value;

                if (!groupByArea.ContainsKey(areaKey))
                    groupByArea[areaKey] = new List<Mission>();

                groupByArea[areaKey].Add(mission);
            }

            return groupByArea;
        }

        /// <summary>
        /// 4) Executing → Completed
        /// </summary>
        /// <param name="groupByArea"></param>
        private void ExecutingToCompleted(Dictionary<string, List<Mission>> groupByArea)
        {
            if (groupByArea == null) return;
            if (groupByArea.Count == 0) return;

            if (_repository == null)
            {
                EventLogger.Error("[Traffic][ExecutingToCompleted] _repository is null. Aborting process.");
                return;
            }

            foreach (var kv in groupByArea)
            {
                string ZoneKey = kv.Key;
                List<Mission> areaMissionList = kv.Value;

                if (areaMissionList == null || areaMissionList.Count == 0)
                    continue;

                // COMPLETED가 이미 있으면 더 승격하지 않음
                var hasCompleted = areaMissionList.FirstOrDefault(m => m != null && m.state == nameof(MissionState.COMPLETED));
                if (hasCompleted != null)
                    continue;

                // EXECUTING 중 오래된 것 1개를 COMPLETED로 승격
                var executingList = areaMissionList.Where(m => m != null && m.state == nameof(MissionState.EXECUTING)).OrderBy(m => m.updatedAt).ToList();

                if (executingList.Count == 0)
                    continue;

                var mission = executingList.FirstOrDefault();
                if (mission == null)
                {
                    EventLogger.Warn($"[Traffic][ExecutingToCompleted] FirstOrDefault returned null. ZoneKey={ZoneKey}");
                    continue;
                }

                EventLogger.Info($"[Traffic][ExecutingToCompleted] Promote EXECUTING->COMPLETED. guid={mission.guid},missionName={mission.name}, ZoneKey={ZoneKey}");
                updateStateMission(mission, nameof(MissionState.COMPLETED), true);
            }
        }

        /// <summary>
        /// 5) Completed → Remove (폴리곤 기준 Inside/Outside 적용)
        /// </summary>
        private void CompletedToRemove()
        {
            // ------------------------------------------------------------
            // [0] Repository 방어
            // ------------------------------------------------------------
            if (_repository == null)
            {
                EventLogger.Error("[Traffic][CompletedToRemove] _repository is null. Aborting process.");
                return;
            }

            // --------------------------------------------------------
            // [1] 미션 조회
            // --------------------------------------------------------
            var missions = _repository.Missions.GetAll();
            if (missions == null)
            {
                EventLogger.Warn("[Traffic][CompletedToRemove] Mission list is null. Aborting.");
                return;
            }

            // --------------------------------------------------------
            // [2] COMPLETED 상태 미션만
            // --------------------------------------------------------
            var completedMissions = missions.Where(m => m != null && m.state == nameof(MissionState.COMPLETED)).ToList();

            foreach (var mission in completedMissions)
            {
                if (mission == null) continue;

                // 파라미터 없으면 Traffic 미션이 아니므로 스킵
                if (mission.parameters == null || mission.parameters.Count == 0)
                    continue;

                // LINKEDAREA 조회 (COMPLETED인데 LINKEDAREA 없으면 Traffic이 아니므로 스킵)
                var linkedZoneParam = _repository.Missions.FindParameterByKey(mission.parameters, PARAM_KEY);
                if (linkedZoneParam == null || string.IsNullOrWhiteSpace(linkedZoneParam.value))
                    continue;

                string ZoneKey = linkedZoneParam.value;

                // Area 조회
                var Zone = _repository.ACSZones.GetById(ZoneKey);
                if (Zone == null)
                {
                    EventLogger.Warn($"[Traffic][CompletedToRemove] Zone not found. guid={mission.guid}, ZoneKey={ZoneKey}");
                    continue;
                }

                // 폴리곤 캐시 준비 확인
                if (Zone.cacheReady == false)
                {
                    EventLogger.Warn($"[Traffic][CompletedToRemove] Zone cacheReady=false. Skip remove check. guid={mission.guid}, ZoneId={Zone.zoneId}, ZoneName={Zone.name}");
                    continue;
                }

                // Worker 배정 확인
                if (string.IsNullOrWhiteSpace(mission.assignedWorkerId))
                {
                    // Worker 정보가 없으면 정책상 삭제
                    EventLogger.Warn($"[Traffic][CompletedToRemove] assignedWorkerId empty. Remove mission. guid={mission.guid}, ZoneKey={ZoneKey}");
                    _repository.Missions.Remove(mission);
                    continue;
                }

                // Worker 조회
                var worker = _repository.Workers.GetById(mission.assignedWorkerId);
                if (worker == null)
                {
                    EventLogger.Warn($"[Traffic][CompletedToRemove] Worker not found. workerId={mission.assignedWorkerId}, guid={mission.guid}. Remove mission.");
                    _repository.Missions.Remove(mission);
                    continue;
                }

                // mapId 불일치면 판정하지 않음(맵 전환/엘리베이터 등)
                if (Zone.mapId != worker.mapId)
                {
                    EventLogger.Warn(
                        $"[Traffic][CompletedToRemove] mapId mismatch. Keep mission. guid={mission.guid}" +
                        $",workerId={worker.id}, workerMap={worker.mapId}, areaMap={Zone.mapId}, zoneId={Zone.zoneId}"
                    );
                    continue;
                }

                // ----------------------------------------------------
                // [핵심] 폴리곤 Inside 판정
                // ----------------------------------------------------
                bool inside = _repository.ACSZones.IsInsideZone(worker.position_X, worker.position_Y,worker.mapId, Zone);

                // 1) 아직 한번도 IN 한 적 없는데, 지금 IN이면 기록만 남기고 유지
                if (mission.enteredZoneOnce == false && inside)
                {
                    mission.enteredZoneOnce = true;
                    _repository.Missions.Update(mission); // DB에 남길 거면 Update, 아니면 생략 가능

                    EventLogger.Info($"[Traffic][CompletedToRemove] First ENTER. Mark. guid={mission.guid},missionName={mission.name}, workerId={worker.id},workerName={worker.name}" +
                                     $", zoneId={Zone.zoneId}, zoneName = {Zone.name}, enteredZoneOnce={mission.enteredZoneOnce}");
                    continue; // 또는 continue
                }

                // 2) 한번도 IN 안 했고 지금도 OUT이면 → Remove 금지(유지)
                if (mission.enteredZoneOnce == false && inside == false)
                {
                    // 필요하면 로그는 Debug 수준 권장(너무 많이 찍힘)
                    // EventLogger.Info($"[Traffic][CompletedToRemove] Not entered yet. Keep. guid={mission.guid}");
                    continue; // 또는 continue
                }

                // 3) IN을 한번이라도 했고, 이제 OUT이면 → Remove
                if (mission.enteredZoneOnce && inside == false)
                {
                    //에어리어를 지났는데도 정확히 Exit를 확인하기 위해서 일정거리가 지나면 Exit로 감지한다
                    bool exitConfirmed = _repository.ACSZones.IsExitConfirmed(worker.position_X, worker.position_Y,worker.mapId, Zone);

                    if (exitConfirmed)
                    {
                        mission.finishedAt = DateTime.UtcNow;

                        _repository.Missions.Update(mission);
                        _repository.Missions.Remove(mission);

                        EventLogger.Info($"[Traffic][CompletedToRemove] EXIT after enteredOnce. Remove mission. guid={mission.guid}, missionName={mission.name}, workerId={worker.id},workerName={worker.name}" +
                                         $", zoneId={Zone.zoneId}, zoneName = {Zone.name}, enteredZoneOnce={mission.enteredZoneOnce}");
                    }
                    else
                    {
                        // 아직 경계 근처 튐일 수 있으니 유지
                        EventLogger.Info($"[Traffic][CompletedToRemove] OUT but not confirmed yet. Keep.  guid={mission.guid}, missionName={mission.name}, workerId={worker.id},workerName={worker.name}" +
                                         $", zoneId={Zone.zoneId}, zoneName = {Zone.name}, enteredZoneOnce={mission.enteredZoneOnce}");
                    }
                }
                else
                {
                    // Area 내부면 유지
                    EventLogger.Info($"[Traffic][CompletedToRemove] Worker still inside polygon Zone. Keep mission. guid={mission.guid}, missionName={mission.name}, workerId={worker.id},workerName={worker.name}" +
                                         $", zoneId={Zone.zoneId}, zoneName = {Zone.name}, enteredZoneOnce={mission.enteredZoneOnce}");
                }
            }
        }
    }
}