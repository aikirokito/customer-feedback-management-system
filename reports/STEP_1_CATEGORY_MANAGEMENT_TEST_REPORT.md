# Báo cáo hoàn thành và kiểm thử Bước 1

**Hạng mục:** Quản lý danh mục phản hồi bằng database  
**Ngày kiểm tra:** 30/06/2026  
**Nhánh:** `codex/fix-cfms-srs-audit`

## 1. Phạm vi Bước 1

Bước 1 hoàn thiện luồng quản lý danh mục phản hồi theo SRS:

- System Admin có thể xem toàn bộ danh mục.
- System Admin có thể tạo, cập nhật, kích hoạt và vô hiệu hóa danh mục.
- Tên danh mục phải duy nhất.
- Danh mục bị vô hiệu hóa không xuất hiện trong form gửi phản hồi mới.
- Backend từ chối tạo feedback với danh mục đã bị vô hiệu hóa.
- Feedback đã tồn tại vẫn giữ nguyên khóa ngoại và thông tin danh mục sau khi danh mục bị vô hiệu hóa.
- Thay đổi danh mục được ghi vào audit log.
- Category có thể được gán hoặc gỡ khỏi phòng ban xử lý.

## 2. Đối chiếu yêu cầu SRS

| Yêu cầu | Nội dung | Trạng thái | Bằng chứng |
|---|---|---:|---|
| FR-CAT-01 | Admin tạo, cập nhật và disable category | Đạt | Category service, admin API và trang quản trị category |
| FR-CAT-02 | Feedback cũ giữ category khi category bị disable | Đạt | FK `CategoryId` với `DeleteBehavior.Restrict`, migration backfill và test EF InMemory |
| FR-FB-01 | Feedback mới sử dụng category hợp lệ | Đạt | `CreateFeedbackRequest.CategoryId` và kiểm tra `IsActive` trong backend |
| FR-AUDIT-01 | Thay đổi category phải được audit | Đạt | Create/Update gọi `IAuditLogService.LogAsync` |
| NFR-SEC-02 | Quyền được kiểm tra tại backend | Đạt | Admin endpoints yêu cầu role `SystemAdmin` |
| NFR-MAIN-03 | Business rule nằm ở backend | Đạt | Kiểm tra unique name, active category và active department trong service/validator |

## 3. Thay đổi hoàn thiện trong lượt này

### Backend

- Bổ sung khả năng gỡ phòng ban khỏi category bằng `ClearDepartment`.
- Validator từ chối request update rỗng.
- Validator từ chối gửi đồng thời `DepartmentId` và `ClearDepartment`.
- Khi tạo hoặc đổi phòng ban, service nạp và gắn navigation `Department`; DTO trả đúng `DepartmentName`.
- Từ chối gán category vào phòng ban đã bị vô hiệu hóa.

### Frontend

- Form quản trị gửi `clearDepartment` khi admin chọn “Chưa gán phòng ban”.
- Cho phép xóa nội dung mô tả category khi cập nhật.
- Trang gửi feedback tiếp tục lấy category hoạt động từ `/api/Categories`, không dùng danh sách enum tĩnh.

### Database và migration

- `AddFeedbackCategories` tạo bảng category.
- `CompleteMvpDataModel` tạo dữ liệu category mặc định, backfill `CategoryId` cho feedback cũ rồi mới xóa cột enum cũ.
- Quan hệ feedback-category dùng `DeleteBehavior.Restrict`, bảo vệ tham chiếu lịch sử.
- Idempotent migration script chứa đủ bước tạo bảng, seed, backfill và chuyển đổi schema.

## 4. Ma trận kiểm thử chức năng Bước 1

| STT | Test | Kết quả |
|---:|---|---:|
| 1 | Tạo category hợp lệ, trim dữ liệu, mặc định active và ghi audit log | Pass |
| 2 | Tạo category trùng tên bị từ chối, không ghi database | Pass |
| 3 | Gán category vào phòng ban disabled bị từ chối | Pass |
| 4 | Disable category không xóa hoặc thay đổi tham chiếu feedback cũ | Pass |
| 5 | Admin có thể gỡ phòng ban khỏi category | Pass |
| 6 | Update request rỗng hoặc mâu thuẫn bị validator từ chối | Pass |
| 7 | Submit feedback bằng category disabled bị backend từ chối | Pass |
| 8 | Submit feedback bằng category active lưu đúng category, department, status NEW và priority MEDIUM | Pass |
| 9 | Repository chỉ trả category active và sắp xếp theo tên | Pass |
| 10 | EF InMemory xác nhận feedback vẫn đọc được category sau khi category bị disable | Pass |

## 5. Kết quả các cổng kiểm tra

| Cổng kiểm tra | Lệnh | Kết quả |
|---|---|---:|
| Category-focused tests | `dotnet test ... --filter "FullyQualifiedName~CFMS.Tests.Category"` | **10/10 pass** |
| Backend unit/integration tests | `dotnet test CFMS.sln --no-restore` | **41/41 pass** |
| Backend compile | `dotnet build CFMS.sln --no-restore` | **Pass, 0 warning, 0 error** |
| Frontend lint | `npm.cmd run lint` | **Pass** |
| Frontend production build | `npm.cmd run build` | **Pass, 104 modules** |
| Migration generation check | `dotnet ef migrations script --idempotent ...` | **Pass** |
| Whitespace/error check | `git diff --check` | **Pass**; chỉ có cảnh báo LF/CRLF của Git trên Windows |

## 6. Hạn chế kiểm thử

Chưa chạy migration hoặc API E2E trên PostgreSQL/Supabase thật vì:

- `ConnectionStrings:DefaultConnection` vẫn chứa `YOUR_SUPABASE_HOST`.
- Không có biến môi trường `ConnectionStrings__DefaultConnection`.
- JWT và Supabase Storage trong `appsettings.json` cũng đang là placeholder.

Đây là giới hạn cấu hình môi trường, không phải lỗi compile hoặc unit/integration test.

## 7. Bước triển khai trên database thật

Sau khi cấu hình connection string hợp lệ:

```powershell
cd BE
dotnet ef database update --project src/CFMS.Infrastructure --startup-project src/CFMS.Api
```

Sau đó cần smoke-test bằng tài khoản System Admin:

1. Tạo category mới.
2. Sửa tên/mô tả/phòng ban.
3. Disable category.
4. Xác nhận category không còn trong form gửi feedback.
5. Mở feedback cũ và xác nhận category vẫn hiển thị.
6. Kiểm tra audit log của các thao tác trên.

## 8. Kết luận

Bước 1 đã hoàn thành ở mức code, migration, UI wiring và automated test. Tất cả cổng kiểm tra cục bộ đều đạt. Phần còn lại để xác nhận trên môi trường triển khai là cung cấp cấu hình PostgreSQL/Supabase thật, áp migration và chạy smoke-test API/UI với dữ liệu thật.
