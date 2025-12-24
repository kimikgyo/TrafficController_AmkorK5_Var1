using Common.DTOs.Rests.Zone;
using Common.Models.Zone;

namespace JOB.Mappings.Areas
{
    public class ACSZoneMapping
    {
        public ACSZone Response(Response_ACSZoneDto response_ACS_AareDto)
        {
            var response = new ACSZone
            {
                zoneId = response_ACS_AareDto.zoneId,
                mapId = response_ACS_AareDto.mapId,
                name = response_ACS_AareDto.name,
                type = response_ACS_AareDto.type,
                subType = response_ACS_AareDto.subType,
                groupId = response_ACS_AareDto.groupId,
                linkedNode = response_ACS_AareDto.linkedNode,
                polygon = response_ACS_AareDto.polygon,
                isDisplayed = response_ACS_AareDto.isDisplayed,
                isEnabled = response_ACS_AareDto.isEnabled
            };
            return response;
        }
    }
}