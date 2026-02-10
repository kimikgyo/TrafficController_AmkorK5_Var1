using Common.DTOs.Rests.Missions;
using Common.Models;
using Common.Models.Bases;
using Common.Models.Missions;
using Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using TrafficController.Mappings.Interfaces;
using TrafficController.MQTTs.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TrafficController.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class missionsController : ControllerBase
    {
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitofWorkMqttQueue _mqttQueue;

        public missionsController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue)
        {
            _repository = repository;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
        }

        // GET: api/<ValuesController>
        [HttpGet]
        public ActionResult<List<Get_MissionDto>> Get()
        {
            try
            {
                List<Get_MissionDto> _responseDtos = new List<Get_MissionDto>();

                foreach (var mission in _repository.Missions.GetAll())
                {
                    var responce = _mapping.Missions.Get(mission);

                    _responseDtos.Add(responce);
                    //logger.Info($"{this.ControllerLogPath()} GetAll = {responceJob}");
                }

                return Ok(_responseDtos);
            }
            catch (Exception ex)
            {
                //LogExceptionMessage(ex);
                return NotFound();
            }
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public ActionResult<Get_MissionDto> Get(string id)
        {
            try
            {
                Get_MissionDto responseDto = null;

                var mission = _repository.Missions.GetById(id);
                if (mission != null)
                {
                    responseDto = _mapping.Missions.Get(mission);
                    //logger.Info($"{this.ControllerLogPath(id)} GetById = {responseDto}");
                    return Ok(responseDto);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                //LogExceptionMessage(ex);
                return NotFound();
            }
        }

        // POST api/<ValuesController>
        [HttpPost]
        public ActionResult Post([FromBody] Post_MissionDto post_Mission)
        {
            // ============================================================
            // [0] 기본 방어 (입력값 체크)
            // - 디버깅 포인트: post_Mission 값이 실제로 들어왔는지
            // ============================================================
            if (post_Mission == null) return BadRequest("post_Mission is null");
            if (string.IsNullOrWhiteSpace(post_Mission.guid)) return BadRequest("guid is empty");
            if (string.IsNullOrWhiteSpace(post_Mission.assignedWorkerId)) return BadRequest("assignedWorkerId is empty");

            // ============================================================
            // [1] GUID 중복 체크
            // - 같은 guid 가 이미 DB에 있으면 중복 요청으로 판단
            // - 디버깅 포인트: 동일 guid 요청이 반복해서 들어오는지
            // ============================================================
            var sameGuid = _repository.Missions.GetByACSMissionId(post_Mission.guid);
            // 기존 코드에서 NotFound 쓰고 있었는데 "이미 존재"는 Conflict가 자연스러움
            if (sameGuid != null) return Conflict($"Mission guid already exists: {post_Mission.guid}");

            // ============================================================
            // [2] DTO -> Entity 변환(매핑)
            // - create는 실제 DB에 저장할 Mission Entity
            // - 디버깅 포인트: create.parameters, create.assignedWorkerId 값 확인
            // ============================================================
            var create = _mapping.Missions.Post(post_Mission);
            if (create == null) return StatusCode(500, "Mapping result is null");
            if (create.parameters == null) create.parameters = new List<Parameter>();

            // ============================================================
            // [3] 같은 assignedWorkerId(같은 로봇/워커)에 할당된 기존 미션들 조회
            // - 디버깅 포인트: workerMissions 개수, 각 미션의 parameters 확인
            // ============================================================
            var workerMissions = _repository.Missions.GetByAssignedWorkerId(create.assignedWorkerId);
            if (workerMissions == null) workerMissions = new List<Mission>();

            // ============================================================
            // [4] linkedZone 기준으로 동일 미션 찾기
            // - FindSameParameterMissions(...) 는 "linkedZone 값이 같은 미션들"을 리턴
            // - 디버깅 포인트:
            //    - create.parameters 안에 linkedZone 키가 실제 존재하는지
            //    - duplicated에 몇 개가 담겼는지
            // ============================================================
            var duplicated =_repository.Missions.FindSamePrameterMissions(workerMissions, create, "linkedZone").ToList();

            // ============================================================
            // [5] 기존 동일 미션 삭제
            // - Remove가 void 이면 성공/실패를 bool로 받을 수 없음 (CS0029 원인)
            // - 디버깅 포인트:
            //    - old.guid 값 확인
            //    - Remove 내부에서 예외가 나는지 try/catch로 확인 가능
            // ============================================================
            foreach (var old in duplicated)
            {
                try
                {
                    _repository.Missions.Remove(old); // ✅ void 리턴
                }
                catch (Exception ex)
                {
                    // 삭제 실패 시 더 진행하면 꼬일 수 있으므로 즉시 중단
                    return StatusCode(500, $"Failed to delete mission guid={old?.guid}, err={ex.Message}");
                }
            }

            // ============================================================
            // [6] 새 미션 생성
            // - 위에서 동일 미션들은 삭제했으니, 이번 POST로 받은 미션만 Add
            // - 디버깅 포인트: Add 직후 create.guid, create.state 확인
            // ============================================================
            _repository.Missions.Add(create);

            // ============================================================
            // [7] MQTT Publish (생성/상태 전파)
            // - 디버깅 포인트: Publish payload 값, topic 확인
            // ============================================================
            _mqttQueue.MqttPublishMessage(TopicType.mission,TopicSubType.state,_mapping.Missions.Publish(create)
);

            return Ok(create);
        }

        //// PUT api/<ValuesController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<ValuesController>/5
        [HttpDelete("{acsMissionId}")]
        public ActionResult Delete(string acsMissionId)
        {
            var mission = _repository.Missions.GetByACSMissionId(acsMissionId);
            if (mission != null)
            {
                if (mission.state == nameof(MissionState.COMPLETED))
                {
                    _repository.Missions.Remove(mission);
                }
                else
                {
                    updateStateMission(mission, nameof(MissionState.COMPLETED), true);
                    _repository.Missions.Remove(mission);
                }
                return Ok(mission);
            }
            else
                return NotFound();
        }

        private void updateStateMission(Mission mission, string state, bool historyAdd = false)
        {
            if (mission.state != state)
            {
                mission.state = state;

                switch (mission.state)
                {
                    case nameof(MissionState.INIT):
                    case nameof(MissionState.WORKERASSIGNED):
                    case nameof(MissionState.WAITING):
                    case nameof(MissionState.COMMANDREQUEST):
                    case nameof(MissionState.COMMANDREQUESTCOMPLETED):
                    case nameof(MissionState.PENDING):
                    case nameof(MissionState.EXECUTING):
                    case nameof(MissionState.FAILED):
                    case nameof(MissionState.ABORTINITED):
                    case nameof(MissionState.ABORTFAILED):
                    case nameof(MissionState.CANCELINITED):
                    case nameof(MissionState.CNACELFAILED):
                    case nameof(MissionState.COMPLETED):
                        mission.updatedAt = DateTime.Now;
                        break;

                    case nameof(MissionState.SKIPPED):
                    case nameof(MissionState.ABORTCOMPLETED):
                    case nameof(MissionState.CANCELINITCOMPLETED):
                    case nameof(MissionState.CANCELED):
                        mission.finishedAt = DateTime.Now;
                        break;
                }

                _repository.Missions.Update(mission);
                //if (historyAdd) _repository.MissionHistorys.Add(mission);
                _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.status, _mapping.Missions.Publish(mission));
            }
        }
    }
}