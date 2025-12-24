using Common.DTOs.Rests.Maps;
using Common.DTOs.Rests.Positions;
using Common.DTOs.Rests.Workers;
using Common.DTOs.Rests.Zone;
using Common.Models.Bases;

namespace Common.Interfaces
{
    public interface IApi
    {
        Uri BaseAddress { get; }

        Task<List<Response_WorkerDto>> Get_Worker_Async();

        Task<List<Response_MapDto>> Get_Map_Async();

        Task<List<Response_PositionDto>> Get_Position_Async();

        Task<List<Response_ACSZoneDto>> Get_ACSZone_Async();

        Task<ApiResponseDto> Post_Worker_Mission_Async(object value);

        Task<ApiResponseDto> Post_Elevator_Mission_Async(object value);

        Task<ApiResponseDto> Post_Middleware_Mission_Async(object value);

        Task<ApiResponseDto> Delete_Worker_Mission_Async(string id);

        Task<ApiResponseDto> Delete_Middleware_Mission_Async(string id);

        Task<ApiResponseDto> Deletet_Elevator_Mission_Async(string id);

        Task<ApiResponseDto> Patch_Position_Async(string id, object value);
    }
}