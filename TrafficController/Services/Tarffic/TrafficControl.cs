using Common.Models.Missions;
using System.Configuration;
using System.Reflection;

namespace TrafficController.Services
{
    public partial class TrafficService
    {
        private void TrafficControl()
        {
            pendingToExecting();                        // 1) PENDING → EXECUTING 승격
            completedToRemove();                        // 2) COMPLETE 중 Area 밖에 있는 것 삭제
            var groupByArea = GroupMissionsByArea();    // 3) Area 기준 그룹핑
            executingToCompleted(groupByArea);          // 4) Area별 순서대로 EXECUTING을 COMPLETE로 승격
        }

        private void pendingToExecting()
        {
            var missions = _repository.Missions.GetAll();
            var pandingMissions = missions.Where(m => m.state == nameof(MissionState.PENDING)).ToList();

            foreach (var pandingMission in pandingMissions)
            {
                updateStateMission(pandingMission, nameof(MissionState.EXECUTING), true);
            }
        }

        private void executingToCompleted(Dictionary<string, List<Mission>> groupByArea)
        {
            // 3) Area별로 순회하면서 EXECUTING → COMPLETED 승격 처리
            foreach (var kv in groupByArea)
            {
                string areaKey = kv.Key;
                List<Mission> areaMissionList = kv.Value;  // 이 Area에 속한 모든 미션들

                // 3-1) 이 Area에 이미 COMPLETED 상태의 미션이 있는지 확인
                var completed = areaMissionList.FirstOrDefault(m => m.state == nameof(MissionState.COMPLETED));

                // 이미 COMPLETE 살아 있으면 → 이 Area 에서는 승격하지 않음
                if (completed != null) continue;

                // 3-2) EXECUTING 상태인 미션들만 모은다
                var executingList = areaMissionList.Where(m => m.state == nameof(MissionState.EXECUTING)).OrderBy(m => m.updatedAt).ToList();

                // EXECUTING 미션이 없다면 이 Area는 할 일이 없음
                if (executingList.Count == 0) continue;

                // 3-3) EXECUTING 중에서 createdAt 가장 오래된 미션 1개 선택
                var mission = executingList.FirstOrDefault();

                if (mission != null)
                {
                    // 3-4) 선택된 미션을 COMPLETED 로 승격
                    updateStateMission(mission, nameof(MissionState.COMPLETED), true);

                    EventLogger.Info($"[TRAFFIC] EXECUTING → COMPLETED  guid={mission.guid}, area={areaKey}");
                }
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
            // 1) 모든 미션 불러오기
            var missions = _repository.Missions.GetAll();

            // 2) AreaKey(LinkedArea) 기준으로 미션 그룹핑
            var groupByArea = new Dictionary<string, List<Mission>>();

            foreach (var m in missions)
            {
                // 미션에서 AreaKey(LinkedArea) 파라미터 추출
                string areaKey = _repository.Missions.FindParameterByKey(m.parameters, "LINKEDAREA").value;

                // LinkedArea 가 없는 미션은 트래픽 대상이 아님 → 스킵
                if (string.IsNullOrWhiteSpace(areaKey))
                    continue;

                // 처음 등장한 AreaKey라면 새로운 리스트를 만든다.
                if (!groupByArea.ContainsKey(areaKey))
                {
                    groupByArea[areaKey] = new List<Mission>();
                }

                // 해당 AreaKey 리스트에 미션 추가
                groupByArea[areaKey].Add(m);
            }

            return groupByArea;
        }

        private string GetAreaKey(Mission m)
        {
            string reValue = null;
            // m.parameters 가 null 이면 null 반환
            if (m.parameters != null)
            {
                //LinkedFacility 파라메타를 찾는다.
                var parameter = m.parameters.FirstOrDefault(x => x.key != null && x.key.ToUpper() == "LINKEDAREA");
                if (parameter != null)
                {
                    reValue = parameter.value.Trim();
                }
            }

            return reValue;
        }

        private void completedToRemove()
        {
            // 1) 모든 미션 불러오기
            var missions = _repository.Missions.GetAll();

            // 2) COMPLETED 상태인 미션들만 추리기
            var completedMissions = missions.Where(m => m.state == nameof(MissionState.COMPLETED)).ToList();

            foreach (var mission in completedMissions)
            {
                // 2-1) LINKEDAREA (AreaKey) 가져오기
                string areaKey = _repository.Missions.FindParameterByKey(mission.parameters, "LINKEDAREA").value;
                // 트래픽 대상이 아닌 일반 COMPLETE 미션이면 이 루틴에서는 건너뜀
                if (string.IsNullOrWhiteSpace(areaKey)) continue;

                // 2-2) AreaStore에서 Area 정보 조회
                var area = _repository.ACSAreas.GetById(areaKey);
                if (area == null)
                {
                    // 설정/동기화 오류 → 일단 삭제하지 않고 경고만 남기고 건너뜀
                    EventLogger.Warn($"[TRAFFIC] Area not found for mission guid={mission.guid}, areaKey={areaKey}");
                    continue;
                }

                // 2-3) 로봇 ID 확인
                if (string.IsNullOrWhiteSpace(mission.assignedWorkerId))
                {
                    // Worker 정보가 없다면 '더 이상 이 Area를 점유하지 않는다' 로 보고 삭제해도 되고,
                    // 혹은 스킵해도 된다. 여기서는 삭제 쪽 정책 예시.
                    EventLogger.Warn($"[TRAFFIC] IsNullOrWhiteSpace, worker={mission.assignedWorkerId}, guid={mission.guid}");
                    _repository.Missions.Remove(mission);
                    continue;
                }

                // 2-4) 로봇 위치 가져오기
                var worker = _repository.Workers.GetById(mission.assignedWorkerId);
                if (worker == null)
                {
                    // 위치를 알 수 없으면, 정책에 따라:
                    // A) 삭제하지 않고 다음에 다시 검사
                    // B) 더 이상 점유 안 한다고 보고 삭제
                    // 여기서는 A안: 삭제하지 않고 건너뜀
                    _repository.Missions.Remove(mission);
                    EventLogger.Warn($"[TRAFFIC] Worker position not found , worker={mission.assignedWorkerId}, guid={mission.guid}");
                    continue;
                }

                // 2-5) 로봇이 Area 내부에 있는지 체크
                bool inside = _repository.ACSAreas.IsInsideArea(worker.position_X, worker.position_Y, area);
                if (!inside)
                {
                    // 2-6) Area 밖으로 완전히 나갔으므로 이 미션 삭제
                    mission.finishedAt = DateTime.Now;
                    _repository.Missions.Update(mission);
                    _repository.Missions.Remove(mission);
                }
                // inside == true 면, 아직 Area를 점유하고 있으므로 유지
            }
        }
    }
}