// activity.js — loads and manages a single activity detail page
// Requires config.js and app.js to be loaded first.

const params     = new URLSearchParams(window.location.search);
const activityId = parseInt(params.get('id'), 10);

if (!activityId) {
    window.location.href = 'activities.html';
}

const isTeacher = _user.role === 'teacher';

// Show role-specific UI
if (isTeacher) {
    const actions = document.getElementById('teacher-actions');
    actions.style.display = 'flex';
    document.getElementById('activity-title').removeAttribute('readonly');
    document.getElementById('activity-deadline').removeAttribute('readonly');
    document.getElementById('activity-description').removeAttribute('readonly');
} else {
    document.getElementById('student-section').style.display = 'block';
}

loadActivity();

// ─── Load activity ────────────────────────────────────────────────────────────

async function loadActivity() {
    try {
        const res  = await fetch(`${API}/activities/${activityId}`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (!data.success) {
            document.getElementById('activity-title').value = 'Actividad no encontrada';
            return;
        }

        const activity = data.data;

        document.title = `${activity.title} — PyStudio`;
        document.getElementById('activity-title').value       = activity.title;
        document.getElementById('activity-description').value = activity.description || '';

        // Format deadline for datetime-local input (YYYY-MM-DDTHH:MM)
        if (activity.deadline) {
            const d = new Date(activity.deadline);
            const pad = n => String(n).padStart(2, '0');
            const local = `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
            document.getElementById('activity-deadline').value = local;
        }

        // Update back link to go to the course this activity belongs to
        if (activity.course_id) {
            document.getElementById('back-link').href = `course.html?id=${activity.course_id}`;
            document.getElementById('back-link').textContent = '← Curso';
        }

    } catch (err) {
        document.getElementById('activity-error').textContent   = 'Error cargando la actividad.';
        document.getElementById('activity-error').style.display = 'block';
    }
}

// ─── Save activity (teachers only) ───────────────────────────────────────────

document.getElementById('save-activity-btn')?.addEventListener('click', async function () {
    const title       = document.getElementById('activity-title').value.trim();
    const description = document.getElementById('activity-description').value.trim();
    const deadline    = document.getElementById('activity-deadline').value;
    const errorMsg    = document.getElementById('activity-error');
    const btn         = this;

    if (!title) {
        errorMsg.textContent   = 'El título es obligatorio.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Guardando...';

    try {
        const res  = await fetch(`${API}/activities/${activityId}`, {
            method:  'PUT',
            headers: {
                'Content-Type':  'application/json',
                'Authorization': `Bearer ${_token}`,
            },
            body: JSON.stringify({ title, description, deadline }),
        });
        const data = await res.json();

        if (!data.success) {
            errorMsg.textContent   = data.message;
            errorMsg.style.display = 'block';
        } else {
            document.title = `${title} — PyStudio`;
        }

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión.';
        errorMsg.style.display = 'block';
    } finally {
        btn.disabled    = false;
        btn.textContent = 'Guardar';
    }
});

// ─── Delete activity (teachers only) ─────────────────────────────────────────

document.getElementById('delete-activity-btn')?.addEventListener('click', async function () {
    const title = document.getElementById('activity-title').value || 'this activity';
    if (!confirm(`Delete "${title}"?`)) return;

    try {
        const res  = await fetch(`${API}/activities/${activityId}`, {
            method:  'DELETE',
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (data.success) {
            window.location.href = 'activities.html';
        }
    } catch (err) {
        document.getElementById('activity-error').textContent   = 'Error eliminando la actividad.';
        document.getElementById('activity-error').style.display = 'block';
    }
});

// ─── File upload (students only — placeholder) ────────────────────────────────

document.getElementById('upload-btn')?.addEventListener('click', function () {
    // File upload is not yet implemented in the backend.
    alert('Subida de archivos disponible pronto.');
});
