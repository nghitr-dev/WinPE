# WinPE Nghitr Dev 🚀

> **Hệ điều hành cứu hộ & bảo trì máy tính Windows độc lập**  
> Tích hợp giao diện quản trị đồ họa hiện đại (Dark Theme), bộ công cụ chẩn đoán phần cứng, sửa lỗi khởi động và sao lưu dữ liệu chuyên sâu.  
> Tác giả: **Nghitr Dev** | Phiên bản: **v1.0.0**

---

## 🌟 Giới thiệu

**WinPE Nghitr Dev** là môi trường Windows Preinstallation Environment (WinPE) độc lập được thiết kế tối ưu cho kỹ thuật viên và người dùng khi máy tính gặp sự cố không thể vào được Windows. Hệ thống hỗ trợ khởi động đa nền tảng (**Dual Boot: UEFI 64-bit & Legacy BIOS**), tương thích tốt với hầu hết dòng máy tính để bàn và laptop hiện nay.

Điểm đặc biệt của bản WinPE này là sở hữu **giao diện điều khiển đồ họa trực quan (WinPE-Tool)** viết bằng C# .NET Framework 4.8, giúp thao tác cứu hộ nhanh chóng, an toàn mà không cần phải ghi nhớ các câu lệnh phức tạp.

---

## 🛠️ Bộ tính năng cốt lõi

### 1. 🪟 Cứu hộ & Khắc phục lỗi Windows (Recovery)
- **Tự động quét hệ điều hành:** Dò tìm chính xác các bản Windows cài trên máy (kể cả khi bị đổi ký tự ổ sang D:, E:...).
- **Sửa lỗi Boot & BCD:** Nạp lại dữ liệu khởi động nhanh bằng `bcdboot`, `bootrec`, cấu hình lại BCD khi bị lỗi màn hình xanh hoặc "No bootable device".
- **Sửa file hệ thống offline:** Chạy `DISM /Cleanup-Image /RestoreHealth` và `SFC /ScanNow` trực tiếp trên bản Windows offline để vá các file hệ thống hỏng mà không cần cài lại Win.
- **Kiểm tra & sửa lỗi ổ đĩa:** Tích hợp công cụ quét bad sector, sửa lỗi phân vùng `CHKDSK`.

### 2. 📁 Trình duyệt File Offline (File Explorer)
- Trình quản lý file hai ngăn (Thư mục + Danh sách tệp) trực quan.
- Hỗ trợ thao tác: Duyệt mọi ổ đĩa, Sao chép (Copy), Cắt (Cut), Dán (Paste), Xóa (Delete), Đổi tên.
- Tích hợp chỉnh sửa file văn bản / cấu hình (`.txt`, `.log`, `.ini`, `.inf`, `.bat`) bằng **Notepad**.
- Phím tắt mở nhanh Command Prompt tại thư mục đang đứng và truy cập nhanh vào thư mục `Windows`, `System32`, `Users`.

### 3. 💾 Quản lý Đĩa & Phân vùng (Disk Management)
- Hiển thị danh sách ổ đĩa (HDD, SSD, NVMe, USB), thông tin dung lượng, chuẩn phân vùng MBR / GPT.
- Liệt kê chi tiết từng Partition (Dung lượng, File System NTFS/FAT32, Trạng thái phân vùng).
- Tích hợp công cụ dòng lệnh **DiskPart** phục vụ các tác vụ phân chia, gán ký tự ổ đĩa nâng cao.

### 4. 🔌 Quản lý & Khắc phục lỗi Driver
- **Export Drivers:** Trích xuất toàn bộ driver bên thứ ba (WiFi, NVMe, Âm thanh, Chipset...) từ Windows cũ lưu ra USB trước khi cài lại Win.
- **Inject Drivers:** Cài driver vào Windows offline (rất hữu ích khi cài Win xong bị mất driver chuột, bàn phím, touchpad hoặc thiếu driver ổ đĩa NVMe).
- **DrvLoad (Live):** Nạp driver trực tiếp vào môi trường WinPE đang chạy mà không cần khởi động lại.

### 5. 📦 Sao lưu & Phục hồi hệ thống (Backup & Restore)
- **Capture WIM:** Nén toàn bộ hệ điều hành Windows thành file ảnh `.wim` chất lượng cao.
- **Apply WIM:** Bung bản ghost/backup `.wim`, `.esd` vào phân vùng ổ cứng chỉ với vài cú click chuột.
- **Sao lưu tài liệu cá nhân:** Tự động quét và sao lưu các thư mục Desktop, Documents, Downloads của người dùng ra ổ cứng ngoài qua Robocopy.

### 6. 👤 Mở khóa & Đổi mật khẩu Windows (Account Recovery)
- Khắc phục trường hợp quên mật khẩu tài khoản máy tính Windows.
- Tích hợp thủ thuật can thiệp an toàn qua **Utilman / Sethc** để mở Command Prompt quyền SYSTEM ngay tại màn hình đăng nhập Windows và hướng dẫn đổi mật khẩu mới chỉ trong vài giây.

### 7. 🔍 Chẩn đoán & Giám sát phần cứng (Hardware Diagnostics)
- Xem thông tin chi tiết:
  - **CPU:** Tên vi xử lý, số nhân, số luồng, xung nhịp.
  - **RAM:** Tổng dung lượng, số khe cắm, tốc độ bus.
  - **Mainboard & BIOS:** Hãng sản xuất, phiên bản BIOS, số serial.
  - **Ổ cứng:** Model, dung lượng, chuẩn giao tiếp.
  - **VGA / Card đồ họa:** Tên card màn hình, độ phân giải hiện tại.

### 8. 🌐 Mạng & Kết nối Internet (Network Tools)
- Tự động nhận diện card mạng Ethernet / Wi-Fi.
- Cấu hình IP tĩnh hoặc nhận DHCP tự động.
- Công cụ kiểm tra kết nối: Ping kiểm tra độ trễ mạng, DNS Lookup.
- Kết nối tới ổ đĩa mạng chia sẻ (Map Network Drive / SMB) để lấy dữ liệu từ mạng nội bộ.

### 9. 🔩 Công cụ hệ thống tích hợp
- Mở nhanh các tiện ích kinh điển: Command Prompt (CMD), PowerShell, Registry Editor (Regedit), Task Manager, Notepad, msinfo32, netsh...
- Bảng nhật ký thời gian thực (Live Logs) ghi lại tất cả tiến trình thao tác để dễ dàng theo dõi.

---

## 🚀 Hướng dẫn tạo USB Boot cứu hộ

Sau khi đã có file `WinPE_Nghitr-dev-v1.0.0.iso`, bạn có thể tạo USB cứu hộ theo các cách sau:

### Cách 1: Sử dụng Rufus (Khuyên dùng)
1. Tải phần mềm [Rufus](https://rufus.ie/).
2. Cắm USB (dung lượng từ 2GB trở lên).
3. Tại mục **Boot selection**, chọn file `WinPE_Nghitr-dev-v1.0.0.iso`.
4. Mục **Partition scheme**:
   - Chọn **GPT** nếu máy tính chạy chuẩn UEFI (hầu hết máy đời mới).
   - Chọn **MBR** nếu cần tương thích cả máy đời cũ (Legacy BIOS).
5. Bấm **START** để tiến hành ghi vào USB.

### Cách 2: Sử dụng Ventoy (Tiện lợi nhất)
1. Cài đặt [Ventoy](https://www.ventoy.net/) lên USB của bạn.
2. Copy trực tiếp file `WinPE_Nghitr-dev-v1.0.0.iso` vào USB.
3. Khởi động máy tính và chọn file ISO từ menu của Ventoy để boot.

---

## 📦 Đóng gói ISO từ mã nguồn (Dành cho Developer)

Nếu bạn muốn tùy biến mã nguồn và tự đóng gói ra file ISO mới:

1. Đảm bảo máy tính đã cài đặt **Windows ADK (Deployment Tools)** và **WinPE Add-on**.
2. **Nhấp đúp chuột** vào file [build_iso.bat](file:///d:/Profile/Visual%20Code%20File/Tool_download_for_Nghi/WINPE/build_iso.bat).
3. Chọn **Yes** khi được hỏi quyền Quản trị viên (UAC).
4. Hệ thống sẽ tự động biên dịch ứng dụng GUI, nạp các gói thành phần WinPE, tích hợp driver và xuất file ISO bootable vào thư mục `output/`.

---

## ⚖️ Giấy phép & Bản quyền

- Phát triển bởi **Nghitr Dev**.
- Dự án phát hành theo giấy phép cá nhân phi thương mại.
- Không chứa phần mềm độc hại, mã độc hoặc các công cụ bẻ khóa vi phạm bản quyền.
