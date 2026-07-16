# Shattered Gate — Game Design Brief (Working Doc)

> Nguồn gốc: Mid-term Pitching Report của Group 4 ("Node-based Roguelite" — Tổng quan Thể Loại, Cốt Truyện & Core Gameplay). Tài liệu này mở rộng bản pitch gốc thành hướng thiết kế rõ ràng hơn, tập trung vào yêu cầu **game phải fun**. Đây không phải bản final — cập nhật khi nhóm quyết định thêm.

## Đội ngũ (từ bản pitch gốc)

| Thành viên | Vai trò |
|---|---|
| Nguyễn Huân | Systems & UI/UX Programmer |
| Nguyễn Ngọc Vân Khánh | Game Designer & Project Manager |
| Nguyễn Lê Quỳnh Giang | Technical Artist & QA |
| Trần Tuấn Đạt | Core Gameplay Programmer |

## Tổng quan

- **Tên:** Shattered Gate (working title)
- **Thể loại:** Node-based Roguelite, turn-based combat
- **Nền tảng:** PC, Unity 6.3
- **Art style:** Pixel Art retro (asset itch.io), thế giới Dark Fantasy
- **Cốt truyện:** Main rơi vào thế giới bị phân mảnh thành nhiều chiều không gian do một biến cố thảm khốc. Lối thoát là xuyên qua các Cổng Không Gian (Portals) — mỗi cổng hé lộ 1 mảnh cốt truyện. Mục tiêu: tìm đường về, định đoạt số phận các chiều không gian.

## Fun Pillars (chốt trước khi build hệ thống khác)

1. **Quyết định rủi ro/phần thưởng khi chọn cổng** — mỗi lựa chọn không thể sửa, ảnh hưởng vĩnh viễn tới run.
2. **Combo chiến thuật giữa 4 thành viên** — phối hợp kỹ năng, không phải 4 DPS riêng lẻ.
3. **Cảm giác build-crafting theo từng run** — đội hình + kỹ năng khác nhau mỗi lần chơi.

Mọi hệ thống mới phải phục vụ ít nhất 1 pillar ở trên — tránh nhồi cơ chế không cần thiết.

## Core Gameplay Loop

```
Bắt đầu run (chọn Main, đơn độc)
  → Bản đồ Node hiện ra các cổng (icon loại + độ nguy hiểm mờ)
  → Chọn 1 cổng (không thể đổi ý)
    → Cổng Quái Vật: combat thường (EXP/tài nguyên)
    → Cổng Ác Thần: boss chặng (đòi hỏi combo đúng)
    → Cổng Chìa Khóa: giải đố/sự kiện/lore, đôi khi tuyển đồng đội (tối đa 3 lần/run)
  → Quay lại bản đồ, lặp lại qua 5 chặng (độ khó tăng dần)
  → Final Boss (tổng hợp mọi cơ chế đã học)
  → Kết thúc run (thắng/thua) → thưởng meta nhỏ → run mới
```

## Cơ chế cổng (đã tinh chỉnh so với bản pitch gốc)

- Icon cho biết **loại** cổng (đã có trong pitch gốc).
- Thêm: viền cổng sáng dần theo **độ nguy hiểm** — quyết định là tính toán rủi ro thực sự, không phải đoán mù.
- Cân nhắc kỹ năng/vật phẩm "Insight" (nhìn trước 1 phần thông tin cổng) làm phần thưởng hiếm.

## Combat (turn-based)

- Hiển thị rõ **thứ tự lượt** (theo Speed) của cả đội và địch để lập kế hoạch trước.
- Tài nguyên hành động giới hạn (AP/mana) — không thể spam kỹ năng mạnh nhất mỗi lượt.
- **Combo Tag**: kỹ năng gắn tag hiệu ứng (Choáng, Dễ Vỡ, Thiêu Đốt...), kỹ năng khác "ăn theo" tag để kích hoạt combo — đây là cơ chế xương sống tạo chiều sâu chiến thuật.
- Một số quái cũng biết combo lẫn nhau/gây hiệu ứng lên đội mình → buộc ưu tiên đúng mục tiêu.
- Đội hình tối đa 4 người (1 Main + tối đa 3 đồng đội tuyển được qua Cổng Chìa Khóa).

## Cấu trúc độ khó (5+1 chặng)

| Chặng | Mục tiêu thiết kế |
|---|---|
| 1-2 | Dạy cơ chế cơ bản, chưa cần combo phức tạp |
| 3 | Buộc dùng combo tag (địch có giáp/kháng) |
| 4-5 | Đội hình đủ 4 người, địch biết combo lại mình |
| Final Boss | Bài test tổng hợp toàn bộ cơ chế, không giới thiệu mới |

## Meta-progression (bổ sung — bản pitch gốc chưa có, nhưng cần để giữ chân người chơi)

Sau mỗi run (kể cả thua): mở khoá nhân vật mới có thể tuyển, buff vĩnh viễn nhỏ, hoặc thêm 1 mảnh lore. Không cần hệ thống đồ sộ — bảng unlock đơn giản là đủ cho scope hiện tại.

## Game Feel / Feedback

Không bỏ qua dù dùng asset có sẵn: screen shake nhẹ khi crit, âm thanh khi combo nổ, số damage rõ ràng, hit-pause ngắn khi đòn quyết định. Rẻ nhưng ảnh hưởng "fun" nhiều.

## Việc còn mở (chưa quyết định)

- [ ] Xác nhận tên "Shattered Gate" không trùng game đã có trên Steam.
- [ ] Nguồn asset pixel art cụ thể (itch.io pack nào) — chưa chọn.
- [ ] Khởi tạo Unity project + git repo tại `D:\Projects\ShatteredGate` (folder hiện đang trống).
- [ ] Chi tiết bảng combo tag giữa các kỹ năng.
- [ ] Chi tiết bảng meta-progression unlock.
