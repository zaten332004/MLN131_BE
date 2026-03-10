# MLN131_BE (Backend)

Backend mẫu cho website MLN131:

- Đăng ký / đăng nhập (JWT)
- Phân quyền theo role: `admin`, `user`, `viewer`
- Admin xem thống kê truy cập realtime (REST + SignalR)
- Người dùng đăng nhập mới xem nội dung (chưa đăng nhập chỉ xem homepage)
- Khung chat AI (Gemini) được “ground” bằng FAQs nội bộ (`src/MLN131.Api/Resources/faqs_vi.txt`)

## Yêu cầu

- **SQL Server** (Local SQL Server hoặc SQL Server Express)
- **.NET SDK 8** (project target `net8.0`)

## Tạo database `MLN131`

Chạy script:

- `scripts/sqlserver-create-MLN131.sql`

Sau đó chỉnh connection string trong `src/MLN131.Api/appsettings.json` (mặc định dùng `Server=localhost;Database=MLN131;Trusted_Connection=True;...`).

## Cấu hình bắt buộc

Mở `src/MLN131.Api/appsettings.json`:

- **`Jwt:SigningKey`**: đổi thành một secret dài, ngẫu nhiên
- **`Gemini:ApiKey`**: điền API key Gemini nếu dùng chat AI (không nên commit lên git; nên set qua User Secrets hoặc environment variable `Gemini__ApiKey`)
- **`SeedAdmin:Email` / `SeedAdmin:Password`**: (tuỳ chọn) để tự tạo admin khi chạy lần đầu

## Chạy server

```bash
dotnet run --project src/MLN131.Api
```

Khi chạy, server sẽ tự:

- `Migrate` DB (tạo tables) **nếu database `MLN131` đã tồn tại**
- Seed 3 role: `admin`, `user`, `viewer`
- Seed 1 trang nội dung `chuong-5` từ file FAQs

Swagger: mở `/swagger`

## API chính (tóm tắt)

### Auth

- `POST /api/auth/register` (mặc định gán role `user`)
- `POST /api/auth/login`
- `GET /api/auth/me`

### Homepage (public)

- `GET /api/public/home`

### Content (cần đăng nhập)

- `GET /api/content/pages`
- `GET /api/content/pages/{slug}` (seed sẵn `slug=chuong-5`)

### Profile (cần đăng nhập)

- `PUT /api/profile`
- `POST /api/profile/avatar` (multipart/form-data, field `file`)

### Trả lời câu hỏi (để thống kê)

- `POST /api/responses`

### Tracking page view (frontend gọi khi user vào trang)

- `POST /api/track/pageview`

### Admin

- `GET /api/admin/stats/realtime`
- `GET /api/admin/users?q=...`
- `PATCH /api/admin/users/{id}`
- `POST /api/admin/users/{id}/disabled`
- `POST /api/admin/users/{id}/role`

### Realtime stats (SignalR)

- Hub: `/hubs/stats` (chỉ `admin`)
- Server broadcast event: `realtimeStats`
- Auth cho SignalR: truyền JWT qua query `access_token=...`

### Chat Gemini (role `user` hoặc `admin`)

- `POST /api/chat`

