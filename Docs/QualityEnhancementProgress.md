# Tiến độ nâng cấp chất lượng (Game Feel, Quality & Operations)

Trạng thái: DONE (có bằng chứng) · PARTIAL · BLOCKED · NOT STARTED. Không có Unity Editor/thiết bị: mọi mục "DONE"
được chứng minh bằng test NUnit, harness headless, kiểm tra biên dịch Unity và ảnh render snapshot — **không phải
chạy trong Unity**.

| Phase | Trạng thái | Bằng chứng |
|---|---|---|
| 0 Audit & baseline | DONE | `Docs/QualityEnhancementAudit.md`, `Docs/PerformanceBenchmarks.md` (đo trước/sau) |
| 1 Combat feel | DONE (logic) / PARTIAL (hình ảnh) | `Core/Presentation/Feedback.cs`, `ElementGrammar.cs`, `Game/Vfx.cs`, `GameRoot.Feedback.cs`; test `QualityTests` (tầng, ngân sách, rung, số sát thương, 8 ngữ pháp, 21 phản ứng). Chưa xem trong Unity. |
| 2 Kingdom feel | DONE (code) / PARTIAL (hình ảnh) | hoạt ảnh dựng/nâng cấp, vết nứt tường, đạo cụ lâu đài, chim/lá, hoàng hôn/bình minh, cảnh báo đặt công trình chặn lối (test) |
| 3 Army & AI | DONE | sửa lỗi lính kẹt trong Doanh Trại; lính tầm xa giữ khoảng cách; giới hạn truy đuổi; ưu tiên lỗ thủng; AiTuning; bảng gỡ lỗi; 4 test |
| 4 Atmosphere & audio | DONE (logic/âm thanh tổng hợp) / PARTIAL (chưa nghe thử) | MusicDirector (test trễ chống giật), bus + giới hạn giọng, 15 âm thanh mới, dàn trận (test ngân sách giữ nguyên), phụ đề |
| 5 Progression | DONE | Thành Lũy Cuối Cùng (test 1 lần/đêm, telemetry), cột mốc (test), xem trước nâng cấp (test) |
| 6 UX & accessibility | DONE (code) | Cài đặt 3 thẻ (lỗi cắt dòng đã sửa), rung 0/½/1, số sát thương 3 chế độ, giảm chuyển động, môi trường, zoom, phụ đề, vòng tầm, đánh dấu lỗ thủng, sửa ký tự thiếu font |
| 7 Advanced | DONE: Highlights (tóm tắt sự kiện, **không** phải video), barks, chiến trường nguyên tố (test giới hạn/không dây chuyền), tàn tích công thành; Castle Transformation PARTIAL (đạo cụ đơn giản) | test `QualityTests`, `QualityTests2` |
| 8 Testing & balance | DONE | 105/105 test; 256 + 3×96 ván harness, 0 lỗi; chỉ số breach/HP/reactions/kill-source |
| 9 Performance & release | DONE (headless) / NOT RUN (thiết bị) | cấp phát 2–7,5 KB → ~0,5 KB/tick; pool view Unity; khôi phục save khi ghi dở (test); `Docs/ReleaseReadinessChecklist.md` |

BLOCKED: Unity Editor/PlayMode/profiler/screenshot/video, build Windows/Android/iOS, đo FPS thiết bị, nghe thử âm thanh,
Unity MCP, Blender MCP.
