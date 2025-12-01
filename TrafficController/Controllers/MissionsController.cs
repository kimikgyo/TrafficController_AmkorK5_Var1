using Common.DTOs.Rests.Missions;
using Common.Models;
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

        //// GET: api/<ValuesController>
        //[HttpGet]
        //public ActionResult<Get_MissionDto> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/<ValuesController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

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
        [HttpDelete("{acsMissionId}")]
        public ActionResult Delete(string acsMissionId)
        {
            var mission = _repository.Missions.GetByACSMissionId(acsMissionId);
            if (mission != null)
            {
                _repository.Missions.Remove(mission);
                return Ok(mission);
            }
            else
                return NotFound();
        }
    }
}