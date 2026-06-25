// submissions.js — teacher view of all submissions for an activity (RF-10, RF-19)
// Requires config.js and app.js to be loaded first.

const params     = new URLSearchParams(window.location.search);
const activityId = parseInt(params.get('activityId'), 10);

if (!activityId) {
    window.location.href = 'activities.html';
}

loadActivityTitle();
loadSubmissions();

async function loadActivityTitle() {
    try {
        const res  = await fetch(`${API}/activities/${activityId}`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (data.success) {
            document.title = `Entregas — ${data.data.title} — PyStudio`;
            document.getElementById('activity-title').textContent = `Entregas: ${data.data.title}`;
            document.getElementById('back-link').href = `activity.html?id=${activityId}`;
        }
    } catch (err) {
        // Title is decorative; ignore failures here.
    }
}

async function loadSubmissions() {
    const list    = document.getElementById('submissions-list');
    const errorEl = document.getElementById('submissions-error');

    try {
        const res  = await fetch(`${API}/activities/${activityId}/submissions`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (!data.success) {
            errorEl.textContent   = data.message || 'Error al cargar las entregas.';
            errorEl.style.display = 'block';
            list.innerHTML        = '';
            return;
        }

        list.innerHTML = '';

        if (data.data.length === 0) {
            list.innerHTML = '<tr><td colspan="4" class="empty-state">Aún no hay entregas.</td></tr>';
            return;
        }

        data.data.forEach(sub => renderRow(list, sub));

    } catch (err) {
        errorEl.textContent   = 'Error de conexión.';
        errorEl.style.display = 'block';
    }
}

function renderRow(list, sub) {
    const submitterName = sub.is_group_submission
        ? `Grupo: ${sub.group_name || '—'}`
        : (sub.student_name || '—');

    const filesHtml = (sub.files || []).map(f =>
        `<a class="link" href="../backend/${f.file_path}" target="_blank">${f.file_name}</a>`
    ).join('<br>') || '<span class="empty-state">Sin archivos</span>';

    const submittedAt = new Date(sub.submitted_at).toLocaleString('es-ES', { dateStyle: 'medium', timeStyle: 'short' });

    const row = document.createElement('tr');
    row.innerHTML = `
        <td>${submitterName}</td>
        <td>${filesHtml}</td>
        <td>${submittedAt}</td>
        <td><span class="badge" data-signature-badge>Verificando...</span></td>
    `;
    list.appendChild(row);

    // Trigger re-verification on view (RF-19) and reflect the freshly verified state.
    verifySignature(sub.id, row.querySelector('[data-signature-badge]'));
}

async function verifySignature(submissionId, badgeEl) {
    try {
        const res  = await fetch(`${API}/submissions/${submissionId}`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const data = await res.json();

        if (!data.success) {
            badgeEl.textContent = 'Desconocido';
            return;
        }

        const valid = !!data.data.signature_valid;
        badgeEl.textContent = valid ? 'Válida' : 'Alterada';
        badgeEl.className   = `badge ${valid ? 'badge-valid' : 'badge-invalid'}`;

    } catch (err) {
        badgeEl.textContent = 'Desconocido';
    }
}
