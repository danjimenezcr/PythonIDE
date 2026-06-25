// course.js — loads and manages a single course detail page
// Requires config.js and app.js to be loaded first.

const params   = new URLSearchParams(window.location.search);
const courseId = parseInt(params.get('id'), 10);

if (!courseId) {
    window.location.href = 'courses.html';
}

const isTeacher = _user.role === 'teacher';

// Show teacher-only UI elements once we know the role
if (isTeacher) {
    document.getElementById('add-activity-btn').style.display = 'block';
    document.getElementById('teacher-actions').style.display  = 'flex';
    // Make course fields editable for teachers
    document.getElementById('course-name').removeAttribute('readonly');
    document.getElementById('course-description').removeAttribute('readonly');
}

loadCourse();
loadActivities();
loadMembers();
loadGroups();

// ─── Load course detail ──────────────────────────────────────────────────────

async function loadCourse() {
    try {
        const res  = await fetch(`${API}/courses/${courseId}`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (!data.success) {
            document.getElementById('course-title').textContent = data.message || 'Curso no encontrado';
            return;
        }

        const course = data.data;

        document.title                                             = `${course.name} — PyStudio`;
        document.getElementById('course-title').textContent       = course.name;
        document.getElementById('course-name').value              = course.name;
        document.getElementById('course-description').value       = course.description || '';
        document.getElementById('course-access-code').textContent = course.access_code;
        document.getElementById('course-teacher').textContent     = course.teacher_name || '—';

    } catch (err) {
        document.getElementById('course-title').textContent = 'Error al cargar el curso';
    }
}

// ─── Load members (RF-13) ────────────────────────────────────────────────────

async function loadMembers() {
    const countEl = document.getElementById('course-members');
    const listEl  = document.getElementById('members-list');

    try {
        const res  = await fetch(`${API}/courses/${courseId}/members`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (!data.success) {
            if (countEl) countEl.textContent = '—';
            return;
        }

        const members = data.data;
        if (countEl) countEl.textContent = members.length;

        // Member list table is teacher-only
        if (!listEl) return;

        listEl.innerHTML = '';
        if (members.length === 0) {
            listEl.innerHTML = '<tr><td colspan="3" class="empty-state">Aún no hay estudiantes inscritos.</td></tr>';
            return;
        }

        members.forEach(member => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${member.full_name}</td>
                <td>${member.email}</td>
                <td><button class="btn-icon danger" data-remove="${member.id}" title="Remover">✕</button></td>
            `;
            row.querySelector('[data-remove]').addEventListener('click', async () => {
                if (!confirm(`¿Remover a ${member.full_name} de este curso?`)) return;
                await removeMember(member.id);
            });
            listEl.appendChild(row);
        });

    } catch (err) {
        if (countEl) countEl.textContent = '—';
    }
}

async function removeMember(studentId) {
    try {
        const res  = await fetch(`${API}/courses/${courseId}/members/${studentId}`, {
            method:  'DELETE',
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (data.success) {
            loadMembers();
        } else {
            alert(data.message || 'Error al remover al estudiante.');
        }
    } catch (err) {
        alert('Error de conexión.');
    }
}

// ─── Load activities ─────────────────────────────────────────────────────────

async function loadActivities() {
    try {
        const res  = await fetch(`${API}/courses/${courseId}/activities`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        const list = document.getElementById('activities-list');
        list.innerHTML = '';

        if (!data.success || data.data.length === 0) {
            list.innerHTML = '<p class="empty-state">Aún no hay actividades.</p>';
            return;
        }

        data.data.forEach(activity => {
            const row = document.createElement('div');
            row.className = 'activity-row';
            row.innerHTML = `
                <div class="activity-row-info">
                    <h4>${activity.title} <span class="badge badge-valid">${activity.submission_count} entrega${activity.submission_count == 1 ? '' : 's'}</span></h4>
                    <span class="deadline">Vence: ${new Date(activity.deadline).toLocaleDateString('es-ES', { dateStyle: 'medium' })}</span>
                </div>
                <div class="activity-row-actions">
                    ${isTeacher ? `
                        <button class="btn-icon danger" data-delete="${activity.id}" title="Eliminar">✕</button>
                    ` : ''}
                    <button class="btn-icon" data-goto="${activity.id}" title="Abrir">→</button>
                </div>
            `;

            // Navigate to activity detail page
            row.querySelector('[data-goto]').addEventListener('click', (e) => {
                e.stopPropagation();
                window.location.href = `activity.html?id=${activity.id}`;
            });

            // Clicking the row itself also navigates
            row.addEventListener('click', () => {
                window.location.href = `activity.html?id=${activity.id}`;
            });

            // Delete (teachers only)
            if (isTeacher) {
                row.querySelector('[data-delete]').addEventListener('click', async (e) => {
                    e.stopPropagation();
                    if (!confirm(`¿Eliminar "${activity.title}"?`)) return;
                    await deleteActivity(activity.id);
                });
            }

            list.appendChild(row);
        });

    } catch (err) {
        document.getElementById('activities-list').innerHTML =
            '<p class="error-msg">Error al cargar las actividades.</p>';
    }
}

// ─── Load groups (RF-11) ─────────────────────────────────────────────────────
// Groups are created/joined by students from the desktop client; the web app
// only shows the resulting groups and lets the teacher rename one.

async function loadGroups() {
    const container = document.getElementById('groups-list');

    try {
        const res  = await fetch(`${API}/courses/${courseId}/groupslist`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        container.innerHTML = '';

        if (!data.success) {
            container.innerHTML = `<p class="empty-state">${data.message || 'Error al cargar los grupos depues de la autorizacion.'}</p>`;
            return;
        }

        if (data.data.length === 0) {
            container.innerHTML = '<p class="empty-state">Aún no se han formado grupos.</p>';
            return;
        }

        data.data.forEach(group => {
            const memberNames = group.members.length
                ? group.members.map(m => m.full_name).join(', ')
                : 'Sin miembros aún';

            const row = document.createElement('div');
            row.className = 'activity-row';
            row.innerHTML = `
                <div class="activity-row-info">
                    <h4 data-group-name>${group.name}</h4>
                    <span class="deadline">Código de invitación: ${group.invite_code} — ${memberNames}</span>
                </div>
                <div class="activity-row-actions">
                    <button class="btn-icon" data-rename="${group.id}" title="Renombrar">✎</button>
                </div>
            `;
            row.querySelector('[data-rename]').addEventListener('click', () => renameGroup(group));
            container.appendChild(row);
        });

    } catch (err) {
        container.innerHTML = '<p class="empty-state">Error al cargar los grupos desde auth.</p>';
    }
}

async function renameGroup(group) {
    const newName = prompt('Nuevo nombre del grupo:', group.name);
    if (!newName || !newName.trim() || newName.trim() === group.name) return;

    try {
        const res  = await fetch(`${API}/groups/${group.id}`, {
            method:  'PUT',
            headers: {
                'Content-Type':  'application/json',
                'Authorization': `Bearer ${_token}`,
            },
            body: JSON.stringify({ name: newName.trim() }),
        });
        const data = await res.json();

        if (data.success) {
            loadGroups();
        } else {
            alert(data.message || 'Error al renombrar el grupo.');
        }
    } catch (err) {
        alert('Error de conexión.');
    }
}

// ─── Save course (teachers only) ─────────────────────────────────────────────

document.getElementById('save-course-btn')?.addEventListener('click', async function () {
    const name        = document.getElementById('course-name').value.trim();
    const description = document.getElementById('course-description').value.trim();
    const errorMsg    = document.getElementById('course-error');
    const btn         = this;

    if (!name) {
        errorMsg.textContent   = 'El nombre del curso es obligatorio.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Guardando...';

    try {
        const res  = await fetch(`${API}/courses/${courseId}`, {
            method:  'PUT',
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
        } else {
            document.getElementById('course-title').textContent = name;
        }

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión.';
        errorMsg.style.display = 'block';
    } finally {
        btn.disabled    = false;
        btn.textContent = 'Guardar';
    }
});

// ─── Add activity form (teachers only) ───────────────────────────────────────

document.getElementById('add-activity-btn')?.addEventListener('click', function () {
    const form = document.getElementById('add-activity-form');
    form.style.display = form.style.display === 'none' ? 'block' : 'none';
});

document.getElementById('cancel-activity-btn')?.addEventListener('click', function () {
    document.getElementById('add-activity-form').style.display = 'none';
});

document.getElementById('submit-activity-btn')?.addEventListener('click', async function () {
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

    if (!deadline) {
        errorMsg.textContent   = 'La fecha límite es obligatoria.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Creando...';

    try {
        const res  = await fetch(`${API}/activities`, {
            method:  'POST',
            headers: {
                'Content-Type':  'application/json',
                'Authorization': `Bearer ${_token}`,
            },
            body: JSON.stringify({ course_id: courseId, title, description, deadline }),
        });
        const data = await res.json();

        if (!data.success) {
            errorMsg.textContent   = data.message || 'Error al crear la actividad.';
            errorMsg.style.display = 'block';
        } else {
            document.getElementById('activity-title').value       = '';
            document.getElementById('activity-description').value = '';
            document.getElementById('activity-deadline').value    = '';
            document.getElementById('add-activity-form').style.display = 'none';
            loadActivities();
        }

    } catch (err) {
        errorMsg.textContent   = 'Error de conexión.';
        errorMsg.style.display = 'block';
    } finally {
        btn.disabled    = false;
        btn.textContent = 'Crear Actividad';
    }
});

// ─── Delete activity ──────────────────────────────────────────────────────────

async function deleteActivity(activityId) {
    try {
        const res  = await fetch(`${API}/activities/${activityId}`, {
            method:  'DELETE',
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (data.success) {
            loadActivities();
        }
    } catch (err) {
        alert('Error al eliminar la actividad.');
    }
}
