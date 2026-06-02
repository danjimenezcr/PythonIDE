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

// ─── Load course detail ──────────────────────────────────────────────────────

async function loadCourse() {
    try {
        const res  = await fetch(`${API}/courses`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (!data.success) return;

        const course = data.data.find(c => c.id === courseId);
        if (!course) {
            document.getElementById('course-title').textContent = 'Course not found';
            return;
        }

        document.title                                              = `${course.name} — PyStudio`;
        document.getElementById('course-title').textContent        = course.name;
        document.getElementById('course-name').value               = course.name;
        document.getElementById('course-description').value        = course.description || '';
        document.getElementById('course-access-code').textContent  = course.access_code;
        document.getElementById('course-teacher').textContent      = course.teacher_name || '—';

    } catch (err) {
        document.getElementById('course-title').textContent = 'Error loading course';
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
            list.innerHTML = '<p class="empty-state">No activities yet.</p>';
            return;
        }

        data.data.forEach(activity => {
            const row = document.createElement('div');
            row.className = 'activity-row';
            row.innerHTML = `
                <div class="activity-row-info">
                    <h4>${activity.title}</h4>
                    <span class="deadline">Due: ${new Date(activity.deadline).toLocaleDateString('en-US', { dateStyle: 'medium' })}</span>
                </div>
                <div class="activity-row-actions">
                    ${isTeacher ? `
                        <button class="btn-icon danger" data-delete="${activity.id}" title="Delete">✕</button>
                    ` : ''}
                    <button class="btn-icon" data-goto="${activity.id}" title="Open">→</button>
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
                    if (!confirm(`Delete "${activity.title}"?`)) return;
                    await deleteActivity(activity.id);
                });
            }

            list.appendChild(row);
        });

    } catch (err) {
        document.getElementById('activities-list').innerHTML =
            '<p class="error-msg">Error loading activities.</p>';
    }
}

// ─── Save course (teachers only) ─────────────────────────────────────────────

document.getElementById('save-course-btn')?.addEventListener('click', async function () {
    const name        = document.getElementById('course-name').value.trim();
    const description = document.getElementById('course-description').value.trim();
    const errorMsg    = document.getElementById('course-error');
    const btn         = this;

    if (!name) {
        errorMsg.textContent   = 'Course name is required.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Saving...';

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
        errorMsg.textContent   = 'Connection error.';
        errorMsg.style.display = 'block';
    } finally {
        btn.disabled    = false;
        btn.textContent = 'Save';
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
        errorMsg.textContent   = 'Title is required.';
        errorMsg.style.display = 'block';
        return;
    }

    if (!deadline) {
        errorMsg.textContent   = 'Deadline is required.';
        errorMsg.style.display = 'block';
        return;
    }

    errorMsg.style.display = 'none';
    btn.disabled            = true;
    btn.textContent         = 'Creating...';

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
            errorMsg.textContent   = data.message || 'Error creating activity.';
            errorMsg.style.display = 'block';
        } else {
            document.getElementById('activity-title').value       = '';
            document.getElementById('activity-description').value = '';
            document.getElementById('activity-deadline').value    = '';
            document.getElementById('add-activity-form').style.display = 'none';
            loadActivities();
        }

    } catch (err) {
        errorMsg.textContent   = 'Connection error.';
        errorMsg.style.display = 'block';
    } finally {
        btn.disabled    = false;
        btn.textContent = 'Create Activity';
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
        alert('Error deleting activity.');
    }
}
