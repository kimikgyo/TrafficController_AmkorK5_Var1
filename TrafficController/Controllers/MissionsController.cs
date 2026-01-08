using Common.DTOs.Rests.Missions;
using Common.Models;
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
        public ActionResult ActionResult([FromBody] Post_MissionDto post_Mission)
        {
            var mission = _repository.Missions.GetByACSMissionId(post_Mission.guid);
            if (mission == null)
            {
                var create = _mapping.Missions.Post(post_Mission);
                _repository.Missions.Add(create);
                _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.state, _mapping.Missions.Publish(create));
                return Ok(create);
            }
            else
                return NotFound();
        }

        //// PUT api/<ValuesController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<ValuesController>/5
        [HttpDelete("{Id}")]
        public ActionResult Delete(string Id)
        {
            var mission = _repository.Missions.GetById(Id);
            if (mission != null)
            {
                updateStateMission(mission, nameof(MissionState.CANCELED), true);
                _repository.Missions.Remove(mission);
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