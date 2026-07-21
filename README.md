# ⚽ Football Manager

Phần mềm quản lý bóng đá mô phỏng, viết bằng **.NET 8 / ASP.NET Core Web API + EF Core**. Người dùng điều hành 1 CLB xuyên suốt nhiều mùa giải; hệ thống tự mô phỏng toàn bộ phần còn lại — lịch thi đấu, kết quả trận đấu, đối thủ AI, thị trường chuyển nhượng, và vòng đời sự nghiệp cầu thủ qua từng mùa.

Dự án được xây theo hướng **backend-driven simulation**: gần như toàn bộ logic nghiệp vụ (sinh lịch thi đấu đa thể thức, mô phỏng trận đấu có trọng số, kinh tế chuyển nhượng, AI đối thủ) nằm ở tầng service, expose ra ngoài qua REST API — tách bạch hoàn toàn khỏi bất kỳ client cụ thể nào.

## Điểm nổi bật kỹ thuật

- **State machine mùa giải nhiều thể thức**: cùng lúc vận hành League (vòng tròn 2 lượt), Cup (knockout ghép seed), và giải châu lục kiểu C1 (vòng bảng → bán kết → chung kết) trên chung 1 lịch tuần (`ScheduleTemplate`), tự sinh lịch động cho vòng knockout dựa theo kết quả vòng trước.
- **Match simulator có trọng số thực tế**: sức mạnh tính từ đúng đội hình ra sân (không phải cả đội), cộng dồn lợi thế sân nhà, phong độ CLB, phân phối bàn thắng theo Poisson, chọn người ghi bàn ngẫu nhiên có trọng số theo vị trí, MVP trận đấu = cầu thủ ghi nhiều bàn nhất.
- **Nền kinh tế chuyển nhượng**: định giá cầu thủ theo hàm phi tuyến (Quality, dư địa phát triển, độ tuổi, thời hạn hợp đồng), AI đối thủ tự đánh giá đội hình đang thiếu, tự niêm yết cầu thủ dư thừa, tự thương lượng và chốt giao dịch — toàn bộ chạy tự động theo vòng lặp game, không cần can thiệp thủ công.
- **Vòng đời cầu thủ xuyên suốt nhiều mùa**: lão hoá theo tuổi, chuyển giai đoạn sự nghiệp (Youth → Rising → Peak → Stable → Veteran → giải nghệ), tăng/giảm chỉ số theo cơ chế đào tạo của từng CLB, tự đôn cầu thủ trẻ bù quân số mỗi mùa.
- **Kiến trúc phân lớp rõ ràng**, sẵn sàng mở rộng thêm giải đấu/quốc gia mà không cần sửa logic lõi (chỉ cần thêm dữ liệu).

## Các nhóm chức năng chính

### Season & Fixtures
Khởi tạo mùa giải, gán CLB vào giải theo quốc gia/thứ hạng mùa trước, tự sinh lịch thi đấu cho cả 3 thể thức, tổng kết mùa (vô địch, vua phá lưới, MVP, trao thưởng).
> `POST /api/game/next-week`, `GET /api/tournament/{id}` (BXH hoặc bracket tuỳ thể thức)

### Match Simulation
Mô phỏng trận đấu dựa trên đội hình thực tế, ghi nhận bàn thắng/MVP từng trận, tự cập nhật bảng xếp hạng.
> Chạy ngầm trong `next-week`, kết quả xem qua `GET /api/tournament/{id}`, `GET /api/myclub/last-result`

### Club & Player Management
Xem đội hình, trận tiếp theo, đội hình đối thủ (scouting), nộp đội hình ra sân; cuối mỗi mùa cầu thủ lão hoá/tăng-giảm chỉ số/giải nghệ, đôn cầu thủ trẻ mới bù quân số (tên sinh ngẫu nhiên theo quốc gia).
> `GET /api/myclub`, `GET /api/myclub/players`, `GET /api/myclub/next-match`, `GET /api/myclub/next-opponent-lineup`, `POST /api/myclub/lineup`

### Transfer Market
Thị trường chuyển nhượng cho cả người chơi lẫn AI: chiêu mộ cầu thủ tự do, gia hạn hợp đồng, gửi/nhận đề nghị chuyển nhượng, định giá tự động, AI tự niêm yết cầu thủ dư thừa và tự quyết định mua/bán mỗi đầu mùa.
> `GET /api/transfer/market`, `POST /api/transfer/free-agent-offer`, `POST /api/transfer/renew-contract`, `POST /api/transfer/transfer-offer`

## Kiến trúc

```
FootballManager.Data          Entities, EF Core DbContext, Migrations, Repositories
FootballManager.Business      Services (nghiệp vụ), DTOs, Helpers, DependencyInjection
FootballManagerAPI            ASP.NET Core Web API — cổng vào duy nhất tới nghiệp vụ
FootballManagerMVC            ASP.NET Core MVC (Dashboard), gọi API qua HttpClient
```

Luồng phụ thuộc: `API/MVC → Business → Data` (1 chiều, `Data` không biết gì về `Business`). `MVC` không truy cập DB trực tiếp — mọi thao tác đều qua `API`.

**Pattern đã áp dụng:**
- **Repository + Unit of Work**: mọi repository kế thừa `BaseRepository<T> : IBaseRepository<T>`; ghi dữ liệu qua `IUnitOfWork.SaveChangesAsync()` ở tầng Service cho luồng đơn giản, hoặc repository tự gói gọn nhiều bước ghi trong 1 lần lưu khi nghiệp vụ phức tạp (vd `MatchRepository.SimulateMatchAsync`).
- **Code-first migrations** với EF Core — toàn bộ thay đổi schema (15+ migration) đều có lịch sử, review được.
- **DTO tách biệt Entity** ở phần lớn API response (danh sách cầu thủ, BXH/bracket, kết quả trận...).

## Công nghệ sử dụng

.NET 8 · ASP.NET Core Web API · Entity Framework Core 8 (SQL Server) · ASP.NET Core MVC

## Chạy dự án

```bash
# API
cd FootballManagerAPI
dotnet run

# MVC (đọc BaseAddress trong Program.cs, mặc định trỏ https://localhost:7088/api/)
cd FootballManagerMVC
dotnet run
```

Migrate database (nên review migration trước khi update):

```bash
dotnet ef database update --project FootballManager.Data --startup-project FootballManagerAPI
```
