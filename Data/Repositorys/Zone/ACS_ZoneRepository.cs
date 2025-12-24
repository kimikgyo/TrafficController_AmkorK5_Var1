using Common.Models.Bases;
using Common.Models.Zone;
using log4net;

namespace Data.Repositorys.Areas
{
    public class ACS_ZoneRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("ACS_Zone"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<ACSZone> _aCS_Areas = new List<ACSZone>(); // cached data
        private readonly object _lock = new object();

        public ACS_ZoneRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Add(ACSZone add)
        {
            lock (_lock)
            {
                _aCS_Areas.Add(add);
                logger.Info($"Add: {add}");
            }
        }

        public void update(ACSZone update)
        {
            lock (_lock)
            {
                logger.Info($"update: {update}");
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                _aCS_Areas.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(ACSZone remove)
        {
            lock (_lock)
            {
                _aCS_Areas.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<ACSZone> GetAll()
        {
            lock (_lock)
            {
                return _aCS_Areas.ToList();
            }
        }

        public ACSZone GetById(string Id)
        {
            lock (_lock)
            {
                return _aCS_Areas.FirstOrDefault(a => a.zoneId == Id);
            }
        }
        /*
        /// <summary>
        /// 로봇 좌표 (rx, ry)가 Area 사각형 안에 있는지 체크
        /// </summary>
        public bool IsInsideArea(double rx, double ry, ACSZone area, double margin = 0.0)
        {
            // ----------------------------------------------
            // 1) 사각형의 절반 크기 계산
            // ----------------------------------------------
            // area.Width   : 전체 가로 길이(m)
            // area.Height  : 전체 세로 길이(m)
            //
            // 사각형은 중심을 기준으로 양쪽으로 퍼져 있으므로
            // 절반 크기(half width/height)를 계산한다.
            //
            // margin       : 좌표 오차를 허용하기 위해 살짝 확장할 여유 값
            //                ex) margin = 0.2 면 사각형을 0.2m 확대해서 체크
            //
            double halfW = area.width / 2.0 + margin;   // 중심 기준 가로 반경
            double halfH = area.height / 2.0 + margin;  // 중심 기준 세로 반경

            // ----------------------------------------------
            // 2) X축 방향으로 Area 안에 있는지 확인
            // ----------------------------------------------
            // rx - area.CenterX  : 로봇이 중심에서 좌우로 얼마나 떨어져 있는지 계산
            // Math.Abs(...)      : 음수/양수 제거 → 거리만 비교
            // <= halfW           : 가로 반경(halfW) 안이면 X축 기준으로 '안에 있음'
            //
            bool insideX = Math.Abs(rx - area.x) <= halfW;

            // ----------------------------------------------
            // 3) Y축 방향으로 Area 안에 있는지 확인
            // ----------------------------------------------
            // ry - area.CenterY  : 로봇이 중심에서 위/아래로 얼마나 떨어져 있는지
            // halfH              : 세로 반경
            //
            bool insideY = Math.Abs(ry - area.y) <= halfH;

            // ----------------------------------------------
            // 4) 최종 판정
            // ----------------------------------------------
            // 사각형 내부에 있으려면 X, Y 축 모두 범위 안에 있어야 한다.
            // 즉, 두 조건을 모두 만족해야 inside = true.
            //
            // insideX = true AND insideY = true → Area 내부
            // 하나라도 false → Area 바깥
            //
            return insideX && insideY;
        }
        */
        /// <summary>
        /// [단건] ACSZone 캐시 채우기
        /// - 매핑 직후 호출
        /// - disabled/invalid Area는 cacheReady=false로 유지한다(스킵하지 않음)
        /// - 정상 polygon만 캐시(xs/ys/bbox)를 만들고 cacheReady=true로 만든다
        ///
        /// 반환:
        /// - true  : cacheReady=true (캐시 준비 완료)
        /// - false : cacheReady=false (disabled 또는 invalid)
        /// </summary>
        public bool FillAreaCacheOne(ACSZone area)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------
            if (area == null)
            {
                logger.Warn("[AREA][CACHE][ONE][SKIP] area is null");
                return false;
            }

            // ------------------------------------------------------------
            // 1) 리로드/재사용 대비 캐시 기본값 초기화
            // ------------------------------------------------------------
            area.cacheReady = false;
            area.xs = new double[0];
            area.ys = new double[0];
            area.minX = 0;
            area.maxX = 0;
            area.minY = 0;
            area.maxY = 0;

            // ------------------------------------------------------------
            // 2) disabled면 캐시 미생성(하지만 Area 자체는 유지)
            // ------------------------------------------------------------
            if (area.isEnabled == false)
            {
                logger.Info($"[AREA][CACHE][ONE][DISABLED] cacheReady=false. zoneId={area.zoneId}, mapId={area.mapId}, name={area.name}");
                return false;
            }

            // ------------------------------------------------------------
            // 3) polygon 유효성 검사 (invalid면 cacheReady=false 유지)
            // ------------------------------------------------------------
            if (area.polygon == null)
            {
                logger.Warn($"[AREA][CACHE][ONE][INVALID] polygon is null. cacheReady=false. zoneId={area.zoneId}, mapId={area.mapId}, name={area.name}");
                return false;
            }

            if (area.polygon.Count < 3)
            {
                logger.Warn($"[AREA][CACHE][ONE][INVALID] polygon points < 3. cacheReady=false. zoneId={area.zoneId}, mapId={area.mapId}, name={area.name}, count={area.polygon.Count}");
                return false;
            }

            // ------------------------------------------------------------
            // 4) 폐합점 제거(첫점=끝점) : 마지막 점이 첫 점과 같으면 제거
            // ------------------------------------------------------------
            Point2D first = area.polygon[0];
            Point2D last = area.polygon[area.polygon.Count - 1];

            if (IsSamePoint(first, last))
            {
                area.polygon.RemoveAt(area.polygon.Count - 1);

                if (area.polygon.Count < 3)
                {
                    logger.Warn($"[AREA][CACHE][ONE][INVALID] invalid after closing-point removal. cacheReady=false. zoneId={area.zoneId}, mapId={area.mapId}, name={area.name}");
                    return false;
                }
            }

            // ------------------------------------------------------------
            // 5) xs/ys 배열 캐시 생성 (판정 최적화)
            // ------------------------------------------------------------
            area.xs = area.polygon.Select(p => p.X).ToArray();
            area.ys = area.polygon.Select(p => p.Y).ToArray();

            // ------------------------------------------------------------
            // 6) BoundingBox 캐시 생성 (빠른 1차 컷)
            // ------------------------------------------------------------
            area.minX = area.xs.Min();
            area.maxX = area.xs.Max();
            area.minY = area.ys.Min();
            area.maxY = area.ys.Max();

            // ------------------------------------------------------------
            // 7) 캐시 준비 완료
            // ------------------------------------------------------------
            area.cacheReady = true;

            //EventLogger.Info(
            //    $"[AREA][CACHE][ONE][OK] cacheReady=true. zoneId={area.zoneId}, mapId={area.mapId}, name={area.name}" +
            //    $",points={area.polygon.Count}, bbox=({area.minX:0.###},{area.minY:0.###})~({area.maxX:0.###},{area.maxY:0.###})"
            //);

            return true;
        }

        /// <summary>
        /// 두 점이 같은 점인지 판단 (폐합점 제거용)
        /// </summary>
        private static bool IsSamePoint(Point2D a, Point2D b)
        {
            if (a == null) return false;
            if (b == null) return false;

            double tol = 0.000001;

            double dx = a.X - b.Y;
            if (dx < 0) dx = -dx;

            double dy = a.X - b.Y;
            if (dy < 0) dy = -dy;

            if (dx <= tol && dy <= tol)
                return true;

            return false;
        }

        // ------------------------------------------------------------
        // 폴리곤 Inside 판정(캐시 사용)
        // - Zone.cacheReady=true 인 경우에만 의미 있음
        // - AABB(바운딩박스) 1차 컷 + Edge epsilon + RayCasting(PIP)
        // ------------------------------------------------------------
        public bool IsInsideZone(double x, double y, ACSZone Zone)
        {
            if (Zone == null) return false;

            // enabled 아니면 트래픽 대상이 아니므로 false
            if (Zone.isEnabled == false) return false;

            // 캐시 준비가 안됐으면 판정 불가
            if (Zone.cacheReady == false) return false;

            // 배열 방어
            if (Zone.xs == null) return false;
            if (Zone.ys == null) return false;
            if (Zone.xs.Length < 3) return false;
            if (Zone.ys.Length < 3) return false;
            if (Zone.xs.Length != Zone.ys.Length) return false;

            double eps = Zone.epsilonMeters;

            // [1] AABB 1차 컷(빠름)
            if (x < Zone.minX - eps) return false;
            if (x > Zone.maxX + eps) return false;
            if (y < Zone.minY - eps) return false;
            if (y > Zone.maxY + eps) return false;

            // [2] 경계선 근처면 inside 처리(튐 방지)
            if (IsOnEdge(Zone.xs, Zone.ys, x, y, eps))
                return true;

            // [3] PIP - Ray Casting
            return RayCasting(Zone.xs, Zone.ys, x, y);
        }

        /// <summary>
        /// Ray Casting(짝/홀 규칙)으로 점 (x,y)가 다각형 내부인지 판정한다.
        /// - true  : 내부
        /// - false : 외부
        /// 주의: 경계(변 위)인 경우는 별도 처리(IsOnEdge 등)로 먼저 걸러주는게 안전함.
        /// </summary>
        private bool RayCasting(double[] xs, double[] ys, double x, double y)
        {
            // 내부 여부 (교차할 때마다 토글)
            bool inside = false;

            // 꼭짓점 개수
            int n = xs.Length;

            // i: 현재 꼭짓점, j: 이전 꼭짓점 (처음엔 마지막 점이 이전 점)
            // (j -> i) 가 하나의 변(선분)
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                // 현재 점(i)와 이전 점(j)의 좌표
                double xi = xs[i];
                double yi = ys[i];
                double xj = xs[j];
                double yj = ys[j];

                // ------------------------------------------------------------
                // 1) y 기준으로, 변 (j->i)가 수평선 y와 "교차 가능한지" 체크
                //
                // ((yi > y) != (yj > y)) :
                //  - 한 점은 y보다 위, 다른 점은 y보다 아래(또는 그 반대)면 true
                //  - 둘 다 위거나 둘 다 아래면 false (교차 없음)
                // ------------------------------------------------------------
                bool cross = ((yi > y) != (yj > y));

                if (cross)
                {
                    // --------------------------------------------------------
                    // 2) 교차한다면, 수평선 y에서 변 (j->i)와 만나는 교차점의 x좌표(xOnEdge) 계산
                    //
                    // 선분 방정식을 y 기준으로 보간(interpolation)한 형태:
                    // xOnEdge = xi + (xj - xi) * (y - yi) / (yj - yi)
                    //
                    // cross가 true면 (yj - yi)가 0이 되는 완전 수평선은 제외되므로
                    // 여기서 0으로 나눌 가능성이 낮아짐(실제로는 안전해짐).
                    // --------------------------------------------------------
                    double xOnEdge = (xj - xi) * (y - yi) / (yj - yi) + xi;

                    // --------------------------------------------------------
                    // 3) 점(x,y)에서 오른쪽으로 쏜 반직선이 교차점을 "지나는지" 확인
                    //    즉, 점의 x가 교차점 x보다 왼쪽이면 교차 1번으로 카운트
                    // --------------------------------------------------------
                    if (x < xOnEdge)
                        inside = inside == false;
                }
            }

            return inside;
        }

        /// <summary>
        /// 다각형의 경계(Edge) 위(또는 아주 근접)에 점 (x, y)가 있는지 검사한다.
        /// - xs, ys: 다각형 꼭짓점 좌표 배열 (같은 길이여야 함)
        /// - 다각형의 각 변( i -> i+1 )에 대해 점과 선분 사이 최단거리 d를 구하고,
        ///   d <= epsilon 이면 "경계 위"로 판단한다.
        /// </summary>
        private bool IsOnEdge(double[] xs, double[] ys, double x, double y, double epsilon)
        {
            // 허용오차가 0 이하이면 "근접"이라는 개념이 없으므로 false 처리
            if (epsilon <= 0) return false;

            // 꼭짓점 개수 (변의 개수도 동일)
            int n = xs.Length;

            // 모든 변(선분)을 순회: (i) -> (j)
            for (int i = 0; i < n; i++)
            {
                // 다음 꼭짓점 인덱스
                int j = i + 1;

                // 마지막 꼭짓점이면 다음은 0으로 돌아가서
                // 마지막 점 -> 첫 점으로 이어지는 "닫힌" 변을 만든다.
                if (j >= n) j = 0;

                // 점 (x,y) 와 선분 (xs[i],ys[i]) ~ (xs[j],ys[j]) 사이 최단거리 계산
                // (앞에서 만든 DistancePointToSegment 함수 사용)
                double d = DistancePointToSegment(x, y, xs[i], ys[i], xs[j], ys[j]);

                // 최단거리가 epsilon 이하면,
                // "점이 이 변 위(또는 매우 근접)"이라고 판단 → 즉시 true 리턴
                if (d <= epsilon) return true;
            }
            // 어떤 변에도 가깝지 않으면 경계 위가 아님
            return false;
        }

        /// <summary>
        /// 점 P(px, py)에서 선분 AB(ax, ay) ~ (bx, by)까지의 최단거리(유클리드 거리)를 계산한다.
        /// - 선분: A와 B를 잇는 "구간" (무한 직선이 아니라 A~B 사이만)
        /// - 핵심: P를 AB에 정사영(projection)한 점 C를 구하고, C가 선분 밖이면 A/B로 클램프하여 거리 계산
        /// </summary>

        private double DistancePointToSegment(double px, double py, double ax, double ay, double bx, double by)
        {
            // ------------------------------------------------------------
            // 1) 벡터 AB = B - A
            // ------------------------------------------------------------
            double abx = bx - ax;
            double aby = by - ay;

            // ------------------------------------------------------------
            // 2) 벡터 AP = P - A  (A 기준으로 P가 어디 있는지)
            // ------------------------------------------------------------

            double apx = px - ax;
            double apy = py - ay;

            // ------------------------------------------------------------
            // 3) |AB|^2 (AB 길이의 제곱)
            //    - 제곱을 쓰는 이유: 루트(Sqrt) 없이 연산 가능해서 빠르고 안정적
            // ------------------------------------------------------------
            double abLen2 = abx * abx + aby * aby;

            // ------------------------------------------------------------
            // 4) 예외 케이스: A와 B가 같은 점이면 "선분"이 아니라 "점"이 됨
            //    → 그럴 땐 P와 A 사이 거리로 처리
            // ------------------------------------------------------------
            if (abLen2 <= 0.0)
            {
                double dx0 = px - ax;
                double dy0 = py - ay;
                return Math.Sqrt(dx0 * dx0 + dy0 * dy0);
            }
            // ------------------------------------------------------------
            // 5) 정사영 비율 t 계산
            //    t = (AP · AB) / |AB|^2
            //    - t가 0이면 투영점이 A
            //    - t가 1이면 투영점이 B
            //    - 0~1 사이면 선분 내부의 어떤 점
            // ------------------------------------------------------------
            double t = (apx * abx + apy * aby) / abLen2;

            // ------------------------------------------------------------
            // 6) 선분이므로 t를 0~1로 "클램프"
            //    - t < 0  : 투영점이 A보다 앞쪽(선분 밖) → A로 고정
            //    - t > 1  : 투영점이 B보다 뒤쪽(선분 밖) → B로 고정
            // ------------------------------------------------------------
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            // ------------------------------------------------------------
            // 7) 가장 가까운 점 C = A + t * AB
            //    (투영점이 선분 밖이면 이미 A/B로 클램프 되어 있음)
            // ------------------------------------------------------------
            double cx = ax + t * abx;
            double cy = ay + t * aby;

            // ------------------------------------------------------------
            // 8) 거리 = |P - C|
            // ------------------------------------------------------------
            double dx = px - cx;
            double dy = py - cy;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}