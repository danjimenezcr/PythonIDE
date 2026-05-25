// login.js — handles the login form submission and JWT storage
// API is defined in config.js, which must be loaded before this script

document.getElementById('login-form').addEventListener('submit', async function (e) {
    e.preventDefault();

    const email    = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    const errorMsg = document.getElementById('error-msg');
    const btn      = document.getElementById('submit-btn');

    // Reset error state and disable button while request is in flight
    errorMsg.style.display = 'none';
    btn.disabled           = true;
    btn.textContent        = 'Ingresando...';

    try {
        const res  = await fetch(`${API}/auth/login`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ email, password }),
        });

        const data = await res.json();

        if (!data.success) {
            errorMsg.textContent   = data.message;
            errorMsg.style.display = 'block';
            btn.disabled           = false;
            btn.textContent        = 'Iniciar sesión';
            return;
        }

        // Persist token and user so other pages can read them
        localStorage.setItem('token', data.data.token);
        localStorage.setItem('user',  JSON.stringify(data.data.user));

        window.location.href = '/courses.html';

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión. Intenta de nuevo.';
        errorMsg.style.display = 'block';
        btn.disabled           = false;
        btn.textContent        = 'Iniciar sesión';
    }
});
