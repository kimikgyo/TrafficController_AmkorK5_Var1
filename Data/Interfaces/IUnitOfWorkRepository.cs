using Data.Repositorys.Areas;
using Data.Repositorys.Jobs;
using Data.Repositorys.Maps;
using Data.Repositorys.Positions;
using Data.Repositorys.Services;
using Data.Repositorys.Workers;

namespace Data.Interfaces
{
    public interface IUnitOfWorkRepository : IDisposable
    {
        #region Base

        MapRepository Maps { get; }
        PositionRepository Positions { get; }
        WorkerRepository Workers { get; }

        #endregion Base

        MissionRepository Missions { get; }
        ACS_ZoneRepository ACSZones { get; }
        ServiceApiRepository ServiceApis { get; }

        void SaveChanges();
    }
}