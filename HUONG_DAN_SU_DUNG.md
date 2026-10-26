# HƯỚNG DẪN SỬ DỤNG CHI TIẾT: ALL-IN-ONE ANDROID TV BOX SUITE PRO (BẢN MASTER OFFLINE)

Tài liệu này hướng dẫn cách vận hành toàn bộ các tính năng của phần mềm **[AllInOne_TVBox_Suite.exe](file:///d:/Contains/DV9135_TOOL_PRO_FINAL.exe/AllInOne_TVBox_Suite/bin/Release/net48/AllInOne_TVBox_Suite.exe)** để mở khóa, tối ưu hóa, nạp Launcher, can thiệp Bootloader và nghiên cứu các dòng Android TV Box mới.

---

## 1. Khởi Động & Cấu Trúc Thư Mục Tự Sinh

Khi bạn mở file **[AllInOne_TVBox_Suite.exe](file:///d:/Contains/DV9135_TOOL_PRO_FINAL.exe/AllInOne_TVBox_Suite/bin/Release/net48/AllInOne_TVBox_Suite.exe)** lần đầu tiên, phần mềm sẽ tự động giải nén bộ công cụ nền (`adb.exe`, `fastboot.exe`, `adnl.exe`, `dv9135.apk`, `DVT_DV9135_EnvHelper.apk`) và tạo sẵn **3 thư mục làm việc** ngay cạnh file `.exe` (bạn có thể mở nhanh bằng 3 nút ở góc dưới bên trái phần mềm):

1. **`Profiles/`**: Chứa các file cấu hình `.json` của từng dòng Box (`viettel_sdmc_dv9135.json`, `viettel_zte_b866v2f.json`, `fpt_playbox_all.json`, `vnpt_smartbox_mytv.json`, `generic_universal_box.json`). Bạn có thể mở sửa bằng Notepad hoặc lưu trực tiếp từ giao diện phần mềm.
2. **`Apps/`**:
   - `Apps/Launchers/`: Bỏ file `.apk` của các Launcher bạn muốn cài mặc định cho khách (ví dụ: *Projectivy Launcher*, *ATV Launcher Pro*...) vào đây.
   - `Apps/DefaultApps/`: Bỏ các ứng dụng giải trí muốn cài hàng loạt (YouTube không quảng cáo, xem Tivi, Phim, Karaoke...) vào đây. Khi bấm **Mở khóa Phần mềm 1-Click**, phần mềm sẽ tự động cài tất cả file `.apk` nằm trong thư mục `Apps/`.
3. **`Dumps/`**: Nơi lưu toàn bộ file hệ thống (`Settings.apk`, `TvSettings.apk`, `getprop`, `droidlogic.jar`...) hút ra từ các dòng Box mới khi dùng **Tab 5**.

---

## 2. Cách Kết Nối Máy Tính Với Android TV Box

### Bước A: Nếu Box chưa bật ADB (Đòi mật khẩu Developer Options trên TV)
1. Trên TV Box, vào **Cài đặt (Settings) $\rightarrow$ Giới thiệu (About) $\rightarrow$ Bấm 7 lần vào Bản dựng (Build Number)** để hiện mục *Tùy chọn nhà phát triển (Developer Options)*.
2. Khi Box hiện bảng hỏi mật khẩu để bật **Gỡ lỗi USB (USB Debugging)**:
   - Mở **Tab 2 (Tạo Pass ADB & LAN)** trên phần mềm.
   - Nhập **Số Serial (SN)** (và **Địa chỉ MAC + Ngày trên TV** nếu là Android 11) vào ô tương ứng.
   - Mật khẩu sẽ hiện ngay lập tức ở khung bên phải:
     - **Android 14:** Chuỗi **6 ký tự** chữ thường/số.
     - **Android 11:** Chuỗi **10 ký tự**.
   - Nhập mật khẩu này lên TV để bật **USB Debugging** (hoặc **Network Debugging**).

### Bước B: Kết nối qua Cáp USB hoặc Mạng LAN (Wi-Fi / Dây mạng)
- **Cách 1 — Qua cáp USB 2 đầu đực (USB-A to USB-A):**
  - Cắm cáp vào cổng USB của Box và máy tính.
  - Nhìn lên màn hình TV, tích chọn **"Luôn cho phép từ máy tính này" (Always allow from this computer)** rồi bấm **OK**.
  - Phần mềm sẽ tự động nhận diện trong vòng 3–4 giây (hoặc bấm nút **Làm mới** trên góc phải).
- **Cách 2 — Qua mạng LAN / Wi-Fi (Không cần cáp USB):**
  - Xem địa chỉ IP của Box trong phần Cài đặt mạng trên TV (ví dụ: `192.168.1.15`).
  - Gõ địa chỉ IP vào ô góc trên bên phải phần mềm rồi bấm nút **Kết nối LAN**.

> [!TIP]
> Ngay khi kết nối thành công, **6 Thẻ Thông Số (Live Telemetry)** ở thanh ngang phía trên sẽ sáng đèn màu và hiển thị đầy đủ: `Hãng/Model`, `Phiên bản Android`, `Chip SoC`, `Serial / MAC`, `SELinux` và `Trạng thái Bootloader (LOCKED / UNLOCKED)`.

---

## 3. Hướng Dẫn Chi Tiết Từng Tab Chức Năng

### TAB 1: Trung Tâm 1-Click (Dành cho thao tác nhanh hàng loạt)
Khi cắm Box vào, phần mềm sẽ tự động đọc Model của Box và chọn sẵn **Hồ sơ thiết bị (Device Profile)** tương ứng ở khung bên trái.

Bạn có **3 nút tác vụ chính** ở khung bên phải:
1. **`🚀 MỞ KHÓA PHẦN MỀM 1-CLICK (DEBLOAT + CHẶN OTA + CÀI LAUNCHER)`**:
   - **Tác dụng:** Tự động đóng băng (`disable-user`) toàn bộ ứng dụng kiểm soát thuê bao/khóa màn hình của nhà mạng, chặn dịch vụ tự động cập nhật OTA, và tự động cài đặt toàn bộ file `.apk` bạn đã bỏ sẵn trong thư mục `Apps/`.
   - **Khi nào dùng:** Dùng cho **mọi dòng Box** (Viettel, FPT, VNPT, Box quốc tế) khi bạn chỉ cần mở khóa để khách xem Tivi, dùng Launcher ngoài mà không cần nạp lại ROM.
2. **`🔓 MỞ KHÓA BOOTLOADER / U-BOOT 1-CLICK`**:
   - **Tác dụng:** Can thiệp vào phân vùng `ubootenv` để mở khóa Bootloader (`oem_lock=unlock`, `avb2=0`, `EnableSelinux=permissive`).
   - **Khi nào dùng:** Dùng cho dòng **Viettel SDMC DV9135** (Android 11 & Android 14) trước khi nạp ROM ATV / Google TV.
3. **`📲 CÀI ĐẶT & KÍCH HOẠT APP HỖ TRỢ DV9135.APK`**:
   - **Tác dụng:** Đẩy ứng dụng phụ trợ vào Box và tự động bắn lệnh mở khóa cài đặt ứng dụng không rõ nguồn gốc (*Unknown Sources*).

---

### TAB 2: Tạo Pass ADB & Kết Nối (Giải mã mật khẩu Offline)
- Dùng để tính mật khẩu ADB ngay lập tức mà **không cần Internet**.
- Bạn chỉ cần gõ **Serial Number (SN)** vào ô số 2, mật khẩu **Android 14 (6 ký tự)** sẽ tự nhảy ra ngay khi đang gõ.
- Nếu làm máy **Android 11**, gõ thêm **Địa chỉ MAC** vào ô số 3 và **Ngày hiển thị trên TV (`YYYYMMDD`)** vào ô số 4 để lấy mật khẩu **10 ký tự**.
- Nếu Box đã cắm cáp vào máy tính, bấm nút **`⚡ Tự động lấy SN & MAC từ Box đang cắm`** để phần mềm tự điền.

---

### TAB 3: Quản Lý App, Debloat & Cài Launcher (Tinh chỉnh chuyên sâu)
Đây là công cụ mạnh nhất để bạn kiểm soát từng ứng dụng bên trong Box:
1. Bấm nút **`🔍 Quét Toàn Bộ App Trên Box`**:
   - Toàn bộ ứng dụng trên Box sẽ hiện ra và được **phân loại màu tự động**:
     - **Màu Đỏ (`[ĐỎ] App Nhà Mạng / Kiểm Soát`)**: Các gói của Viettel, FPT, VNPT, ZTE, SDMC, TR-069... (Mặc định được tích chọn sẵn để bạn khóa).
     - **Màu Cam (`[CAM] Cập Nhật OTA / Hệ Thống`)**: Các gói tự động cập nhật firmware (Mặc định tích chọn sẵn để chặn cập nhật khóa lại máy).
     - **Màu Xanh Lá (`[XANH] Giao Diện Launcher`)**: Các trình khởi chạy màn hình chính.
     - **Màu Xanh Dương (`[XANH DƯƠNG] App Cài Thêm`)**: Các ứng dụng người dùng cài thêm.
2. **Các thao tác ở thanh công cụ dưới cùng:**
   - **`❄️ Đóng Băng (Disable) Các Gói Đã Tích`**: Tắt hoàn toàn các ứng dụng đã chọn (an toàn nhất, không gây treo logo, có thể bật lại bất cứ lúc nào).
   - **`✓ Bật Lại (Enable) Gói Đã Tích`**: Khôi phục lại ứng dụng nếu lỡ tay tắt nhầm.
   - **`🗑️ Gỡ Bỏ (Uninstall User 0)`**: Xóa gói ứng dụng khỏi không gian người dùng hiện tại.
   - **`📦 Chọn File APK Cài Vào Box...`** *(hoặc **Kéo-Thả file `.apk`** từ ngoài màn hình Windows thả thẳng vào phần mềm)*: Cài đặt ứng dụng mới vào Box ngay lập tức.
   - **`💾 Lưu Các Gói Đã Tích Thành Profile Chuẩn`**: Sau khi bạn tích chọn xong danh sách các gói cần khóa cho một dòng Box mới, bấm nút này để lưu vĩnh viễn vào file `.json`. Lần sau cắm chiếc Box cùng loại vào chỉ cần qua **Tab 1** bấm **1-Click** là xong!

---

### TAB 4: Bootloader, U-Boot Env & Chế Độ Nạp ROM
Dành cho việc nạp ROM (Flash Firmware) và can thiệp tầng thấp:
- **Cụm nút bên trái:**
  - Mở khóa U-Boot Offline cho DV9135 (tự động nhận diện Android 11 dùng `platformapi` hay Android 14 dùng `DVT_DV9135_EnvHelper.apk`).
  - Đưa máy vào **Fastboot Mode**, **Amlogic USB Burning Mode (`reboot update`)**, **ADNL Burn Mode (`preboot=adnl 1200`)**, hoặc **Recovery Mode** chỉ bằng 1 cú click mà không cần chập chân phần cứng hay giữ nút Reset.
- **Bảng bên phải (Đọc & Sửa biến `ubootenv`):**
  - Bấm **`📥 Đọc Toàn Bộ Biến U-Boot Hiện Tại`** để kiểm tra xem `oem_lock`, `lock`, `avb2`, `EnableSelinux` trên Box đang có giá trị gì.
  - Chọn bất kỳ dòng nào trong bảng để sửa giá trị và bấm **`✍️ Ghi Biến U-Boot`**.

---

### TAB 5: Rút APK Hệ Thống & Nghiên Cứu Box Mới (Mở rộng cho mọi thiết bị)
Khi bạn gặp một chiếc Box mới (ví dụ một mẫu FPT Play Box mới, ZTE mới, hoặc VNPT mới) mà chưa có trong danh sách hỗ trợ:
1.Mở **Tab 5**, giữ nguyên 3 ô tích chọn ở khung bên trái và bấm nút:
   **`🔬 BẮT ĐẦU HÚT DỮ LIỆU BOX ĐANG CẮM VỀ MÁY TÍNH`**
2. Phần mềm sẽ tự động tạo một thư mục riêng trong `Dumps/<Tên_Model>_<Giờ>/` và rút toàn bộ:
   - `1_getprop_full.txt` (Toàn bộ thông số phần cứng, chip, bootloader).
   - `2_packages_with_paths.txt` (Danh sách toàn bộ gói ứng dụng và đường dẫn).
   - `3_content_providers.txt` (Danh sách các cổng ContentProvider và quyền `sharedUserId` để tìm lỗ hổng mở khóa Bootloader).
   - Thư mục `Extracted_APKs/`: Chứa các file `Settings.apk`, `TvSettings.apk`, và các file `.apk` của nhà mạng/hãng sản xuất.
   - Thư mục `Vendor_Frameworks/`: Chứa `droidlogic.jar` và `build.prop`.
3. Sau khi hút xong, thư mục đó sẽ tự động bật lên trên màn hình Windows. Bạn chỉ cần báo cho tôi tên thư mục vừa hút để tôi dịch ngược ngay thuật toán mật khẩu ADB và lỗ hổng hệ thống của dòng Box mới đó rồi cập nhật vào phần mềm cho bạn!
