using Common.Models.Bases;

namespace Common.Models.Zone
{
    public class ACSZone
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


        // ------------------------------------------------------------
        // 런타임 캐시 필드(계산용) - API 직렬화에서 제외
        // ------------------------------------------------------------
        /// <summary>
        /// Bounding Box 최소 X
        /// - polygon 꼭지점들 중 가장 작은 X
        /// - 판정 시 1차 필터(빠른 컷)에 사용
        /// </summary>
        public double minX { get; set; }
        /// <summary>
        /// Bounding Box 최대 X
        /// - polygon 꼭지점들 중 가장 큰 X
        /// </summary>
        public double maxX { get; set; }
        /// <summary>
        /// Bounding Box 최소 Y
        /// - polygon 꼭지점들 중 가장 작은 Y
        /// </summary>
        public double minY { get; set; }
        /// <summary>
        /// Bounding Box 최대 Y
        /// - polygon 꼭지점들 중 가장 큰 Y
        /// </summary>
        public double maxY { get; set; }

        /// <summary>
        /// 폴리곤 꼭지점 X 좌표 배열(캐시)
        /// - xs[i], ys[i]가 i번째 꼭지점
        /// - List<Point2D>보다 반복 판정(PIP)에서 빠름
        /// </summary>
        public double[] xs { get; set; } = new double[0];
        /// <summary>
        /// 폴리곤 꼭지점 Y 좌표 배열(캐시)
        /// - xs.Length == ys.Length
        /// </summary>
        public double[] ys { get; set; } = new double[0];

        /// <summary>
        /// 캐시가 준비되었는지 여부
        /// - 프로그램 시작 시(또는 Area Reload 시) polygon을 기반으로
        ///   xs/ys/bbox를 계산한 뒤 true로 만들어 둔다.
        /// - polygon이 비정상(점 < 3, null 등)이면 false로 두고 판정에서 스킵한다.
        /// </summary>
        public bool cacheReady { get; set; }

        // ------------------------------------------------------------
        // [선택 옵션] - 운영 튐 방지/겹침 처리(원하면 추가)
        // ------------------------------------------------------------

        /// <summary>
        /// 경계 판정 오차(m)
        /// - 점이 폴리곤 경계선(edge) 근처에 있으면 inside로 인정하는 여유값
        /// - 좌표 노이즈로 인한 In/Out 튐 방지에 도움
        /// </summary>
        public double epsilonMeters { get; set; } = 0.02;

        /// <summary>
        /// Exit 히스테리시스(m)
        /// - Inside 상태에서 outside가 되더라도, 일정 거리 이상 확실히 멀어졌을 때만 Exit 처리
        /// - 경계 부근에서 Exit/Enter 반복되는 현상 방지
        /// </summary>
        public double hysteresisMeters { get; set; } = 0.05;

        /// <summary>
        /// 우선순위(겹치는 Area 처리용)
        /// - Area가 겹칠 때 정책 적용 순서를 정할 수 있음
        /// - 값이 클수록 우선(팀 룰에 맞게 정의)
        /// </summary>
        public int priority { get; set; } = 0;


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
                $",polygonCount = {polygon.Count}" + bbox +
                $",polygon = {points}" +
                $",cacheReady = {cacheReady}" +
                $",epsilonMeters = {epsilonMeters}" +
                $",hysteresisMeters = {hysteresisMeters}" +
                $",isDisplayed = {isDisplayed,-5}" +
                $",isEnabled = {isEnabled,-5}";
        }
    }
}