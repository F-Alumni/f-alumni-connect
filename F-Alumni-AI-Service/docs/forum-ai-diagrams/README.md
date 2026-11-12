# Forum + AI diagrams

Hai bộ sơ đồ được dựng từ source code, không gọi API hoặc kết nối database.

| Module | Xem trực tiếp | Chỉnh sửa | Source / notes |
| --- | --- | --- | --- |
| Forum | [Forum-Diagrams.html](Forum-Diagrams.html) | [Forum-Diagrams.drawio](Forum-Diagrams.drawio) | [Forum-Diagrams.md](Forum-Diagrams.md) |
| AI | [AI-Diagrams.html](AI-Diagrams.html) | [AI-Diagrams.drawio](AI-Diagrams.drawio) | [AI-Diagrams.md](AI-Diagrams.md) |

Mỗi bộ gồm 4 trang: Sequence Diagram, Class Diagram ứng dụng, Class Diagram entities và ERD.

Mở HTML bằng Chrome/Edge để xem offline và tải SVG; mở `.drawio` trong diagrams.net để chỉnh sửa.

Chạy lại bằng `node docs/forum-ai-diagrams/generate-diagrams.cjs` từ AI repo; cần hai repo Forum và AI cạnh nhau. Không sửa các bản sinh tự động; sửa generator rồi chạy lại. Không thay đổi runtime code, migrations hoặc tài liệu cũ.
