// Khởi tạo và xử lý đọc an toàn biến ID được lấy từ dataset của thẻ body
const myId = parseInt(document.getElementById('chatConfigContext')?.dataset.myId);

if (isNaN(myId)) {
    window.location.href = "/Account/Login";
}

let friendId = null;
let isCurrentChatGroup = false;
const chatCanvas = document.getElementById('chatCanvas');
const txtMessage = document.getElementById('txtMessage');
const btnSend = document.getElementById('btnSend');
const userList = document.getElementById('userList');
const txtSearchUser = document.getElementById('txtSearchUser');

const chatHeaderBox = document.getElementById('chatHeaderBox');
const chatHeaderName = document.getElementById('chatHeaderName');
const chatHeaderAvatar = document.getElementById('chatHeaderAvatar');
const chatHeaderStatus = document.getElementById('chatHeaderStatus');
const btnGroupSettings = document.getElementById('btnGroupSettings');

const btnAttach = document.getElementById('btnAttach');
const fileAttach = document.getElementById('fileAttach');
const uploadOverlay = document.getElementById('uploadOverlay');

let allConversations = [];
let currentTab = 'all';

// ==========================================
// QUẢN LÝ NHÓM
// ==========================================
let currentGroupData = null;
let selectedNewMemberId = null;

async function openGroupSettings() {
    if (!isCurrentChatGroup || !friendId) return;
    try {
        const res = await fetch(`/api/chat/group/${friendId}/details`);
        if (res.ok) {
            currentGroupData = await res.json();
            document.getElementById('settingGroupName').textContent = currentGroupData.tenNhom;
            document.getElementById('settingGroupAvatar').src = currentGroupData.anhNhom || `https://ui-avatars.com/api/?name=${currentGroupData.tenNhom}&background=0058bc&color=fff&bold=true`;
            document.getElementById('countMembers').textContent = currentGroupData.members.length;

            const isOwner = currentGroupData.nguoiTaoId === myId;
            document.getElementById('btnDeleteGroup').classList.toggle('hidden', !isOwner);
            document.getElementById('btnEditGroupName').classList.toggle('hidden', !isOwner);

            cancelEditGroupName();
            renderGroupMembersList();
            document.getElementById('modalGroupSettings').classList.remove('hidden');
        }
    } catch (e) { console.error(e); }
}

function enableEditGroupName() {
    document.getElementById('boxDisplayName').classList.add('hidden');
    document.getElementById('boxEditName').classList.remove('hidden');
    const input = document.getElementById('txtNewGroupName');
    input.value = currentGroupData.tenNhom;
    input.focus();
}

function cancelEditGroupName() {
    document.getElementById('boxDisplayName').classList.remove('hidden');
    document.getElementById('boxEditName').classList.add('hidden');
}

async function submitUpdateGroupName() {
    const newName = document.getElementById('txtNewGroupName').value.trim();
    if (!newName) return alert("Tên nhóm không được để trống!");
    if (newName === currentGroupData.tenNhom) return cancelEditGroupName();

    try {
        const res = await fetch(`/api/chat/group/${friendId}/name?requesterId=${myId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newName)
        });

        if (res.ok) {
            const data = await res.json();
            currentGroupData.tenNhom = data.newName;
            document.getElementById('settingGroupName').textContent = data.newName;
            chatHeaderName.textContent = data.newName;
            cancelEditGroupName();
            loadRecentConversations();
        } else {
            const err = await res.text();
            alert("Lỗi: " + err);
        }
    } catch (e) { console.error(e); }
}

function closeGroupSettings() {
    document.getElementById('modalGroupSettings').classList.add('hidden');
    document.getElementById('boxAddMember').classList.add('hidden');
    document.getElementById('listNewMemberResults').classList.add('hidden');
}

function renderGroupMembersList() {
    const listContainer = document.getElementById('listGroupMembers');
    listContainer.innerHTML = '';
    const isCreator = currentGroupData.nguoiTaoId === myId;

    currentGroupData.members.forEach(m => {
        const isMe = m.maNhanVien === myId;
        const isMemberCreator = m.maNhanVien === currentGroupData.nguoiTaoId;
        let badge = isMemberCreator ? '<span class="px-2 py-0.5 bg-yellow-100 text-yellow-700 text-[10px] font-bold rounded-md ml-2">Trưởng nhóm</span>' : '';
        if (isMe) badge += '<span class="px-2 py-0.5 bg-gray-100 text-outline text-[10px] font-bold rounded-md ml-1">Bạn</span>';

        let kickBtn = (isCreator && !isMe) ? `<button onclick="kickMember(${m.maNhanVien})" class="text-outline hover:text-error transition-colors p-1"><span class="material-symbols-outlined text-[18px]">person_remove</span></button>` : '';

        listContainer.insertAdjacentHTML('beforeend', `
            <div class="flex items-center justify-between p-2 hover:bg-gray-50 rounded-xl transition-colors border border-gray-100">
                <div class="flex items-center gap-3">
                    <img src="${m.anhDaiDien || '/images/avatar_default.jpg'}" class="w-8 h-8 rounded-full object-cover"/>
                    <span class="text-sm font-medium">${m.hoTen} ${badge}</span>
                </div>
                ${kickBtn}
            </div>
        `);
    });
}

document.getElementById('fileUploadGroupAvatar').addEventListener('change', async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    const formData = new FormData();
    formData.append('file', file);
    try {
        document.getElementById('settingGroupAvatar').style.opacity = '0.5';
        const res = await fetch(`/api/chat/group/${friendId}/avatar`, { method: 'POST', body: formData });
        if (res.ok) {
            const data = await res.json();
            document.getElementById('settingGroupAvatar').src = data.url;
            chatHeaderAvatar.src = data.url;
            loadRecentConversations();
        }
    } catch (e) { console.error(e); }
    finally { document.getElementById('settingGroupAvatar').style.opacity = '1'; }
});

function showAddMemberBox() { document.getElementById('boxAddMember').classList.toggle('hidden'); }

document.getElementById('txtSearchNewMember').addEventListener('input', (e) => {
    const query = e.target.value.trim();
    const resultsBox = document.getElementById('listNewMemberResults');
    if (!query) { resultsBox.classList.add('hidden'); return; }
    setTimeout(async () => {
        const res = await fetch(`/api/chat/search?q=${encodeURIComponent(query)}&currentUserId=${myId}`);
        if (res.ok) {
            const users = await res.json();
            resultsBox.innerHTML = '';
            resultsBox.classList.remove('hidden');
            users.forEach(u => {
                if (currentGroupData.members.find(m => m.maNhanVien === u.maNhanVien)) return;
                resultsBox.insertAdjacentHTML('beforeend', `<div onclick="selectNewMember(${u.maNhanVien}, '${u.hoTen}')" class="flex items-center gap-2 p-2 hover:bg-primary/10 cursor-pointer rounded-lg"><img src="${u.anhDaiDien || '/images/avatar_default.jpg'}" class="w-6 h-6 rounded-full"/><span class="text-xs font-medium">${u.hoTen}</span></div>`);
            });
        }
    }, 300);
});

function selectNewMember(id, name) {
    selectedNewMemberId = id;
    document.getElementById('txtSearchNewMember').value = name;
    document.getElementById('listNewMemberResults').classList.add('hidden');
}

async function submitAddMember() {
    if (!selectedNewMemberId) return alert("Chọn nhân viên!");
    const res = await fetch(`/api/chat/group/${friendId}/add-members`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify([selectedNewMemberId])
    });
    if (res.ok) { selectedNewMemberId = null; document.getElementById('txtSearchNewMember').value = ''; openGroupSettings(); }
}

async function kickMember(userId) {
    if (confirm("Xóa thành viên?")) {
        const res = await fetch(`/api/chat/group/${friendId}/member/${userId}`, { method: 'DELETE' });
        if (res.ok) openGroupSettings();
    }
}

async function leaveGroup() {
    if (confirm("Rời nhóm?")) {
        const res = await fetch(`/api/chat/group/${friendId}/member/${myId}`, { method: 'DELETE' });
        if (res.ok) { location.reload(); }
    }
}

async function deleteGroup() {
    if (confirm("Xóa nhóm vĩnh viễn?")) {
        const res = await fetch(`/api/chat/group/${friendId}?requesterId=${myId}`, { method: 'DELETE' });
        if (res.ok) { location.reload(); }
    }
}

// ==========================================
// CÁC LOGIC CHAT VÀ TAB (GIỮ NGUYÊN)
// ==========================================
function showToast(title, message, avatar) {
    const toast = document.createElement('div');
    toast.className = "bg-white rounded-2xl shadow-2xl border border-gray-100 p-4 flex items-center gap-3 transform transition-all duration-500 translate-x-[120%] opacity-0 w-80 pointer-events-auto";
    toast.innerHTML = `<img src="${avatar || '/images/avatar_default.jpg'}" class="w-10 h-10 rounded-full object-cover shrink-0"/><div class="flex-1 min-w-0"><h4 class="font-bold text-sm truncate">${title}</h4><p class="text-[13px] text-outline truncate">${message}</p></div>`;
    document.getElementById('toastContainer').appendChild(toast);
    requestAnimationFrame(() => setTimeout(() => toast.classList.remove('translate-x-[120%]', 'opacity-0'), 10));
    setTimeout(() => { toast.classList.add('translate-x-[120%]', 'opacity-0'); setTimeout(() => toast.remove(), 500); }, 4000);
}

function switchTab(tab) {
    currentTab = tab;
    document.getElementById('tab-all').className = `flex-1 py-2 text-[13px] font-bold rounded-2xl transition-all ${tab === 'all' ? 'bg-white shadow-sm text-primary' : 'text-outline hover:text-on-surface'}`;
    document.getElementById('tab-personal').className = `flex-1 py-2 text-[13px] font-bold rounded-2xl transition-all ${tab === 'personal' ? 'bg-white shadow-sm text-primary' : 'text-outline hover:text-on-surface'}`;
    document.getElementById('tab-group').className = `flex-1 py-2 text-[13px] font-bold rounded-2xl transition-all ${tab === 'group' ? 'bg-white shadow-sm text-primary' : 'text-outline hover:text-on-surface'}`;
    renderConversations();
}

async function loadRecentConversations() {
    if (isNaN(myId)) return;
    const res = await fetch(`/api/chat/conversations/${myId}`);
    if (res.ok) { allConversations = await res.json(); renderConversations(); }
}

function renderConversations() {
    userList.innerHTML = '';
    let filtered = allConversations;
    if (currentTab === 'personal') filtered = allConversations.filter(c => !c.isGroup);
    else if (currentTab === 'group') filtered = allConversations.filter(c => c.isGroup);
    if (filtered.length === 0) { userList.innerHTML = '<p class="text-center text-sm text-outline mt-8">Chưa có trò chuyện.</p>'; return; }
    filtered.forEach(c => {
        const avatar = c.friendAvatar || (c.isGroup ? `https://ui-avatars.com/api/?name=${c.friendName}&background=0058bc&color=fff&bold=true` : "/images/avatar_default.jpg");
        const redDot = c.hasUnread ? `<div class="absolute -top-1 -right-1 w-4 h-4 bg-error border-2 border-white rounded-full animate-pulse"></div>` : '';
        userList.insertAdjacentHTML('beforeend', `<div onclick="selectChat(${c.friendId}, '${c.friendName}', '${avatar}', ${c.isGroup})" class="flex items-center gap-4 p-4 rounded-3xl hover:bg-gray-100 cursor-pointer"><div class="relative"><img class="w-14 h-14 rounded-2xl object-cover" src="${avatar}"/>${redDot}</div><div class="flex-1 min-w-0"><div class="flex justify-between items-center mb-0.5"><span class="font-bold truncate">${c.isGroup ? '[Nhóm] ' : ''}${c.friendName}</span><span class="text-[10px] text-outline">${c.lastMessageTime || ''}</span></div><p class="text-sm truncate ${c.hasUnread ? 'font-bold text-on-surface' : 'text-outline'}">${c.lastMessageContent || 'Bắt đầu chat'}</p></div></div>`);
    });
}

window.selectChat = async function (id, name, avatar, isGroup) {
    friendId = id; isCurrentChatGroup = isGroup || false;
    chatHeaderName.textContent = name; chatHeaderAvatar.src = avatar;
    if (isCurrentChatGroup) {
        btnGroupSettings.classList.remove('hidden');
        chatHeaderBox.classList.add('hover:bg-gray-100', 'cursor-pointer');
        chatHeaderBox.onclick = openGroupSettings;
        chatHeaderStatus.innerHTML = '<span class="text-[10px]">Cài đặt nhóm</span>';
    } else {
        btnGroupSettings.classList.add('hidden');
        chatHeaderBox.classList.remove('hover:bg-gray-100', 'cursor-pointer');
        chatHeaderBox.onclick = null;
        chatHeaderStatus.innerHTML = '<span class="w-1.5 h-1.5 bg-tertiary rounded-full animate-pulse"></span> Trực tuyến';
        await fetch(`/api/chat/mark-read/${myId}/${friendId}`, { method: 'POST' });
    }
    await loadHistory(); loadRecentConversations();
};

function appendMessage(senderId, content, time, avatarUrl, senderName = null) {
    const isMe = senderId === myId;
    let displayHtml = content.startsWith("[FILE]") ? `<a href="${content.split('|')[1]}" target="_blank" download class="flex items-center gap-3 p-3 rounded-xl border ${isMe ? 'bg-white/20 text-white' : 'bg-white text-primary'}"><span class="material-symbols-outlined">description</span><div class="flex-1 truncate"><p class="text-sm font-bold truncate">${content.split('|')[0].substring(6)}</p></div></a>` : `<p class="text-sm">${content}</p>`;
    let nameHtml = (senderName && !isMe) ? `<div class="text-[10px] font-bold text-primary mb-1">${senderName}</div>` : '';
    chatCanvas.insertAdjacentHTML('beforeend', isMe ? `<div class="flex flex-col items-end gap-1 self-end max-w-[80%] w-full"><div class="bg-primary text-white p-4 rounded-2xl rounded-br-none shadow-md">${displayHtml}<span class="text-[9px] text-white/70 block mt-2 text-right">${time}</span></div></div>` : `<div class="flex items-end gap-3 max-w-[80%] w-full"><img class="w-8 h-8 rounded-full object-cover shrink-0" src="${avatarUrl || '/images/avatar_default.jpg'}"/><div class="bg-white border border-gray-200 p-4 rounded-2xl rounded-bl-none shadow-sm">${nameHtml}${displayHtml}<span class="text-[9px] text-outline block mt-2 text-right">${time}</span></div></div>`);
    chatCanvas.scrollTop = chatCanvas.scrollHeight;
}

async function loadHistory() {
    if (!friendId) return;
    const url = isCurrentChatGroup ? `/api/chat/group-history/${friendId}` : `/api/chat/history/${myId}/${friendId}`;
    const res = await fetch(url);
    if (res.ok) {
        const msgs = await res.json();
        chatCanvas.innerHTML = '<div class="text-center w-full my-4"><span class="px-3 py-1 bg-gray-200 rounded-full text-[10px] font-bold text-outline uppercase">Lịch sử chat</span></div>';
        msgs.forEach(m => appendMessage(m.nguoiGuiId, m.noiDung, m.thoiGian, m.anhDaiDien, isCurrentChatGroup && m.nguoiGuiId !== myId ? m.hoTen : null));
    }
}

const connection = new signalR.HubConnectionBuilder().withUrl("/chathub").withAutomaticReconnect().build();
connection.on("ReceiveMessage", async (sId, rId, c, t) => {
    if (myId !== sId && myId !== rId) return;
    if (!isCurrentChatGroup && ((sId === myId && rId === friendId) || (sId === friendId && rId === myId))) {
        appendMessage(sId, c, t, sId === friendId ? chatHeaderAvatar.src : null);
        if (sId === friendId) await fetch(`/api/chat/mark-read/${myId}/${friendId}`, { method: 'POST' });
    } else if (sId !== myId) {
        const f = allConversations.find(conv => !conv.isGroup && conv.friendId === sId);
        if (f) showToast(f.friendName, c.startsWith("[FILE]") ? "📎 Tệp tin" : c, f.friendAvatar);
    }
    loadRecentConversations();
});
connection.on("ReceiveGroupMessage", (sId, gId, c, t, sName, sAvatar) => {
    const g = allConversations.find(conv => conv.isGroup && conv.friendId === gId);
    if (!g) return;
    if (isCurrentChatGroup && friendId === gId) appendMessage(sId, c, t, sAvatar, sId !== myId ? sName : null);
    else if (sId !== myId) showToast(g.friendName, `${sName}: ${c.startsWith("[FILE]") ? "📎 Tệp" : c}`, sAvatar);
    loadRecentConversations();
});
connection.start().catch(e => console.error(e));

btnSend.onclick = async () => {
    const msg = txtMessage.value.trim();
    if (msg && friendId) {
        try {
            if (isCurrentChatGroup) await connection.invoke("SendGroupMessage", myId, friendId, msg);
            else await connection.invoke("SendMessage", myId, friendId, msg);
            txtMessage.value = ''; txtMessage.focus();
        } catch (e) { console.error(e); }
    }
};

txtMessage.onkeydown = (e) => { if (e.key === 'Enter') btnSend.click(); };
btnAttach.onclick = () => { if (friendId) fileAttach.click(); };

fileAttach.onchange = async (e) => {
    const f = e.target.files[0];
    if (!f || f.size > 52428800) return;
    uploadOverlay.classList.remove('hidden');
    const fd = new FormData(); fd.append("file", f); fd.append("senderId", myId);
    try {
        const res = await fetch('/api/chat/upload', { method: 'POST', body: fd });
        if (res.ok) {
            const data = await res.json();
            const fMsg = `[FILE]${data.fileName}|${data.url}`;
            if (isCurrentChatGroup) await connection.invoke("SendGroupMessage", myId, friendId, fMsg);
            else await connection.invoke("SendMessage", myId, friendId, fMsg);
        }
    } catch (e) { console.error(e); }
    finally { uploadOverlay.classList.add('hidden'); fileAttach.value = ''; }
};

const modalCreateGroup = document.getElementById('modalCreateGroup');
const btnSubmitGroup = document.getElementById('btnSubmitGroup');
const txtGroupName = document.getElementById('txtGroupName');
let selectedMembers = [];

document.getElementById('btnOpenCreateGroup').onclick = () => modalCreateGroup.classList.remove('hidden');
document.getElementById('btnCloseModal').onclick = () => { modalCreateGroup.classList.add('hidden'); selectedMembers = []; renderSelectedMembers(); };

document.getElementById('txtSearchMember').oninput = (e) => {
    const q = e.target.value.trim();
    if (!q) return;
    setTimeout(async () => {
        const res = await fetch(`/api/chat/search?q=${encodeURIComponent(q)}&currentUserId=${myId}`);
        if (res.ok) {
            const users = await res.json();
            const box = document.getElementById('searchMemberResults'); box.innerHTML = '';
            users.forEach(u => {
                if (selectedMembers.find(m => m.id === u.maNhanVien)) return;
                const d = document.createElement('div'); d.className = "flex items-center justify-between p-2 hover:bg-gray-100 cursor-pointer rounded-xl";
                d.innerHTML = `<div class="flex items-center gap-3"><img src="${u.anhDaiDien || '/images/avatar_default.jpg'}" class="w-8 h-8 rounded-full"/><span class="text-sm">${u.hoTen}</span></div>`;
                d.onclick = () => { selectedMembers.push({ id: u.maNhanVien, name: u.hoTen }); renderSelectedMembers(); };
                box.appendChild(d);
            });
        }
    }, 300);
};

function renderSelectedMembers() {
    const box = document.getElementById('selectedMembersContainer'); box.innerHTML = '';
    selectedMembers.forEach(m => { box.insertAdjacentHTML('beforeend', `<div class="bg-primary/10 text-primary px-3 py-1 rounded-full text-xs flex items-center gap-1">${m.name}<span onclick="removeMember(${m.id})" class="material-symbols-outlined text-xs cursor-pointer">cancel</span></div>`); });
}
window.removeMember = (id) => { selectedMembers = selectedMembers.filter(m => m.id !== id); renderSelectedMembers(); };

btnSubmitGroup.onclick = async () => {
    if (!txtGroupName.value.trim() || !selectedMembers.length) return;
    const res = await fetch('/api/chat/create-group', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ groupName: txtGroupName.value.trim(), creatorId: myId, memberIds: selectedMembers.map(m => m.id) }) });
    if (res.ok) { location.reload(); }
};

txtSearchUser.oninput = (e) => {
    const q = e.target.value.trim();
    if (!q) { loadRecentConversations(); return; }
    setTimeout(async () => {
        const res = await fetch(`/api/chat/search?q=${encodeURIComponent(q)}&currentUserId=${myId}`);
        if (res.ok) {
            const users = await res.json();
            allConversations = users.map(u => ({ friendId: u.maNhanVien, friendName: u.hoTen, friendAvatar: u.anhDaiDien, isGroup: false }));
            renderConversations();
        }
    }, 500);
};

// Khởi chạy hệ thống sau khi gán hết các hàm xử lý
loadRecentConversations();