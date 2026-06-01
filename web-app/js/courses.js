// courses.js — loads and renders courses and activities for a teacher
// API is defined in config.js, which must be loaded before this script
const token = localStorage.getItem('token');
const user  = JSON.parse(localStorage.getItem('user') || 'null');

// Tracks the currently selected course so the add-assignment form knows which course to post to
let selectedCourseId = null;

// Redirect to login if no session exists
if (!token || !user) {
    window.location.href = '/index.html';
}

// Show the teacher's name in the header
document.getElementById('user-name').textContent = user.full_name;

// Load courses when the page is ready
loadCourses();

// ─── Courses ────────────────────────────────────────────────────────────────

async function loadCourses() {
    try {
        const res  = await fetch(`${API}/courses`, {
            headers: { 'Authorization': `Bearer ${token}` },
        });
        const data = await res.json();

        const list = document.getElementById('courses-list');
        list.innerHTML = '';

        if (!data.success || data.data.length === 0) {
            list.innerHTML = '<p class="empty-state">No tienes cursos aún. Crea uno para comenzar.</p>';
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
            // Clicking a card loads its activities and tracks the selected course
            card.addEventListener('click', () => {
                selectedCourseId = course.id;
                loadActivities(course.id, course.name);
            });
            list.appendChild(card);
        });

    } catch (err) {
        document.getElementById('courses-list').innerHTML =
            '<p class="error-msg">Error al cargar los cursos.</p>';
    }
}

// ─── Activities ─────────────────────────────────────────────────────────────

async function loadActivities(courseId, courseName) {
    try {
        const res  = await fetch(`${API}/courses/${courseId}/activities`, {
            headers: { 'Authorization': `Bearer ${token}` },
        });
        const data = await res.json();

        const section = document.getElementById('activities-section');
        const title   = document.getElementById('activities-title');
        const list    = document.getElementById('activities-list');

        title.textContent     = `Actividades — ${courseName}`;
        list.innerHTML        = '';
        section.style.display = 'block';

        // Hide the add-assignment form whenever switching courses
        document.getElementById('add-assignment-form').style.display = 'none';

        // Scroll to activities section smoothly
        section.scrollIntoView({ behavior: 'smooth' });

        if (!data.success || data.data.length === 0) {
            list.innerHTML = '<p class="empty-state">Este curso no tiene actividades aún.</p>';
            return;
        }

        data.data.forEach(activity => {
            const item = document.createElement('div');
            item.className = 'activity-item';
            item.innerHTML = `
                <h4>${activity.title}</h4>
                <p>${activity.description || 'Sin descripción'}</p>
                <span class="deadline">Entrega: ${new Date(activity.deadline).toLocaleDateString('es-CR')}</span>
            `;
            list.appendChild(item);
        });

    } catch (err) {
        document.getElementById('activities-list').innerHTML =
            '<p class="error-msg">Error al cargar las actividades.</p>';
    }
}

// ─── Create course ───────────────────────────────────────────────────────────

// Toggle the create-course form on button click
document.getElementById('new-course-btn').addEventListener('click', function () {
    const form = document.getElementById('create-course-form');
    form.style.display = form.style.display === 'none' ? 'block' : 'none';
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
                'Authorization': `Bearer ${token}`,
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

        // Reset form, hide it, and refresh the list
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

// ─── Cancel create course ────────────────────────────────────────────────────

document.getElementById('cancel-course-btn').addEventListener('click', function () {
    document.getElementById('create-course-form').style.display = 'none';
});

// ─── Add assignment ──────────────────────────────────────────────────────────

document.getElementById('add-assignment-btn').addEventListener('click', function () {
    const form = document.getElementById('add-assignment-form');
    form.style.display = form.style.display === 'none' ? 'block' : 'none';
});

document.getElementById('cancel-assignment-btn').addEventListener('click', function () {
    document.getElementById('add-assignment-form').style.display = 'none';
});

document.getElementById('submit-assignment-btn').addEventListener('click', async function () {
    const title       = document.getElementById('assignment-title').value.trim();
    const description = document.getElementById('assignment-description').value.trim();
    const deadline    = document.getElementById('assignment-deadline').value;
    const errorMsg    = document.getElementById('assignment-error');
    const btn         = document.getElementById('submit-assignment-btn');

    if (!title) {
        errorMsg.textContent   = 'El título de la tarea es obligatorio.';
        errorMsg.style.display = 'block';
        return;
    }

    if (!deadline) {
        errorMsg.textContent   = 'La fecha límite es obligatoria.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Creando...';

    try {
        const res  = await fetch(`${API}/courses/${selectedCourseId}/activities`, {
            method:  'POST',
            headers: {
                'Content-Type':  'application/json',
                'Authorization': `Bearer ${token}`,
            },
            body: JSON.stringify({ title, description, deadline }),
        });
        const data = await res.json();

        if (!data.success) {
            errorMsg.textContent   = data.message || 'Error al crear la tarea.';
            errorMsg.style.display = 'block';
            btn.disabled           = false;
            btn.textContent        = 'Crear Tarea';
            return;
        }

        // Reset form, hide it, and reload activities
        document.getElementById('assignment-title').value       = '';
        document.getElementById('assignment-description').value = '';
        document.getElementById('assignment-deadline').value    = '';
        document.getElementById('add-assignment-form').style.display = 'none';
        btn.disabled    = false;
        btn.textContent = 'Crear Tarea';

        // Reload the activities list for the current course
        const courseName = document.getElementById('activities-title').textContent.replace('Actividades — ', '');
        loadActivities(selectedCourseId, courseName);

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión. Intenta de nuevo.';
        errorMsg.style.display = 'block';
        btn.disabled           = false;
        btn.textContent        = 'Crear Tarea';
    }
});

// ─── Logout ──────────────────────────────────────────────────────────────────

document.getElementById('logout-btn').addEventListener('click', function () {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/index.html';
});
