// Quản lý trạng thái khởi tạo menu người dùng
document.addEventListener("DOMContentLoaded", function () {
    const userMenuBtn = document.getElementById('userMenuBtn');
    const userDropdown = document.getElementById('userDropdown');

    if (userMenuBtn && userDropdown) {
        userMenuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            userDropdown.classList.toggle('hidden');
        });
        document.addEventListener('click', function (e) {
            if (!userMenuBtn.contains(e.target) && !userDropdown.contains(e.target)) {
                userDropdown.classList.add('hidden');
            }
        });
    }
});

// ================= XỬ LÝ KHUNG CHAT AI =================
function toggleAiChat() {
    const chatWin = document.getElementById('aiChatWindow');
    const chatBtn = document.getElementById('floatingAiButton');

    if (chatWin.classList.contains('hidden')) {
        // Đang ẩn -> Mở khung chat, ẩn nút tròn
        chatWin.classList.remove('hidden');
        chatWin.classList.add('flex');

        if (chatBtn) {
            chatBtn.classList.add('hidden');
            chatBtn.classList.remove('flex');
        }

        document.getElementById('aiChatInput').focus();
    } else {
        // Đang mở -> Đóng khung chat, hiện lại nút tròn
        chatWin.classList.add('hidden');
        chatWin.classList.remove('flex');

        if (chatBtn) {
            chatBtn.classList.remove('hidden');
            chatBtn.classList.add('flex');
        }
    }
}

function handleAiChatEnter(e) {
    if (e.key === 'Enter') sendAiMessage();
}

async function sendAiMessage() {
    const input = document.getElementById('aiChatInput');
    const text = input.value.trim();
    if (!text) return;

    const body = document.getElementById('aiChatBody');

    // Hiện tin nhắn người dùng
    body.insertAdjacentHTML('beforeend', `
        <div class="bg-primary text-white p-3 rounded-2xl rounded-tr-none shadow-sm max-w-[85%] self-end text-sm leading-relaxed">
            ${text}
        </div>
    `);
    input.value = '';
    body.scrollTop = body.scrollHeight;

    // Hiện trạng thái AI đang tải
    const loadingId = 'loading-' + Date.now();
    body.insertAdjacentHTML('beforeend', `
        <div id="${loadingId}" class="flex gap-2 w-[85%]">
            <div class="w-7 h-7 shrink-0 rounded-full bg-primary text-white flex items-center justify-center shadow-sm">
                <span class="material-symbols-outlined text-[14px]">smart_toy</span>
            </div>
            <div class="bg-white p-3 rounded-2xl rounded-tl-none border border-gray-100 shadow-sm text-sm text-outline flex items-center gap-2">
                <span class="material-symbols-outlined animate-spin text-[16px]">hourglass_empty</span>
                Đang suy nghĩ...
            </div>
        </div>
    `);
    body.scrollTop = body.scrollHeight;

    try {
        // Gọi API tới C# Controller
        const res = await fetch('/api/Ai/Chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: text }) // Chuyển câu hỏi thành JSON
        });

        const data = await res.json();

        // Xóa biểu tượng loading
        const loadingEl = document.getElementById(loadingId);
        if (loadingEl) loadingEl.remove();

        if (data.success) {
            // TẠO HTML CHO CÂU TRẢ LỜI CỦA AI VÀ CÁC NÚT TẢI FILE
            let messageHtml = `
            <div class="flex gap-2 w-[85%] mb-4">
                <div class="w-7 h-7 shrink-0 rounded-full bg-primary text-white flex items-center justify-center shadow-sm">
                    <span class="material-symbols-outlined text-[14px]">smart_toy</span>
                </div>
                <div class="flex flex-col gap-2 w-full">
                    <div class="bg-white p-3 rounded-2xl rounded-tl-none border border-gray-100 shadow-sm text-sm text-gray-800 leading-relaxed break-words">
                        ${data.reply}
                    </div>`;

            // KIỂM TRA NẾU CÓ FILE THÌ VẼ NÚT TẢI
            if (data.attachedFiles && data.attachedFiles.length > 0) {
                messageHtml += `<div class="flex flex-col gap-1.5 mt-1">`;
                data.attachedFiles.forEach(file => {
                    messageHtml += `
                        <a href="${file.url}" target="_blank" class="flex items-center gap-2 p-2 bg-blue-50 border border-blue-100 rounded-xl text-primary hover:bg-primary hover:text-white transition-colors active:scale-95 shadow-sm text-xs font-bold w-fit">
                            <span class="material-symbols-outlined text-[16px]">${file.icon}</span>
                            ${file.tenFile}
                            <span class="material-symbols-outlined text-[16px] ml-1">download</span>
                        </a>
                    `;
                });
                messageHtml += `</div>`;
            }

            // Đóng các thẻ
            messageHtml += `</div></div>`;

            // In kết quả ra màn hình
            body.insertAdjacentHTML('beforeend', messageHtml);

        } else {
            body.insertAdjacentHTML('beforeend', `
                <div class="bg-red-50 text-error p-3 rounded-2xl rounded-tl-none border border-red-100 shadow-sm max-w-[85%] self-start text-sm">
                    ${data.reply}
                </div>
            `);
        }

    } catch (e) {
        const loadingEl = document.getElementById(loadingId);
        if (loadingEl) loadingEl.remove();

        body.insertAdjacentHTML('beforeend', `
            <div class="bg-red-50 text-error p-3 rounded-2xl rounded-tl-none border border-red-100 shadow-sm max-w-[85%] self-start text-sm">
                Mất kết nối đến máy chủ. Vui lòng thử lại!
            </div>
        `);
    }
    body.scrollTop = body.scrollHeight;
}