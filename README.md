# Runavelle

Game xây vương quốc nhỏ + phòng thủ lâu đài thời gian thực + roguelite nhẹ (Unity 2D, PC/Android/iOS, giao diện tiếng Việt).
Ban ngày xây nhanh bằng **Vàng** và **Mana** (chỉ 2 tài nguyên); ban đêm điều khiển **một Tướng** trong khi lính, tháp và
tướng đồng minh AI tự chiến đấu; 8 hệ nguyên tố tạo phản ứng; sống sót 15 đêm.

## Mở dự án
1. Unity Hub → Add → thư mục repo này. Editor: **Unity 6000.4.8f1** (cùng bản với Arcane-Brigade). Chưa từng được mở thử
   trong môi trường phát triển hiện tại (không có Unity Editor) – lần mở đầu tiên là lần kiểm chứng Unity thật đầu tiên.
2. Menu **Runavelle → 1. Setup project** (tạo `Assets/Runavelle/Scenes/Main.unity`, Build Settings, PlayerSettings ngang).
3. Nhấn Play ở bất kỳ scene nào – game tự khởi tạo bằng code (`Bootstrap`).
4. **Runavelle → 2. Run checks**: kiểm tra content, sprite và chạy mô phỏng 3 đêm trên mọi bản đồ.
5. **Runavelle → 3/4/5**: build Windows / Android (APK ARM64, cần Android module) / iOS (Xcode, cần macOS).

Điều khiển: xem `Docs/CONTROLS.md`.

## Công cụ ngoài Unity (đã chạy thật trong quá trình phát triển)
```bash
dotnet test Tools/Tests                                   # test tương đương EditMode
dotnet run --project Tools/Sim -c Release -- seeds=8      # chạy cân bằng headless (bot AutoPlayer, 15 đêm)
dotnet run --project Tools/Sim -c Release -- catalog      # sinh Docs/Content_Catalog.md từ dữ liệu thật
./Tools/fetch_unity_refs.sh && dotnet build Tools/UnityCheck   # compile-check lớp Unity/Editor
python3 Tools/Art/build_art.py --sheet                    # sinh lại toàn bộ sprite (Skia) + contact sheet
python3 Tools/Audio/build_audio.py                        # sinh lại toàn bộ SFX/nhạc (numpy)
```
Yêu cầu: .NET 8 SDK; Python 3 với `skia-python numpy scipy soundfile pillow`.

## Tài liệu
- `Docs/GDD.md` – thiết kế; `Docs/ARCHITECTURE.md` – kiến trúc; `Docs/Content_Catalog.md` – nội dung (sinh tự động)
- `Docs/Balance_Spec.md` – số liệu & kết quả mô phỏng; `Docs/QA_Matrix.md` – test và trạng thái
- `Docs/Expansion_Progress.md` – tiến độ MVP + 5 phase mở rộng; `Docs/KNOWN_ISSUES.md`; `Docs/CHANGELOG.md`
- `Docs/Previews/` – contact sheet sprite và ảnh cảnh dựng từ snapshot mô phỏng thật (**không phải ảnh chụp Unity**)

## Nguồn gốc tài sản
Toàn bộ hình ảnh và âm thanh được sinh bằng code trong repo (Tools/Art, Tools/Audio) – không dùng ảnh/âm thanh bên ngoài.
Font: Be Vietnam Pro, Young Serif, Noto Sans Mono (SIL OFL, kèm file giấy phép trong `Assets/Runavelle/Resources/Fonts`).
