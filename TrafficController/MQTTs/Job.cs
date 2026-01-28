using Common.DTOs.MQTTs.Jobs;
using Common.Models;
using Common.Models.Missions;
using Common.Models.Queues;
using System.Text.Json;

namespace TrafficController.MQTTs
{
    public partial class MqttProcess
    {
        public void SubscribeJob()
        {
            while (QueueStorage.MqttTryDequeueSubscribeJob(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    var state = JsonSerializer.Deserialize<Subscribe_JobDto>(subscribe.Payload!);

                    //if (state == null || string.IsNullOrWhiteSpace(state.assignedWorkerId)) return;
                    //var missions = _repository.Missions.GetByAssignedWorkerId(state.assignedWorkerId);
                    //if (missions == null || missions.Count == 0) return;
                    //var CompletedMission = missions.FirstOrDefault(m => m.jobId != state.guid);
                    //if (CompletedMission != null)
                    //{
                    //    updateStateMission(CompletedMission, nameof(MissionState.COMPLETED), true);
                    //    _repository.Missions.Remove(CompletedMission);
                    //}
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}