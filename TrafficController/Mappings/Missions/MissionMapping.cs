using Common.DTOs.MQTTs.Missions;
using Common.DTOs.Rests.Missions;
using Common.Models.Missions;

namespace TrafficController.Mappings.Missions
{
    public class MissionMapping
    {
        public Get_MissionDto Get(Mission model)
        {
            var response = new Get_MissionDto()
            {
                guid = model.guid,
                trafficWorker = model.trafficWorker,
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt,

                orderId = model.orderId,
                jobId = model.jobId,
                acsMissionId = model.acsMissionId,
                carrierId = model.carrierId,
                name = model.name,
                service = model.service,
                type = model.type,
                subType = model.subType,
                sequence = model.sequence,
                linkedFacility = model.linkedFacility,
                isLocked = model.isLocked,
                sequenceChangeCount = model.sequenceChangeCount,
                retryCount = model.retryCount,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,

                parameters = model.parameters,
                preReports = model.preReports,
                postReports = model.postReports
            };

            return response;
        }

        public Publish_MissionDto Publish(Mission model)
        {
            var publish = new Publish_MissionDto()
            {
                guid = model.guid,
                trafficWorker = model.trafficWorker,
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt,

                orderId = model.orderId,
                jobId = model.jobId,
                acsMissionId = model.acsMissionId,
                carrierId = model.carrierId,
                name = model.name,
                service = model.service,
                type = model.type,
                subType = model.subType,
                sequence = model.sequence,
                linkedFacility = model.linkedFacility,
                isLocked = model.isLocked,
                sequenceChangeCount = model.sequenceChangeCount,
                retryCount = model.retryCount,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                parameters = model.parameters,
                preReports = model.preReports,
                postReports = model.postReports
            };
            return publish;
        }

        public Mission Post(Post_MissionDto post_Mission)
        {
            var create = new Mission
            {
                guid = Guid.NewGuid().ToString(),
                createdAt = DateTime.Now,

                orderId = post_Mission.orderId,
                jobId = post_Mission.jobId,
                acsMissionId = post_Mission.guid,
                carrierId = post_Mission.carrierId,
                name = post_Mission.name,
                service = post_Mission.service,
                type = post_Mission.type,
                subType = post_Mission.subType,
                sequence = post_Mission.sequence,
                linkedFacility = post_Mission.linkedFacility,
                isLocked = post_Mission.isLocked,
                sequenceChangeCount = post_Mission.sequenceChangeCount,
                retryCount = post_Mission.retryCount,
                state = nameof(MissionState.PENDING),
                specifiedWorkerId = post_Mission.specifiedWorkerId,
                assignedWorkerId = post_Mission.assignedWorkerId,
                parameters = post_Mission.parameters,
            };

            return create;
        }
    }
}