# Football Manager

Game quản lý bóng đá theo lượt (turn-based), viết bằng .NET 8. Người chơi điều hành 1 CLB, thời gian tiến theo tuần (`GameState`), mỗi tuần các trận đấu trong lịch (`ScheduleTemplate`) được tự động mô phỏng.

## Kiến trúc

Solution gồm 4 project, chia theo layer:

```
FootballManager.Data          Entities, EF Core DbContext, Migrations, Repositories
FootballManager.Business      Services (nghiệp vụ), DTOs, DependencyInjection
FootballManagerAPI            ASP.NET Core Web API (FootballManagerAPI.sln)
FootballManagerMVC            ASP.NET Core MVC, gọi API qua ApiClient (HttpClient)
```

Luồng phụ thuộc: `API/MVC → Business → Data`. `MVC` không truy cập DB trực tiếp, chỉ gọi `API` qua HTTP.

### Repository pattern

Tất cả repository đều kế thừa `BaseRepository<T> : IBaseRepository<T>`. Thao tác ghi (`AddAsync`/`Update`/`Delete`) chỉ đánh dấu thay đổi trên `DbContext`; việc lưu được thực hiện qua `IUnitOfWork.SaveChangesAsync()` ở tầng Service (cho các luồng đơn giản), hoặc repository tự gọi `SaveChangesAsync()` khi thao tác phức tạp/nhiều bước cần gói gọn trong 1 lần lưu (`MatchRepository.SimulateMatchAsync`, `TransferRepository`).

### Luồng chính

- `POST api/game/next-week` (`GameStateService.AdvanceNextWeekAsync`): mô phỏng các trận trong vòng đấu hiện tại, thiết lập đội hình BOT ở tuần 3 (`BotService`), tổng kết mùa giải + chia thưởng khi tới tuần cuối, tiến sang tuần/mùa kế tiếp.
- `GET api/dashboard`: tổng hợp trạng thái game, CLB của người chơi, bảng xếp hạng, trận kế tiếp, kết quả gần nhất.
- `api/transfer/*`: thị trường chuyển nhượng — chiêu mộ cầu thủ tự do, gia hạn hợp đồng, gửi đề nghị chuyển nhượng, BOT tự quyết định mua/bán.

## Chạy dự án

```bash
# API
cd FootballManagerAPI
dotnet run

# MVC (đọc BaseAddress trong Program.cs, mặc định trỏ https://localhost:7088/api/)
cd FootballManagerMVC
dotnet run
```

Migrate database (sau khi review migration):

```bash
dotnet ef database update --project FootballManager.Data --startup-project FootballManagerAPI
```
