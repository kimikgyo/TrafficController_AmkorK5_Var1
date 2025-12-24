using Common.Models.Bases;

namespace Common.DTOs.Rests.Zone
{
    public class Response_ACSZoneDto
    {
        public string zoneId { get; set; }
        public string mapId { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public string groupId { get; set; }
        public string linkedNode { get; set; }
        public List<Point2D> polygon { get; set; } = new List<Point2D>();

        public bool isDisplayed { get; set; }
        public bool isEnabled { get; set; }
        public DateTime careatedAt { get; set; }
        public DateTime updatedAt { get; set; }

        public override string ToString()
        {
            // 전체 꼭지점 출력
            string points = string.Join(" | ", polygon.Select(p => $"({p.X:0.###},{p.Y:0.###})"));

            // (선택) Bounding Box도 같이 출력하면 디버깅에 매우 도움
            // polygon이 비어있을 수 있으니 Count로 체크 (Any 사용 안 함)

            string bbox = "";
            if (polygon.Count > 0)
            {
                double minX = polygon.Min(p => p.X);
                double maxX = polygon.Max(p => p.X);
                double minY = polygon.Min(p => p.Y);
                double maxY = polygon.Max(p => p.Y);

                bbox = $",bbox=({minX:0.###},{minY:0.###})~({maxX:0.###},{maxY:0.###})";
            }

            return
                $"zoneId = {zoneId,-5}" +
                $",mapId = {mapId,-5}" +
                $",name = {name,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",groupId = {groupId,-5}" +
                $",linkedNode = {linkedNode,-5}" +
                $",isDisplayed = {isDisplayed,-5}" +
                $",isEnabled = {isEnabled,-5}" +
                $",polygonCount = {polygon.Count}" + bbox +
                $",polygon = {points}" +
                $",careatedAt = {careatedAt,-5}" +
                $",updatedAt = {updatedAt,-5}";
        }
    }
}