# Hướng dẫn Build & Đóng gói PackWatch App

Thư mục `deploy/` chứa các công cụ và cấu hình dùng để build, đóng gói và tạo file cài đặt cho phần mềm ghi hình đóng gói **PackWatch**.

```
deploy/
├── build.ps1                       # Publish dự án .NET WPF sang thư mục 'deploy/dist'
├── deploy.ps1                      # Build + tự động gọi Inno Setup Compiler để tạo bộ cài
├── ScriptSetupApp_UsingInno.iss    # Cấu hình Inno Setup để tạo file cài đặt .exe
├── GUIDE.md                        # Tài liệu hướng dẫn này
└── Output/                         # Nơi chứa file cài đặt đầu ra (PackWatch_Setup.exe)
```

---

## 1. Yêu cầu môi trường Build

Để tiến hành build và tạo file cài đặt, máy tính cần đáp ứng:
1. **.NET 8 SDK** trở lên.
2. **PowerShell 5.1** hoặc cao hơn.
3. **Inno Setup 6** (Nếu bạn muốn tạo ra bộ cài đặt `.exe` tự động). Tải tại: [jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php).

---

## 2. Hướng dẫn sử dụng các Script

Mở PowerShell tại thư mục gốc của dự án hoặc thư mục `deploy/` và chạy một trong các lệnh sau:

### Lựa chọn A: Chỉ Build ra thư mục chạy trực tiếp (Không đóng gói bộ cài)
Chạy lệnh:
```powershell
.\deploy\build.ps1 -OutputDir "deploy/dist"
```
* **Kết quả**: Ứng dụng sẽ được biên dịch và đóng gói hoàn chỉnh tại `deploy/dist/`. Bạn có thể sao chép thư mục `dist` này sang bất kỳ máy tính chạy Windows nào để sử dụng ngay mà không cần cài đặt.

### Lựa chọn B: Build ứng dụng + Tự động đóng gói thành bộ cài đặt `.exe` (Khuyên dùng)
Chạy lệnh:
```powershell
.\deploy\deploy.ps1
```
* **Cách hoạt động**:
  1. Script sẽ tự động chạy `build.ps1` để biên dịch dự án WPF sang `deploy/dist`.
  2. Script tiếp tục tìm kiếm Trình biên dịch Inno Setup (`ISCC.exe`) trên máy tính của bạn.
  3. Nếu tìm thấy Inno Setup, script sẽ biên dịch file `.iss` và tạo ra bộ cài đặt hoàn chỉnh tại thư mục đầu ra `deploy\Output\PackWatch_Setup.exe`.
  4. Nếu máy tính chưa cài Inno Setup, script sẽ thông báo kết quả build tại `deploy/dist` để bạn chạy trực tiếp hoặc mở Inno Setup thủ công sau.

---

## 3. Cấu hình Inno Setup (`ScriptSetupApp_UsingInno.iss`)
File `.iss` đã được tối ưu hóa riêng cho **PackWatch**:
- Thiết lập biểu tượng ứng dụng bằng icon chính thức (`icon.ico`).
- Tích hợp trình kiểm tra thông minh: Tự động phát hiện xem máy tính của người dùng cuối đã cài đặt `.NET 8 Desktop Runtime` hay chưa.
  - Nếu chưa cài đặt, chương trình cài đặt sẽ hiện cảnh báo và dẫn link trực tiếp đến trang chủ Microsoft để tải về, đảm bảo người dùng không gặp lỗi khi mở app.
