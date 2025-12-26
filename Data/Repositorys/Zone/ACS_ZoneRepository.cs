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
        /// <summary>
        /// [단건] ACSZone의 폴리곤 캐시(xs/ys, bbox)를 채운다.
        ///
        /// 처리 내용:
        /// 1) cache 초기화
        /// 2) enabled=false면 캐시 생성 안 함(cacheReady=false 유지)
        /// 3) polygon 유효성 검사(최소 3점)
        /// 4) 폐합점(첫점==끝점)이면 마지막 점 제거
        /// 5) xs/ys 배열 생성
        /// 6) bounding box(minX/maxX/minY/maxY) 계산
        /// 7) cacheReady=true 설정
        /// </summary>
        public bool FillAreaCacheOne(ACSZone zone)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------
            if (zone == null)
            {
                logger.Warn("[ZONE][CACHE][ONE][SKIP] zone is null");
                return false;
            }

            // ------------------------------------------------------------
            // 1) 캐시 기본값 초기화 (재호출/리로드 대비)
            // ------------------------------------------------------------
            zone.cacheReady = false;
            zone.xs = new double[0];
            zone.ys = new double[0];
            zone.minX = 0;
            zone.maxX = 0;
            zone.minY = 0;
            zone.maxY = 0;

            // ------------------------------------------------------------
            // 2) 비활성 Zone이면 캐시 생성하지 않음 (Zone 자체는 유지)
            // ------------------------------------------------------------
            if (zone.isEnabled == false)
            {
                logger.Info($"[ZONE][CACHE][ONE][DISABLED] cacheReady=false. zoneId={zone.zoneId}, mapId={zone.mapId}, name={zone.name}");
                return false;
            }

            // ------------------------------------------------------------
            // 3) polygon 유효성 검사
            // ------------------------------------------------------------
            if (zone.polygon == null)
            {
                logger.Warn($"[ZONE][CACHE][ONE][INVALID] polygon is null. cacheReady=false. zoneId={zone.zoneId}, mapId={zone.mapId}, name={zone.name}");
                return false;
            }

            if (zone.polygon.Count < 3)
            {
                logger.Warn($"[ZONE][CACHE][ONE][INVALID] polygon points < 3. cacheReady=false. zoneId={zone.zoneId}, mapId={zone.mapId}, name={zone.name}, count={zone.polygon.Count}");
                return false;
            }

            // ------------------------------------------------------------
            // 4) 폐합점 제거 (첫점 == 끝점이면 마지막 점 제거)
            // - 일부 시스템은 닫힌 폴리곤 표현을 위해 마지막 점을 첫 점과 동일하게 넣음
            // - RayCasting / Edge 체크는 (첫점=끝점) 중복이 있으면 애매해질 수 있으니 제거 권장
            // ------------------------------------------------------------
            Point2D first = zone.polygon[0];
            Point2D last = zone.polygon[zone.polygon.Count - 1];

            bool removedClosingPoint = false;

            if (IsSamePoint(first, last))
            {
                zone.polygon.RemoveAt(zone.polygon.Count - 1);
                removedClosingPoint = true;

                // 제거 후에도 최소 3점 필요
                if (zone.polygon.Count < 3)
                {
                    logger.Warn($"[ZONE][CACHE][ONE][INVALID] invalid after closing-point removal. cacheReady=false. zoneId={zone.zoneId}, mapId={zone.mapId}, name={zone.name}");
                    return false;
                }
            }

            // ------------------------------------------------------------
            // 5) xs/ys 배열 생성 + 좌표 유효성 방어(NaN/Infinity)
            // ------------------------------------------------------------
            // (중요) Min/Max는 NaN이 섞이면 결과가 NaN으로 망가질 수 있음
            // -> 캐시 생성 단계에서 걸러주는게 안전
            for (int i = 0; i < zone.polygon.Count; i++)
            {
                double px = zone.polygon[i].X;
                double py = zone.polygon[i].Y;

                if (double.IsNaN(px) || double.IsInfinity(px) || double.IsNaN(py) || double.IsInfinity(py))
                {
                    logger.Warn($"[ZONE][CACHE][ONE][INVALID] polygon has NaN/Infinity. zoneId={zone.zoneId}, mapId={zone.mapId}, name={zone.name}, index={i}, x={px}, y={py}");
                    return false;
                }
            }

            zone.xs = zone.polygon.Select(p => p.X).ToArray();
            zone.ys = zone.polygon.Select(p => p.Y).ToArray();

            // ------------------------------------------------------------
            // 6) BoundingBox 생성 (빠른 1차 컷)
            // ------------------------------------------------------------
            zone.minX = zone.xs.Min();
            zone.maxX = zone.xs.Max();
            zone.minY = zone.ys.Min();
            zone.maxY = zone.ys.Max();

            // ------------------------------------------------------------
            // 7) 캐시 준비 완료
            // ------------------------------------------------------------
            zone.cacheReady = true;

            // 필요한 경우 캐시 생성 로그
            //logger.Info(
            //    $"[ZONE][CACHE][ONE][OK] zoneId={zone.zoneId}, mapId={zone.mapId}, name={zone.name}, points={zone.polygon.Count}" +
            //    $",removedClosingPoint={removedClosingPoint}, bbox=({zone.minX:0.###},{zone.minY:0.###})~({zone.maxX:0.###},{zone.maxY:0.###})"
            //);

            return true;
        }

        /// <summary>
        /// 두 점이 같은 점인지 판단(폐합점 제거용)
        /// - tol(허용오차) 이내면 동일점으로 판단
        /// </summary>
        private static bool IsSamePoint(Point2D a, Point2D b)
        {
            if (a == null) return false;
            if (b == null) return false;

            // 좌표계가 meter라면 1e-6m = 0.000001m 는 사실상 동일점
            double tol = 0.000001;

            // 버그 수정: X는 X끼리, Y는 Y끼리 비교해야 한다.
            double dx = a.X - b.X;
            if (dx < 0) dx = -dx;

            double dy = a.Y - b.Y;
            if (dy < 0) dy = -dy;

            if (dx <= tol && dy <= tol)
                return true;

            return false;
        }

        /// <summary>
        /// 폴리곤 Inside 판정(캐시 사용)
        /// - cacheReady=true 인 경우에만 의미 있음
        /// - AABB(바운딩박스) 1차 컷 + Edge epsilon + RayCasting(PIP)
        /// </summary>
        public bool IsInsideZone(double x, double y, ACSZone zone)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------
            if (zone == null) return false;
            if (zone.isEnabled == false) return false;
            if (zone.cacheReady == false) return false;

            // 좌표 유효성 방어
            if (double.IsNaN(x) || double.IsInfinity(x)) return false;
            if (double.IsNaN(y) || double.IsInfinity(y)) return false;

            // 배열 방어
            if (zone.xs == null) return false;
            if (zone.ys == null) return false;
            if (zone.xs.Length < 3) return false;
            if (zone.ys.Length < 3) return false;
            if (zone.xs.Length != zone.ys.Length) return false;

            // ------------------------------------------------------------
            // 1) AABB 1차 컷(빠른 필터)
            // - epsilonMeters를 포함해 바운딩박스를 약간 확장해서 튐을 줄임
            // ------------------------------------------------------------
            double eps = zone.epsilonMeters;

            if (x < zone.minX - eps) return false;
            if (x > zone.maxX + eps) return false;
            if (y < zone.minY - eps) return false;
            if (y > zone.maxY + eps) return false;

            // ------------------------------------------------------------
            // 2) 경계선 근처면 inside로 인정(센서 노이즈 튐 방지)
            // ------------------------------------------------------------
            if (IsOnEdge(zone.xs, zone.ys, x, y, eps))
                return true;

            // ------------------------------------------------------------
            // 3) Ray Casting(PIP)으로 내부 판정
            // ------------------------------------------------------------
            return RayCasting(zone.xs, zone.ys, x, y);
        }

        /// <summary>
        /// Ray Casting(짝/홀 규칙)으로 점 (x,y)가 다각형 내부인지 판정한다.
        /// - true  : 내부
        /// - false : 외부
        /// 주의: 경계(변 위)는 IsOnEdge로 먼저 처리하는 것이 안전
        /// </summary>
        private bool RayCasting(double[] xs, double[] ys, double x, double y)
        {
            bool inside = false;
            int n = xs.Length;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = xs[i];
                double yi = ys[i];
                double xj = xs[j];
                double yj = ys[j];

                // y 기준으로 한 점은 위, 다른 점은 아래면 교차 가능
                bool cross = ((yi > y) != (yj > y));
                if (cross)
                {
                    // 교차점 x좌표 계산
                    double xOnEdge = (xj - xi) * (y - yi) / (yj - yi) + xi;

                    // 점에서 오른쪽으로 레이를 쏴서 교차하면 inside 토글
                    if (x < xOnEdge)
                        inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// 다각형의 경계(Edge) 위(또는 epsilon 이내 근접)에 점 (x, y)가 있는지 검사한다.
        /// - 모든 변에 대해 점~선분 최단거리 <= epsilon 이면 true
        /// </summary>
        private bool IsOnEdge(double[] xs, double[] ys, double x, double y, double epsilon)
        {
            if (epsilon <= 0) return false;

            int n = xs.Length;

            for (int i = 0; i < n; i++)
            {
                int j = i + 1;
                if (j >= n) j = 0;

                double d = DistancePointToSegment(x, y, xs[i], ys[i], xs[j], ys[j]);
                if (d <= epsilon) return true;
            }

            return false;
        }

        /// <summary>
        /// 점 P(px, py)에서 선분 AB(ax, ay) ~ (bx, by)까지의 최단거리(유클리드 거리)를 계산한다.
        /// - P를 AB에 정사영한 점 C를 구하고, C가 선분 밖이면 A 또는 B로 clamping 후 거리 계산
        /// </summary>
        private double DistancePointToSegment(double px, double py, double ax, double ay, double bx, double by)
        {
            double abx = bx - ax;
            double aby = by - ay;

            double apx = px - ax;
            double apy = py - ay;

            double abLen2 = abx * abx + aby * aby;

            // A==B면 선분이 아니라 점
            if (abLen2 <= 0.0)
            {
                double dx0 = px - ax;
                double dy0 = py - ay;
                return Math.Sqrt(dx0 * dx0 + dy0 * dy0);
            }

            // t = (AP·AB)/|AB|^2
            double t = (apx * abx + apy * aby) / abLen2;

            // 선분이므로 0~1로 클램프
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            // C = A + t*AB
            double cx = ax + t * abx;
            double cy = ay + t * aby;

            double dx = px - cx;
            double dy = py - cy;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Exit(OUT) 확정 판정
        /// - inside가 false로 나왔더라도(레이캐스팅 결과), 경계 근처 튐일 수 있다.
        /// - hysteresisMeters 만큼 "확실히 멀어졌을 때"만 Exit로 인정한다.
        ///
        /// 사용 시나리오:
        /// 1) inside = IsInsideZone(x,y,zone) 가 false일 때
        /// 2) 이미 한번 IN 했던 상태(enteredOnce=true)일 때
        /// 3) IsExitConfirmed(...) == true 이면 Remove/Exit 처리
        /// </summary>
        public bool IsExitConfirmed(double x, double y, ACSZone zone)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------
            if (zone == null) return false;
            if (zone.isEnabled == false) return false;
            if (zone.cacheReady == false) return false;

            // 좌표 유효성 방어
            if (double.IsNaN(x) || double.IsInfinity(x)) return false;
            if (double.IsNaN(y) || double.IsInfinity(y)) return false;

            // 배열 방어
            if (zone.xs == null) return false;
            if (zone.ys == null) return false;
            if (zone.xs.Length < 3) return false;
            if (zone.ys.Length < 3) return false;
            if (zone.xs.Length != zone.ys.Length) return false;

            // ------------------------------------------------------------
            // 1) hysteresis 값
            // - 0 이하이면 "outside가 되는 순간 Exit 확정"으로 취급
            // ------------------------------------------------------------
            double h = zone.hysteresisMeters;
            if (h <= 0) return true;

            // ------------------------------------------------------------
            // 2) 확장 BoundingBox 밖이면 "확실히 멀어짐" → Exit 확정
            // - zone bbox를 h 만큼 확장한 박스 밖이면, 경계에서 최소 h 이상 멀어진 것
            // ------------------------------------------------------------
            if (x < zone.minX - h) return true;
            if (x > zone.maxX + h) return true;
            if (y < zone.minY - h) return true;
            if (y > zone.maxY + h) return true;

            // ------------------------------------------------------------
            // 3) 확장 BoundingBox 안이면, "경계선까지 최단거리"로 Exit 확정 여부 판단
            // - 현재 점이 폴리곤 경계선에서 h 이상 떨어졌을 때만 Exit 확정
            // ------------------------------------------------------------
            double d = DistanceToEdges(zone.xs, zone.ys, x, y);

            if (d >= h)
                return true;

            // 아직 경계 근처(튐 가능) → Exit 확정 아님
            return false;
        }

        /// <summary>
        /// 점(x,y)에서 폴리곤 "모든 변"까지의 최단거리(min)를 구한다.
        /// - Exit hysteresis 판단용
        /// </summary>
        private double DistanceToEdges(double[] xs, double[] ys, double x, double y)
        {
            int n = xs.Length;

            // Any 금지 → 첫 변으로 초기값 설정
            int j0 = 1;
            if (j0 >= n) j0 = 0;

            double min = DistancePointToSegment(x, y, xs[0], ys[0], xs[j0], ys[j0]);

            // 나머지 변들에 대해 최소값 갱신
            for (int i = 1; i < n; i++)
            {
                int j = i + 1;
                if (j >= n) j = 0;

                double d = DistancePointToSegment(x, y, xs[i], ys[i], xs[j], ys[j]);
                if (d < min) min = d;
            }

            return min;
        }

    }
}