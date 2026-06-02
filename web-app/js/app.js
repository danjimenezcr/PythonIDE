// app.js — shared auth guard, user display, logout, and active nav link
// Must be loaded after config.js on every app page.

const _token = localStorage.getItem('token');
const _user  = JSON.parse(localStorage.getItem('user') || 'null');

// Redirect to login if no session
if (!_token || !_user) {
    window.location.href = '/index.html';
}

// Populate sidebar user info
document.addEventListener('DOMContentLoaded', () => {
    const nameEl = document.getElementById('sidebar-user-name');
    const roleEl = document.getElementById('sidebar-user-role');
    if (nameEl) nameEl.textContent = _user.full_name;
    if (roleEl) roleEl.textContent = _user.role === 'teacher' ? 'Teacher' : 'Student';

    // Highlight the nav link that matches the current page
    const current = window.location.pathname.split('/').pop();
    document.querySelectorAll('.sidebar-link[data-page]').forEach(link => {
        if (link.dataset.page === current) {
            link.classList.add('active');
        }
    });

    // Logout button
    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            window.location.href = '/index.html';
        });
    }
});
