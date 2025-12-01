using Common.Models;

namespace TrafficController.MQTTs.Interfaces
{
    public interface IUnitofWorkMqttQueue
    {
        void MqttPublishMessage(TopicType topicType, TopicSubType topicSubType, object value);

        void HandleReceivedMqttMessage();
    }
}