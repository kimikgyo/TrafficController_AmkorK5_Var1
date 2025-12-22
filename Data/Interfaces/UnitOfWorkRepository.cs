using Data.Repositorys.Areas;
using Data.Repositorys.Jobs;
using Data.Repositorys.Maps;
using Data.Repositorys.Positions;
using Data.Repositorys.Services;
using Data.Repositorys.Workers;
using System.Data;

namespace Data.Interfaces
{
    public class ConnectionStrings
    {
        //public static readonly string DB1 = @"Data SOURCE=.\SQLEXPRESS;Initial Catalog=AmkorK5_TrafficController; User ID = sa;TrustServerCertificate=true; Password=acsserver;Connect Timeout=30;";
        //public static readonly string DB1 = @"Data Source=192.168.8.215,1433; Initial Catalog=JobScheduler; User ID = sa; Password=acsserver; Connect Timeout=30; TrustServerCertificate=true"; // STI
        public static readonly string DB1 = @"Data SOURCE=192.168.1.200,1433;Initial Catalog=AmkorK5_TrafficController; User ID = sa;TrustServerCertificate=true; Password=acsserver;Connect Timeout=30;";
    }

    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private IDbConnection _db;

        private static readonly string connectionString = ConnectionStrings.DB1;

        #region Base

        public MapRepository Maps { get; private set; }
        public WorkerRepository Workers { get; private set; }
        public PositionRepository Positions { get; private set; }

        #endregion Base

        public MissionRepository Missions { get; private set; }
        public ACS_AreaRepository ACSAreas { get; private set; }
        public ServiceApiRepository ServiceApis { get; private set; }

        public UnitOfWorkRepository()
        {
            repository();
        }

        private void repository()
        {
            #region Base

            Maps = new MapRepository(connectionString);
            Workers = new WorkerRepository(connectionString);
            Positions = new PositionRepository(connectionString);

            #endregion Base

            Missions = new MissionRepository(connectionString);

            ServiceApis = new ServiceApiRepository(connectionString);
            ACSAreas = new ACS_AreaRepository(connectionString);
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}