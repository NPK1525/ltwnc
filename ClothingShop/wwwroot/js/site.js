// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Smart Navbar: Ẩn khi cuộn xuống, hiện khi cuộn lên
document.addEventListener('DOMContentLoaded', function () {
    const navbar = document.querySelector('.navbar.sticky-top');
    if (!navbar) return;

    let lastScrollTop = 0;
    const delta = 5; // Khoảng cuộn tối thiểu để kích hoạt
    const navbarHeight = navbar.offsetHeight;

    window.addEventListener('scroll', function () {
        let st = window.pageYOffset || document.documentElement.scrollTop;

        // Tránh giật khi scroll ở mép trên hoặc mép dưới trang
        if (Math.abs(lastScrollTop - st) <= delta) return;

        // Cuộn xuống và đã vượt qua chiều cao navbar
        if (st > lastScrollTop && st > navbarHeight) {
            navbar.classList.add('nav-up');
        } else {
            // Cuộn lên
            if (st + window.innerHeight < document.documentElement.scrollHeight) {
                navbar.classList.remove('nav-up');
            }
        }

        // Nếu ở sát đầu trang thì luôn luôn hiển thị
        if (st <= 0) {
            navbar.classList.remove('nav-up');
        }

        lastScrollTop = st;
    });
});
