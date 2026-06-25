// courses.js — loads and renders the course grid on courses.html
// Requires config.js and app.js to be loaded first.

loadCourses();

async function loadCourses() {
    try {
        const res  = await fetch(`${API}/courses`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        const list = document.getElementById('courses-list');
        list.innerHTML = '';

        if (!data.success || data.data.length === 0) {
            list.innerHTML = '<p class="empty-state">Aún no hay cursos. Crea uno para empezar.</p>';
            return;
        }

        data.data.forEach(course => {
            const card = document.createElement('div');
            card.className = 'course-card';
            card.innerHTML = `
                <h3>${course.name}</h3>
                <p>${course.description || 'Sin descripción'}</p>
                <span class="access-code">Código: ${course.access_code}</span>
            `;
            // Navigate to the course detail page
            card.addEventListener('click', () => {
                window.location.href = `course.html?id=${course.id}`;
            });
            list.appendChild(card);
        });

    } catch (err) {
        document.getElementById('courses-list').innerHTML =
            '<p class="error-msg">Error al cargar los cursos.</p>';
    }
}

// Toggle the create-course form
document.getElementById('new-course-btn').addEventListener('click', function () {
    const form = document.getElementById('create-course-form');
    form.style.display = form.style.display === 'none' ? 'block' : 'none';
});

document.getElementById('cancel-course-btn').addEventListener('click', function () {
    document.getElementById('create-course-form').style.display = 'none';
});

document.getElementById('create-course-submit').addEventListener('click', async function () {
    const name        = document.getElementById('course-name').value.trim();
    const description = document.getElementById('course-description').value.trim();
    const errorMsg    = document.getElementById('create-error');
    const btn         = document.getElementById('create-course-submit');

    if (!name) {
        errorMsg.textContent   = 'El nombre del curso es obligatorio.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Creando...';

    try {
        const res  = await fetch(`${API}/courses`, {
            method:  'POST',
            headers: {
                'Content-Type':  'application/json',
                'Authorization': `Bearer ${_token}`,
            },
            body: JSON.stringify({ name, description }),
        });
        const data = await res.json();

        if (!data.success) {
            errorMsg.textContent   = data.message;
            errorMsg.style.display = 'block';
            btn.disabled           = false;
            btn.textContent        = 'Crear Curso';
            return;
        }

        // Reset form, hide it, refresh the list
        document.getElementById('course-name').value        = '';
        document.getElementById('course-description').value = '';
        document.getElementById('create-course-form').style.display = 'none';
        btn.disabled    = false;
        btn.textContent = 'Crear Curso';
        loadCourses();

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión. Intenta de nuevo.';
        errorMsg.style.display = 'block';
        btn.disabled           = false;
        btn.textContent        = 'Crear Curso';
    }
});
