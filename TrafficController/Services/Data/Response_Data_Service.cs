using Common.DTOs.Rests.Maps;
using Common.DTOs.Rests.Positions;
using Common.DTOs.Rests.Workers;
using Common.DTOs.Rests.Zone;
using Common.Models;
using Common.Models.Bases;
using Common.Models.Zone;
using Data.Interfaces;
using Data.Repositorys.Bases;
using log4net;
using RestApi.Interfases;
using System.Data;
using System.Diagnostics;
using TrafficController.Mappings.Interfaces;

namespace TrafficController.Services
{
    public class Response_Data_Service
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent");
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");

        public readonly IUnitOfWorkRepository _repository;
        public readonly IUnitOfWorkMapping _mapping;
        public readonly ILog _eventlog;
        public List<MqttTopicSubscribe> mqttTopicSubscribes = new List<MqttTopicSubscribe>();

        public Response_Data_Service(ILog eventLog, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mapping = mapping;
            _eventlog = eventLog;
        }

        public async Task<bool> StartAsyc()
        {
            bool Complete = false;
            bool Resource = false;

            bool GetWorkerData = false;
            bool ResourceData = false;
            while (!Complete)
            {
                try
                {
                    ApiClient();
                    foreach (var serviceApi in _repository.ServiceApis.GetAll())
                    {
                        if (serviceApi.type == "Resource")
                        {
                            _repository.Workers.Delete();
                            _repository.Maps.Delete();
                            _repository.Positions.Delete();
                            _repository.ACSZones.Delete();
                            var Workers = await serviceApi.Api.Get_Worker_Async();
                            var Maps = await serviceApi.Api.Get_Map_Async();
                            var Positions = await serviceApi.Api.Get_Position_Async();
                            var AcsZones = await serviceApi.Api.Get_ACSZone_Async();

                            if (Workers == null)
                            {
                                _eventlog.Info($"{nameof(Workers)}GetDataFail");
                                break;
                            }
                            else if (Maps == null)
                            {
                                _eventlog.Info($"{nameof(Maps)}GetDataFail");
                                break;
                            }
                            else if (Positions == null)
                            {
                                _eventlog.Info($"{nameof(Positions)}GetDataFail");
                                break;
                            }
                            else if (AcsZones == null)
                            {
                                _eventlog.Info($"{nameof(AcsZones)}GetDataFail");
                                break;
                            }
                            else
                            {
                                foreach (var getmap in Maps)
                                {
                                    var map = _mapping.Maps.Response(getmap);
                                    _repository.Maps.Add(map);
                                }

                                foreach (var getworker in Workers)
                                {
                                    var worker = _mapping.Workers.Response(getworker);
                                    _repository.Workers.Add(worker);
                                }

                                foreach (var getPosition in Positions)
                                {
                                    var position = _mapping.Positions.Response(getPosition);
                                    _repository.Positions.Add(position);
                                }
                                foreach (var getAcsZone in AcsZones)
                                {
                                    var acsZone = _mapping.ACSZones.Response(getAcsZone);
                                    _repository.ACSZones.FillAreaCacheOne(acsZone);
                                    _repository.ACSZones.Add(acsZone);
                                }
                                Resource = true;
                            }
                        }
                    }
                    Resource = true;
                    if (Resource)
                    {
                        Complete = true;
                        ConfigData.SubscribeTopics = suscreibeTopicsAdd();
                        _eventlog.Info($"GetData{nameof(Complete)}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                    await Task.Delay(500);
                }
            }

            return Complete;
        }

        private List<MqttTopicSubscribe> suscreibeTopicsAdd()
        {
            mqttTopicSubscribes.Clear();
            var workers = _repository.Workers.GetAll();
            foreach (var worker in workers)
            {
                var statetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{worker.id}/state"
                };
                var missiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{worker.id}/mission"
                };
                mqttTopicSubscribes.Add(statetopic);
                mqttTopicSubscribes.Add(missiontopic);

                if (worker.isMiddleware)
                {
                    var middlewarestatetopic = new MqttTopicSubscribe
                    {
                        topic = $"acs/middleware/{worker.id}/state"
                    };
                    var middlewaremissiontopic = new MqttTopicSubscribe
                    {
                        topic = $"acs/middleware/{worker.id}/mission"
                    };
                    mqttTopicSubscribes.Add(middlewarestatetopic);
                    mqttTopicSubscribes.Add(middlewaremissiontopic);
                }
            }
            return mqttTopicSubscribes;
        }

        public async Task<bool> ReloadAsyc()
        {
            bool Complete = false;
            bool Resource = false;

            bool GetWorkerData = false;
            bool ResourceData = false;
            while (!Complete)
            {
                try
                {
                    foreach (var serviceApi in _repository.ServiceApis.GetAll())
                    {
                        if (serviceApi.type == "Resource")
                        {
                            var getReloadWorkers = await serviceApi.Api.Get_Worker_Async();
                            var getReloadMaps = await serviceApi.Api.Get_Map_Async();
                            var getReloadPositions = await serviceApi.Api.Get_Position_Async();
                            var getAcsZones = await serviceApi.Api.Get_ACSZone_Async();
                            if (getReloadWorkers == null)
                            {
                                _eventlog.Info($"{nameof(getReloadWorkers)}GetDataFail");
                                break;
                            }
                            else if (getReloadMaps == null)
                            {
                                _eventlog.Info($"{nameof(getReloadMaps)}GetDataFail");
                                break;
                            }
                            else if (getReloadPositions == null)
                            {
                                _eventlog.Info($"{nameof(getReloadPositions)}GetDataFail");
                                break;
                            }
                            else if (getAcsZones == null)
                            {
                                _eventlog.Info($"{nameof(getAcsZones)}GetDataFail");
                                break;
                            }
                            else
                            {
                                ReloadMap(getReloadMaps);
                                ReloadWorker(getReloadWorkers);
                                ReloadPosition(getReloadPositions);
                                ReloadACSZone(getAcsZones);
                                Resource = true;
                            }
                        }
                    }
                    if (Resource)
                    {
                        Complete = true;
                        ConfigData.SubscribeTopics = mqttTopicSubscribes;
                        _eventlog.Info($"GetData{nameof(Complete)}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                    await Task.Delay(500);
                }
            }

            return Complete;
        }

        private void ReloadACSZone(List<Response_ACSZoneDto> response_ACS_ZoneDtos)
        {
            List<ACSZone> Reload = new List<ACSZone>();

            //update Add
            foreach (var response_ACS_ZoneDto in response_ACS_ZoneDtos)
            {
                Reload.Add(_mapping.ACSZones.Response(response_ACS_ZoneDto));
            }

            var ReloadId = Reload.Select(x => x.zoneId);
            var aCSZones = _repository.ACSZones.GetAll();
            var aCSAreaIds = aCSZones.Select(x => x.zoneId);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddACSZones = Reload.Where(x => !aCSAreaIds.Contains(x.zoneId)).ToList();
            foreach (var AddACSZone in AddACSZones)
            {
                _repository.ACSZones.FillAreaCacheOne(AddACSZone);
                _repository.ACSZones.Add(AddACSZone);
            }

            //기존데이터 기준 새로운 데이터와 같은것 업데이트
            foreach (var aCSZone in aCSZones)
            {
                var Update = Reload.FirstOrDefault(x => x.zoneId == aCSZone.zoneId);
                if (Update != null)
                {
                    aCSZone.zoneId = Update.zoneId;
                    aCSZone.mapId = Update.mapId;
                    aCSZone.name = Update.name;
                    aCSZone.type = Update.type;
                    aCSZone.subType = Update.subType;
                    aCSZone.groupId = Update.groupId;
                    aCSZone.linkedNode = Update.linkedNode;
                    aCSZone.polygon = Update.polygon;
                    aCSZone.isDisplayed = Update.isDisplayed;
                    aCSZone.isEnabled = Update.isEnabled;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var removes = aCSZones.Where(x => !ReloadId.Contains(x.zoneId)).ToList();
            foreach (var remove in removes)
            {
                _repository.ACSZones.Remove(remove);
            }
        }

        private void ReloadMap(List<Response_MapDto> dtoResourceMaps)
        {
            List<Map> Reload = new List<Map>();
            //update Add
            foreach (var dtoResourceMap in dtoResourceMaps)
            {
                Reload.Add(_mapping.Maps.Response(dtoResourceMap));
            }

            var ReloadId = Reload.Select(x => x.id).ToList();
            var maps = _repository.Maps.GetAll();
            var mapIds = maps.Select(x => x.id);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddMaps = Reload.Where(x => !ReloadId.Contains(x.id)).ToList();

            foreach (var AddMap in AddMaps)
            {
                _repository.Maps.Add(AddMap);
            }

            //기존데이터 기준 새로운 데이터와 같은것 업데이트
            foreach (var map in maps)
            {
                var reloadmap = Reload.FirstOrDefault(x => x.id == map.id);
                if (reloadmap != null)
                {
                    map.mapId = reloadmap.mapId;
                    map.source = reloadmap.source;
                    map.level = reloadmap.level;
                    map.name = reloadmap.name;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var removedateMaps = maps.Where(x => !ReloadId.Contains(x.id)).ToList();
            foreach (var removedateMap in removedateMaps)
            {
                _repository.Maps.Remove(removedateMap);
            }
        }

        private void ReloadWorker(List<Response_WorkerDto> dtoResourceWorkers)
        {
            List<Worker> Reload = new List<Worker>();

            foreach (var dtoResourceWorker in dtoResourceWorkers)
            {
                var ReloadWorker = _mapping.Workers.Response(dtoResourceWorker);
                Reload.Add(ReloadWorker);
            }

            var ReloadId = Reload.Select(x => x.id);
            var workers = _repository.Workers.GetAll();
            var workerIds = workers.Select(x => x.id);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddWorkers = Reload.Where(x => !workerIds.Contains(x.id)).ToList();

            foreach (var AddWorker in AddWorkers)
            {
                _repository.Workers.Add(AddWorker);
                var statetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{AddWorker.id}/state"
                };
                var missiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{AddWorker.id}/mission"
                };
                mqttTopicSubscribes.Add(statetopic);
                mqttTopicSubscribes.Add(missiontopic);
            }
            foreach (var worker in workers)
            {
                var Update = Reload.FirstOrDefault(x => x.id == worker.id);
                if (Update != null)
                {
                    worker.id = Update.id;
                    worker.source = Update.source;
                    worker.name = Update.name;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var RemoveWorkers = workers.Where(x => !ReloadId.Contains(x.id)).ToList();

            foreach (var RemoveWorker in RemoveWorkers)
            {
                _repository.Workers.Remove(RemoveWorker);
                var statetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{RemoveWorker.id}/state"
                };
                var missiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{RemoveWorker.id}/mission"
                };
                mqttTopicSubscribes.Remove(statetopic);
                mqttTopicSubscribes.Remove(missiontopic);
            }
        }

        private void ReloadPosition(List<Response_PositionDto> dtoResourcePositions)
        {
            List<Position> Reload = new List<Position>();
            //update Add
            foreach (var dtoResourcePosition in dtoResourcePositions)
            {
                Reload.Add(_mapping.Positions.Response(dtoResourcePosition));
            }

            var ReloadId = Reload.Select(x => x.id);
            var positions = _repository.Positions.MiR_GetAll();
            var positionIds = positions.Select(x => x.id);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddPositions = Reload.Where(x => !positionIds.Contains(x.id)).ToList();

            foreach (var AddPosition in AddPositions)
            {
                _repository.Positions.Add(AddPosition);
            }

            //기존데이터 기준 새로운 데이터와 같은것 업데이트
            foreach (var position in positions)
            {
                var Update = Reload.FirstOrDefault(x => x.id == position.id);
                if (Update != null)
                {
                    position.source = Update.source;
                    position.group = Update.group;
                    position.type = Update.type;
                    position.subType = Update.subType;
                    position.mapId = Update.mapId;
                    position.name = Update.name;
                    position.x = Update.x;
                    position.y = Update.y;
                    position.theth = Update.theth;
                    position.isDisplayed = Update.isDisplayed;
                    position.isEnabled = Update.isEnabled;
                    position.linkedFacility = Update.linkedFacility;
                    position.linkedRobotId = Update.linkedRobotId;
                    position.hasCharger = Update.hasCharger;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var removes = positions.Where(x => !ReloadId.Contains(x.id)).ToList();
            foreach (var remove in removes)
            {
                _repository.Positions.Remove(remove);
            }
        }

        private void ApiClient()
        {
            //Config 파일을 불러온다
            foreach (var apiInfo in ConfigData.ServiceApis)
            {
                var serviceInfo = _repository.ServiceApis.GetByIpPort(apiInfo.ip, apiInfo.port);
                if (serviceInfo == null)
                {
                    var client = new Api(apiInfo.type, apiInfo.ip, apiInfo.port, double.Parse(apiInfo.timeOut), apiInfo.connectId, apiInfo.connectPassword);
                    apiInfo.Api = client;
                    _repository.ServiceApis.Add(apiInfo);
                }
            }
        }



        public void LogExceptionMessage(Exception ex)
        {
            //string message = ex.InnerException?.Message ?? ex.Message;
            //string message = ex.ToString();
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            _eventlog.Info(message);
        }
    }
}