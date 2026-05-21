/**
 * Hàm điền dữ liệu vào biểu mẫu in và kích hoạt lệnh in phiếu lương của trình duyệt
 */
function inPhieuLuong(ten, thang, nam, luongCB, tangCa, thuong, khauTru, bhxh, bhyt, bhtn, tongLuong) {

    // Hàm định dạng số theo chuẩn tiền tệ Việt Nam (Vd: 10,000,000)
    const formatVN = (num) => parseFloat(num).toLocaleString('vi-VN');

    // Gán các thông tin tổng quan
    document.getElementById('pTenNhanVien').innerText = ten;
    document.getElementById('pKyLuong').innerText = `Kỳ lương: Tháng ${thang}/${nam}`;

    // Gán dữ liệu phần thu nhập (Cộng)
    document.getElementById('pLuongCoBan').innerText = formatVN(luongCB);
    document.getElementById('pTangCa').innerText = formatVN(tangCa);
    document.getElementById('pThuong').innerText = formatVN(thuong);

    // Gán dữ liệu phần khấu trừ & Bảo hiểm (Trừ)
    document.getElementById('pBHXH').innerText = formatVN(bhxh);
    document.getElementById('pBHYT').innerText = formatVN(bhyt);
    document.getElementById('pBHTN').innerText = formatVN(bhtn);
    document.getElementById('pKhauTru').innerText = formatVN(khauTru);

    // Gán dữ liệu thực lãnh cuối cùng
    document.getElementById('pTongLuong').innerText = formatVN(tongLuong);

    // Hiển thị khu vực in ấn để chuẩn bị gửi lệnh sang máy in
    const printArea = document.getElementById('printArea');
    if (printArea) {
        printArea.classList.remove('hidden');
        printArea.classList.add('block');

        // Kích hoạt giao diện in mặc định của hệ điều hành/trình duyệt
        window.print();

        // Ẩn lại khu vực in ấn sau khi quá trình in kết thúc/bị hủy để trả lại giao diện web công tác
        printArea.classList.add('hidden');
        printArea.classList.remove('block');
    }
}