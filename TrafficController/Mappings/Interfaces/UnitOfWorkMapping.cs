using JOB.Mappings.Areas;
using JTrafficControllerOB.Mappings.Bases;
using TrafficController.Mappings.Bases;
using TrafficController.Mappings.Missions;

namespace TrafficController.Mappings.Interfaces
{
    public class UnitOfWorkMapping : IUnitOfWorkMapping
    {
        public MissionMapping Missions { get; private set; }
        public WorkerMapping Workers { get; private set; }
        public PositionMapping Positions { get; private set; }
        public MapMapping Maps { get; private set; }
        public ACSAreaMapping ACSAreas { get; private set; }
        public UnitOfWorkMapping()
        {
            mapping();
        }
        
        private void mapping()
        {
            Missions = new MissionMapping();
            Workers = new WorkerMapping();
            Positions = new PositionMapping();
            Maps = new MapMapping();
            ACSAreas = new ACSAreaMapping();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}