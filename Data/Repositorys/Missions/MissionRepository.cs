using Common.Models.Bases;
using Common.Models.Missions;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace Data.Repositorys.Jobs
{
    public class MissionRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Mission"); //Function 실행관련 Log
        private readonly string connectionString;
        private readonly List<Mission> _missions = new List<Mission>(); // cached data
        private readonly object _lock = new object();

        public MissionRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
            Load();
        }

        private void createTable()
        {
            //VARCHAR 대신 NVARCHAR로 저장해야함 VARCHAR은 영문만 가능함
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[TrafficController_Mission]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[TrafficController_Mission]
                    (
                        [guid]                     NVARCHAR(64)     NULL,
                        [state]                    NVARCHAR(64)     NULL,
                        [createdAt]                datetime        NULL,
                        [updatedAt]                datetime        NULL,
                        [finishedAt]               datetime        NULL,
                        [orderId]                  NVARCHAR(64)     NULL,
                        [jobId]                    NVARCHAR(64)     NULL,
                        [acsMissionId]             NVARCHAR(64)     NULL,
                        [carrierId]                NVARCHAR(64)     NULL,
                        [name]                     NVARCHAR(64)     NULL,
                        [service]                  NVARCHAR(64)     NULL,
                        [type]                     NVARCHAR(64)     NULL,
                        [subType]                  NVARCHAR(64)     NULL,
                        [linkedFacility]           NVARCHAR(64)     NULL,
                        [sequence]                 int             NULL,
                        [isLocked]                 int             NULL,
                        [sequenceChangeCount]      int             NULL,
                        [retryCount]               int             NULL,
                        [specifiedWorkerId]        NVARCHAR(64)     NULL,
                        [assignedWorkerId]         NVARCHAR(64)     NULL,
                        [enteredZoneOnce]           int              NULL,
                        [parametersJson]            NVARCHAR(2000)    NULL,
                        [preReportsJson]            NVARCHAR(2000)    NULL,
                        [postReportsJson]           NVARCHAR(2000)    NULL,

                    );
                END;
            ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(checkTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void Load()
        {
            _missions.Clear();
            using (var con = new SqlConnection(connectionString))
            {
                foreach (var data in con.Query<Mission>("SELECT * FROM [TrafficController_Mission]"))
                {
                    //파라메타를 Json으로 되어있던것을 다시 List로 변경한다.
                    if (data.parametersJson != null) data.parameters = JsonSerializer.Deserialize<List<Parameter>>(data.parametersJson);
                    if (data.preReportsJson != null) data.preReports = JsonSerializer.Deserialize<List<PreReport>>(data.preReportsJson);
                    if (data.postReportsJson != null) data.postReports = JsonSerializer.Deserialize<List<PostReport>>(data.postReportsJson);
                    _missions.Add(data);

                    logger.Info($"Load:{data}");
                }
            }
        }

        public void Add(Mission add)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO [TrafficController_Mission]
                                 (
                                       [guid]
                                      ,[state]
                                      ,[createdAt]
                                      ,[updatedAt]
                                      ,[finishedAt]
                                      ,[orderId]
                                      ,[jobId]
                                      ,[acsMissionId]
                                      ,[carrierId]
                                      ,[name]
                                      ,[service]
                                      ,[type]
                                      ,[subType]
                                      ,[linkedFacility]
                                      ,[sequence]
                                      ,[isLocked]
                                      ,[sequenceChangeCount]
                                      ,[retryCount]
                                      ,[specifiedWorkerId]
                                      ,[assignedWorkerId]
                                      ,[enteredZoneOnce]
                                      ,[parametersJson]
                                      ,[preReportsJson]
                                      ,[postReportsJson]
                                   )
                                  values
                                  (

                                         @guid
                                        ,@state
                                        ,@createdAt
                                        ,@updatedAt
                                        ,@finishedAt
                                     	,@orderId
                                        ,@jobId
                                        ,@acsMissionId
                                        ,@carrierId
                                        ,@name
                                        ,@service
                                        ,@type
                                        ,@subType
                                        ,@linkedFacility
                                        ,@sequence
                                        ,@isLocked
                                        ,@sequenceChangeCount
                                        ,@retryCount
                                        ,@specifiedWorkerId
                                        ,@assignedWorkerId
                                        ,@enteredZoneOnce
                                        ,@parametersJson
                                        ,@preReportsJson
                                        ,@postReportsJson
                                  );";
                    //TimeOut 시간을 60초로 연장 [기본30초]
                    //con.Execute(INSERT_SQL, param: add, commandTimeout: 60);
                    con.Execute(INSERT_SQL, param: add);
                    _missions.Add(add);
                    logger.Info($"Add: {add}");
                }
            }
        }

        public void Update(Mission update)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string UPDATE_SQL = @"
                            UPDATE [TrafficController_Mission]
                            SET

                                [state]                     = @state
                               ,[createdAt]                 = @createdAt
                               ,[updatedAt]                 = @updatedAt
                               ,[finishedAt]                = @finishedAt
                               ,[orderId]                  =  @orderId
                               ,[jobId]                    =  @jobId
                               ,[acsMissionId]             = @acsMissionId
                               ,[carrierId]                =  @carrierId
                               ,[name]                     =  @name
                               ,[service]                  =  @service
                               ,[type]                     =  @type
                               ,[subType]                  =  @subType
                               ,[linkedFacility]           =  @linkedFacility
                               ,[sequence]                 =  @sequence
                               ,[isLocked]                 =  @isLocked
                               ,[sequenceChangeCount]      =  @sequenceChangeCount
                               ,[retryCount]               =  @retryCount
                               ,[specifiedWorkerId]        =  @specifiedWorkerId
                               ,[assignedWorkerId]         =  @assignedWorkerId
                               ,[enteredZoneOnce]         =  @enteredZoneOnce
                               ,[parametersJson]           =  @parametersJson
                               ,[preReportsJson]           =  @preReportsJson
                               ,[postReportsJson]          =  @postReportsJson

                            WHERE [guid] = @guid";
                    //TimeOut 시간을 60초로 연장 [기본30초]
                    //con.Execute(UPDATE_SQL, param: update, commandTimeout: 60);
                    con.Execute(UPDATE_SQL, param: update);
                    logger.Info($"Update: {update}");
                }
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [TrafficController_Mission]");
                    _missions.Clear();
                    logger.Info($"Delete");
                }
            }
        }

        public void Remove(Mission remove)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [TrafficController_Mission] WHERE guid = @guid", param: new { guid = remove.guid });
                    _missions.Remove(remove);
                    logger.Info($"Remove: {remove}");
                }
            }
        }

        public List<Mission> GetAll()
        {
            lock (_lock)
            {
                return _missions.ToList();
            }
        }

        public Mission GetById(string Id)
        {
            lock (_lock)
            {
                return _missions.FirstOrDefault(m => m.guid == Id);
            }
        }

        public List<Mission> GetByJobId(string jobId)
        {
            lock (_lock)
            {
                return _missions.Where(m => m.jobId == jobId).ToList();
            }
        }

        public List<Mission> GetByOrderId(string orderId)
        {
            lock (_lock)
            {
                return _missions.Where(m => m.orderId == orderId).ToList();
            }
        }

        public List<Mission> GetByAssignedWorkerId(string AssignedWorkerId)
        {
            lock (_lock)
            {
                return _missions.Where(m => m.assignedWorkerId == AssignedWorkerId).ToList();
            }
        }

        public Mission GetByACSMissionId(string acsMissionId)
        {
            lock (_lock)
            {
                return _missions.FirstOrDefault(m => m.acsMissionId == acsMissionId);
            }
        }

        public List<Mission> GetByRunMissions(List<Mission> missions)
        {
            lock (_lock)
            {
                return missions.Where(m => m.state == nameof(MissionState.PENDING) || m.state == nameof(MissionState.EXECUTING) || m.state == nameof(MissionState.COMMANDREQUESTCOMPLETED)).ToList();
            }
        }

        public List<Parameter> GetParametas(List<Mission> missions)
        {
            //파라메타 내용을 찾을때 사용
            //1. parameters 가 null 인 Mission은 제외
            //2. List<Mission> → 모든 parameters 를 하나의 열로 평탄화
            //3. List<Parameter> 로 리턴
            lock (_lock)
            {
                return missions.Where(m => m.parameters != null).SelectMany(m => m.parameters).ToList();
            }
        }

        public Parameter FindParameterByKey(List<Parameter> parameters, string key)
        {
            if (parameters == null)
                return null;

            if (string.IsNullOrWhiteSpace(key))
                return null;

            string targetKey = key.ToUpper();

            foreach (var p in parameters)
            {
                if (p.key == null)
                    continue;

                if (p.key.ToUpper() == targetKey)
                    return p;
            }

            return null;
        }

        public List<Mission> FindSamePrameterMissions(List<Mission> missions, Mission newMission, string key)
        {
            // [결과] 동일한 파라미터(key 기준)가 발견된 기존 미션들을 담을 리스트
            var result = new List<Mission>();

            // [방어 1] 기존 미션 리스트가 null이면 비교 자체가 불가능 → 빈 결과 리턴
            if (missions == null) return result;

            // [방어 2] 새 미션이 null이면 비교 기준이 없으므로 → 빈 결과 리턴
            if (newMission == null) return result;

            // ------------------------------------------------------------
            // (디버깅/검증용) missions 전체에서 parameters를 평탄화해서 모아둠
            // - 실제 비교에는 직접 쓰지 않지만,
            //   "missions에 파라미터가 어떤게 들어있는지" 확인할 때 브레이크 포인트로 유용
            // - 디버깅: allParams.Count, allParams에서 key 존재 여부 확인
            // ------------------------------------------------------------
            var allParams = GetParametas(missions);

            // ------------------------------------------------------------
            // [방어 3] newMission.parameters가 null이면 빈 리스트로 만들어서 NullReference 방지
            // - 디버깅: newMission.parameters.Count 확인
            // ------------------------------------------------------------
            if (newMission.parameters == null)
                newMission.parameters = new List<Parameter>();

            // ------------------------------------------------------------
            // [기준 파라미터 찾기]
            // - 새 미션(newMission)에서 key(예: linkedZone) 파라미터를 찾는다.
            // - 이게 없으면 비교 기준이 없으므로 result는 빈 리스트로 리턴
            // - 디버깅:
            //    - key 값이 정확한지 ("linkedZone" 철자/대소문자)
            //    - newLinkedZone.values에 어떤 값이 있는지
            // ------------------------------------------------------------
            var newLinkedZone = FindParameterByKey(newMission.parameters, key);
            if (newLinkedZone == null)
                return result; // 새 미션에 linkedZone이 없으면 비교 불가(정책에 따라 변경 가능)

            // ------------------------------------------------------------
            // [기존 미션들을 순회하면서]
            // - 각 미션(m)의 key 파라미터와 newMission의 key 파라미터를 비교한다.
            // ------------------------------------------------------------
            foreach (var m in missions)
            {
                // [방어 4] missions 안에 null 요소가 있으면 스킵
                if (m == null) continue;

                // [방어 5] 기존 미션의 parameters가 null이면 빈 리스트로 만들어 NullReference 방지
                if (m.parameters == null)
                    m.parameters = new List<Parameter>();

                // --------------------------------------------------------
                // [기존 미션의 key 파라미터 찾기]
                // - 없으면 비교 불가 → 해당 미션은 스킵
                // - 디버깅: oldLinkedZone.values 확인
                // --------------------------------------------------------
                var oldLinkedZone = FindParameterByKey(m.parameters, key);
                if (oldLinkedZone == null)
                    continue;

                // --------------------------------------------------------
                // [values 비교]
                // - IsSameValues(...)가 true면 "동일 linkedZone"이라고 판단
                // - 결과 리스트에 해당 미션을 추가
                // - 디버깅:
                //    - oldLinkedZone.values vs newLinkedZone.values
                //    - IsSameValues 내부에서 정렬/Trim 정책 확인
                // --------------------------------------------------------
                if (IsSameValues(oldLinkedZone.values, newLinkedZone.values))
                    result.Add(m);
            }
            // 최종적으로 "동일 key 파라미터를 가진 기존 미션들" 반환
            return result;
        }

        private bool IsSameValues(List<string> a, List<string> b)
        {
            // ------------------------------------------------------------
            // [1] null 방어
            // - a 또는 b가 null이면 foreach에서 NullReferenceException 발생
            // - 정책적으로 null은 "값 없음"이므로 빈 리스트로 취급
            // - 디버깅: a/b가 null로 들어오는 케이스가 있는지 확인 포인트
            // ------------------------------------------------------------
            if (a == null) a = new List<string>();
            if (b == null) b = new List<string>();

            // ------------------------------------------------------------
            // [2] a 리스트 정규화(na 생성)
            // - 공백/빈 문자열 제거
            // - Trim()으로 앞뒤 공백 제거
            // - 디버깅: na에 어떤 값이 들어갔는지 확인(브레이크포인트)
            // ------------------------------------------------------------
            var na = new List<string>();
            foreach (var x in a)
            {
                // "", "   ", null 제거
                if (string.IsNullOrWhiteSpace(x)) continue;
                // " Z01 " -> "Z01"
                na.Add(x.Trim());
            }

            // ------------------------------------------------------------
            // [3] b 리스트 정규화(nb 생성)
            // - a와 동일한 규칙 적용
            // - 디버깅: nb에 어떤 값이 들어갔는지 확인
            // ------------------------------------------------------------
            var nb = new List<string>();
            foreach (var x in b)
            {
                if (string.IsNullOrWhiteSpace(x)) continue;
                nb.Add(x.Trim());
            }

            // ------------------------------------------------------------
            // [4] 정렬(순서 무시 비교를 위한 핵심)
            // - 입력 a, b는 원래 순서가 다를 수 있음
            // - 정렬해두면 같은 값 집합이면 같은 순서로 정렬됨
            // - StringComparer.Ordinal: 대소문자 구분 + 빠르고 명확한 비교
            // - 디버깅: 정렬 전/후 값 비교하면 왜 false인지 원인 찾기 쉬움
            // ------------------------------------------------------------
            na.Sort(StringComparer.Ordinal);
            nb.Sort(StringComparer.Ordinal);

            // ------------------------------------------------------------
            // [5] 개수 비교
            // - 값 개수가 다르면 절대 같을 수 없으므로 즉시 false
            // - 디버깅: 왜 Count가 다른지(공백 제거로 인해 줄었을 수도 있음)
            // ------------------------------------------------------------
            if (na.Count != nb.Count) return false;

            // ------------------------------------------------------------
            // [6] 같은 인덱스끼리 1:1 비교
            // - 정렬이 되어있으므로, 완전히 동일하면 모든 인덱스가 같아야 함
            // - StringComparison.Ordinal: 대소문자 구분
            // - 디버깅: i에서 처음 틀리는 지점 확인 가능
            // ------------------------------------------------------------
            for (int i = 0; i < na.Count; i++)
            {
                if (!string.Equals(na[i], nb[i], StringComparison.Ordinal))
                    return false;
            }
            // ------------------------------------------------------------
            // [7] 여기까지 왔으면 완전 동일
            // ------------------------------------------------------------
            return true;
        }
    }
}