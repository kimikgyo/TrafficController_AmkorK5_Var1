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
               IF OBJECT_id('dbo.[Mission]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[Mission]
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
                foreach (var data in con.Query<Mission>("SELECT * FROM [Mission]"))
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
                            INSERT INTO [Mission]
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
                            UPDATE [Mission]
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
                    con.Execute("DELETE FROM [Mission]");
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
                    con.Execute("DELETE FROM [Mission] WHERE guid = @guid", param: new { guid = remove.guid });
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

    }
}