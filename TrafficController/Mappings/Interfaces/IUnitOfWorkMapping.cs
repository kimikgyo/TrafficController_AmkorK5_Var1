using JOB.Mappings.Areas;
using JTrafficControllerOB.Mappings.Bases;
using TrafficController.Mappings.Bases;
using TrafficController.Mappings.Missions;

namespace TrafficController.Mappings.Interfaces
{
    public interface IUnitOfWorkMapping : IDisposable
    {
        MissionMapping Missions { get; }
        WorkerMapping Workers { get; }
        PositionMapping Positions { get; }
        MapMapping Maps { get; }
        ACSAreaMapping ACSAreas { get; }
    }
}