// signup.js — handles teacher registration form submission
// API is defined in config.js, which must be loaded before this script

document.getElementById('signup-form').addEventListener('submit', async function (e) {
    e.preventDefault();

    const firstName       = document.getElementById('first-name').value.trim();
    const lastName        = document.getElementById('last-name').value.trim();
    const email           = document.getElementById('email').value.trim();
    const password        = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirm-password').value;
    const errorMsg        = document.getElementById('error-msg');
    const btn             = document.getElementById('submit-btn');
    const passwordCaption = document.getElementById('password-caption');

    // Client-side validation: passwords must match before hitting the API
    if (password !== confirmPassword) {
        passwordCaption.textContent   = 'Las contraseñas no coinciden.';
        //passwordCaption.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled           = true;
    btn.textContent        = 'Registrando...';
    
    console.log('Enviando datos de registro:', { firstName, lastName, email });

    try {
        const res = await fetch(`${API}/auth/register`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({
                full_name: `${firstName} ${lastName}`,
                email,
                password,
                role: 'teacher', // Web registration is for teachers only
            }),
        });

        const data = await res.json();

        if (!data.success) {
            errorMsg.textContent   = data.message;
            errorMsg.style.display = 'block';
            btn.disabled           = false;
            btn.textContent        = 'Registrarme';
            return;
        }

        // Registration successful — redirect to login
        window.location.href = '/index.html';

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión. Intenta de nuevo.';
        errorMsg.style.display = 'block';
        btn.disabled           = false;
        btn.textContent        = 'Registrarme';
    }
});
