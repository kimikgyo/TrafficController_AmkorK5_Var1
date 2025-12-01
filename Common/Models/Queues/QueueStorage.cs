using System.Collections.Concurrent;

namespace Common.Models.Queues
{
    public static class QueueStorage
    {
        #region MQTT

        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishOrder = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishJob = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishMission = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishPosition = new ConcurrentQueue<MqttPublishMessageDto>();

        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttMessagesSubscribe = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeWorker = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeMiddleware = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeCarrier = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeElevator = new ConcurrentQueue<MqttSubscribeMessageDto>();

        public static void MqttEnqueuePublishOrder(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishOrder.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishOrder(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishOrder.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishJob(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishJob.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishJob(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishJob.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishMission(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishMission.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishMission(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishMission.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishPosition(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishPosition.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishPosition(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishPosition.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeWorker(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeWorker.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeWorker(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeWorker.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeMiddleware(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeMiddleware.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeMiddleware(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeMiddleware.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeCarrier(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeCarrier.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeCarrier(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeCarrier.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeElevator(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeElevator.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeElevator(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeElevator.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribe(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttMessagesSubscribe.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribe(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttMessagesSubscribe.TryDequeue(out item);
        }

        #endregion MQTT
    }
}