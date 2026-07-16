# Shattered Gate — Handoff / Tình trạng dự án

> Cập nhật lần cuối: 2026-07-16. File này tóm tắt toàn bộ những gì đã làm, chưa làm, và
> cách tiếp tục khi chuyển sang máy khác. Đọc file này trước khi làm gì tiếp theo.

## 1. Tổng quan dự án

- **Tên:** Shattered Gate (đã xác nhận không trùng game nào trên Steam — an toàn dùng).
- **Thể loại:** Node-based Roguelite, turn-based combat, Pixel Art, Dark Fantasy.
- **Engine:** Unity **6000.0.79f1** (bắt buộc dùng đúng bản này khi cài lại, khác bản dễ lỗi convert project).
- **Repo GitHub:** https://github.com/GiangJuly/ShatteredGateGame
- **Team gốc:** Group 4 (Huân - Systems/UI, Vân Khánh - Designer/PM, Quỳnh Giang - Tech Artist/QA, Đạt - Core Gameplay) — nhưng hiện tại đang làm **một mình**.
- **Tài liệu gốc:** [`docs/game-design-brief.md`](game-design-brief.md) (ý tưởng thiết kế) và `docs/Pitching Dự Án Game.pdf` (bản pitch Mid-term gốc, đã đối chiếu kỹ để build đúng ý tưởng).

## 2. Đã hoàn thành (tính đến giờ)

### Asset pipeline
- Đã chọn & tải asset pack: **Holder's Animated Battlers** (Free Heroes + Free Enemies), **0x72 DungeonTilesetII**, **Fantasy-UI.png**, **Frostwindz Animated Portal** — tất cả miễn phí, license thương mại OK.
- Đã cắt sprite (Sprite Editor) cho 22 nhân vật: 4 hero (Graham/Sally/Violet/James) + 18 enemy (đủ Tier A/B/C/D + Mechasphere bổ sung).
- Mỗi nhân vật có 4 Animation Clip (Idle loop / Hurt / Attack / Dead) + Animator Controller (3 Trigger: Attack/Hurt/Dead, 6 Transition chuẩn).
- Prefab đã tổ chức theo Tier: `Assets/Prefabs/Heroes/`, `Assets/Prefabs/Enemies/TierA..D/`.
- Đã convert sang Sprite: 4 portrait hero, 11 icon enemy, và **toàn bộ 370 file trong `DungeonTileset/frames/`** (tile tường modular, sàn, prop, icon heart HP...).

### Gameplay (vertical slice chơi được đầy đủ)
- Scene chính: `Assets/Scenes/Main.unity` — 1 scene duy nhất, chuyển màn bằng bật/tắt panel (không load scene rời).
- **Luồng chơi:** Main Menu → Node Map (5 chặng, mỗi chặng tối đa 3 cổng hình thoi: Quái Vật/Ác Thần/Chìa Khóa) → Combat (turn-based theo Speed, HP bar + portrait, nút Attack) → Cổng Chìa Khóa (lore text, tuyển đồng đội) → Final Boss (Zero) → Victory/Defeat.
- **Hệ thống tuyển mộ đúng PDF gốc:** bắt đầu chỉ có Graham, tuyển thêm Sally → Violet → James qua 3 Cổng Chìa Khóa (đúng "khởi đầu đơn độc, tối đa 3 đồng đội").
- Dùng đủ cả 5 boss Tier D (Treant/Asimole/Creeps/Demonpot/Mechasphere) cho 5 chặng + Zero làm Final Boss.
- Nền dungeon dùng Tilemap thật (8 biến thể sàn ngẫu nhiên + tường modular góc/cạnh/giữa) từ `DungeonTileset/frames/`.
- Camera shake khi đánh trúng, màu HP bar đổi theo % máu (xanh/vàng/đỏ).
- **Git repo đã tạo và push thành công** lên GitHub.

### Script đã viết
Runtime (`Assets/Scripts/`): `UnitStatsHolder.cs`, `CombatManager.cs`, `MapManager.cs`, `GameFlow.cs`, `CameraShake.cs`.

Editor tool (`Assets/Editor/`) — dùng qua menu **ShatteredGate** trong Unity:
| Tool | Việc làm | Khi nào chạy lại |
|---|---|---|
| `BattlerAnimSetup.cs` | Tạo Animation Clip + Animator cho enemy theo Tier, Portal loop | Chỉ khi thêm nhân vật/quái mới |
| `UnitStatsSetup.cs` | Gán HP/ATK/SPD lên toàn bộ prefab | Khi cần đổi chỉ số cân bằng |
| `ConvertToSprites.cs` | Convert portrait hero + icon enemy sang Sprite | Đã chạy xong, không cần lại trừ khi thêm nhân vật |
| `ConvertTilesetFrames.cs` | Convert 370 file trong `frames/` sang Sprite | Đã chạy xong |
| `FixTileMesh.cs` | Sửa Mesh Type sang Full Rect cho tile lặp mượt | Đã chạy xong |
| `SceneBuilder.cs` | **Dựng lại toàn bộ scene Main.unity từ đầu** | **Chạy lại mỗi khi sửa code UI/Map/Combat layout** |
| `BuildTools.cs` | Xuất file `.exe` Windows standalone | Chạy trước khi nộp bài |

⚠️ **Thứ tự bắt buộc nếu cần dựng lại scene từ đầu** (VD trên máy mới, hoặc sau khi sửa Editor script):
1. `ShatteredGate > Setup Hero Prefabs`
2. `ShatteredGate > Setup Unit Stats On All Prefabs`
3. `ShatteredGate > Convert Portraits & Icons To Sprites`
4. `ShatteredGate > Convert DungeonTileset Frames To Sprites`
5. `ShatteredGate > Fix Tile Sprite Mesh (Full Rect)`
6. `ShatteredGate > Build Vertical Slice Scene`

Nếu chỉ sửa 1-2 dòng trong `CombatManager.cs`/`MapManager.cs`/`GameFlow.cs` (logic gameplay) thì **không cần** chạy lại các bước trên — chỉ cần bấm Play test trực tiếp trên scene `Main.unity` đã có sẵn.

## 3. Việc CẦN LÀM TIẾP (theo thứ tự ưu tiên)

1. **Build file `.exe`** — chạy `ShatteredGate > Build Windows Executable (.exe)`, kiểm tra file chạy được ngoài Unity Editor. **Đây là việc bắt buộc để nộp bài** (giảng viên yêu cầu bản build, không phải chỉ project Unity).
2. **Xác nhận nền dungeon Tilemap mới** đã hiển thị đúng (vừa sửa xong bằng tile modular thật, chưa có ảnh xác nhận cuối cùng).
3. **Âm thanh (SFX)** — hiện tại game **chưa có âm thanh nào** (SFX đánh, thắng, thua, nhạc nền). Đã bị bỏ qua do ưu tiên gameplay trước. Cần tìm SFX CC0 miễn phí (itch.io/opengameart) và wire vào `CombatManager` (đã có sẵn field `hitSfx`/`victorySfx`/`defeatSfx`, chỉ cần gán AudioClip).
4. **Điều khiển bàn phím** — hiện tại chỉ dùng chuột (click nút). User có yêu cầu thêm phím tắt, chưa làm.
5. **Hiệu ứng chuyển cảnh/combat** — user có nhắc muốn thêm hiệu ứng dùng sprite có sẵn (VD Portal animation khi chuyển cảnh), đang để dành ý tưởng, chưa quyết định thiết kế cụ thể (user nói "để tôi suy nghĩ một chút").
6. **Bảng Combo Tag** giữa các kỹ năng — chưa thiết kế (việc còn mở từ brief gốc, cần Vân Khánh hoặc tự quyết định).
7. **Bảng Meta-progression unlock** — chưa thiết kế (việc còn mở từ brief gốc).
8. Cân nhắc: HP hiện **reset đầy 100% mỗi trận** (không cộng dồn rủi ro giữa các chặng như thiết kế roguelite thật) — là lựa chọn đơn giản hoá có chủ đích để kịp deadline, có thể nâng cấp sau nếu muốn đúng tinh thần "risk/reward" hơn.
9. Combat hiện tại chỉ hỗ trợ **1 kẻ địch / trận** (đủ dùng cho vertical slice, nhưng game đầy đủ có thể cần nhiều địch cùng lúc).
10. James không có ảnh portrait riêng (giới hạn của asset pack gốc, không phải lỗi).

## 4. Lưu ý khi chuyển sang máy khác

1. **Cài đúng Unity 6000.0.79f1** trước (qua Unity Hub) — khác bản Unity sẽ hỏi convert project, rủi ro lỗi.
2. Clone repo bằng:
   ```bash
   git clone https://github.com/GiangJuly/ShatteredGateGame.git
   ```
3. Mở project bằng Unity Hub → **Add** → chọn đúng folder vừa clone.
4. **Lần đầu mở sẽ chậm** (vài phút) vì Unity phải tự sinh lại folder `Library/` (không lưu trên Git, sẽ tự tạo). Đây là bình thường, không phải lỗi/treo máy — cứ đợi thanh progress bar chạy xong.
5. Sau khi mở xong, kiểm tra Console **không có lỗi đỏ** trước khi làm gì tiếp (nếu có, thường do thiếu package — kiểm tra `Packages/manifest.json` đã có `com.unity.2d.sprite` và `com.unity.ugui` chưa, 2 package này bắt buộc cho project).
6. Nếu Console báo lỗi liên quan tới thiếu prefab/animation khi mở scene `Main.unity` lần đầu — khả năng do thứ tự tạo asset qua Editor Tool (mục 2 ở trên) — chạy lại đúng thứ tự 6 bước đã liệt kê.
7. Sau khi sửa gì trên máy mới, nhớ đẩy ngược lại GitHub:
   ```bash
   git add .
   git commit -m "mô tả thay đổi"
   git push
   ```
   và khi quay lại máy cũ, nhớ `git pull` trước khi sửa tiếp — tránh làm việc trên bản cũ gây xung đột.
8. File `.gitignore` đã loại trừ `Library/`, `Temp/`, `Builds/`, `Logs/` — không cần lo các folder này làm nặng repo.

## 5. Cảnh báo Console không cần lo (bình thường, không phải lỗi)

- `Sprite Tiling might not appear correctly...` — cảnh báo cũ từ trước khi sửa Full Rect, có thể vẫn còn hiện với vài sprite chưa dùng tới, không ảnh hưởng gameplay.
- `Không tìm thấy sprite... James 1A[portrait].png` — James không có file portrait trong asset gốc, đã xử lý graceful (ẩn ảnh, không crash).
