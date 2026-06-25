// activities.js — lists all activities grouped by course
// Requires config.js and app.js to be loaded first.

loadAllActivities();

async function loadAllActivities() {
    const container = document.getElementById('activities-container');

    try {
        // First fetch all courses the user belongs to
        const coursesRes  = await fetch(`${API}/courses`, {
            headers: { 'Authorization': `Bearer ${_token}` },
        });
        const coursesData = await coursesRes.json();

        if (!coursesData.success || coursesData.data.length === 0) {
            container.innerHTML = '<p class="empty-state">No cursos disponibles.</p>';
            return;
        }

        container.innerHTML = '';

        // For each course fetch its activities
        for (const course of coursesData.data) {
            const actRes  = await fetch(`${API}/courses/${course.id}/activities`, {
                headers: { 'Authorization': `Bearer ${_token}` },
            });
            const actData = await actRes.json();

            // Skip courses with no activities
            if (!actData.success || actData.data.length === 0) continue;

            const group = document.createElement('div');
            group.className = 'activities-group';
            group.innerHTML = `<p class="activities-group-title">${course.name}</p>`;

            actData.data.forEach(activity => {
                const row = document.createElement('a');
                row.className = 'activity-list-row';
                row.href      = `activity.html?id=${activity.id}`;
                row.innerHTML = `
                    <h4>${activity.title} <span class="badge badge-valid">${activity.submission_count} submission${activity.submission_count == 1 ? '' : 's'}</span></h4>
                    <span class="deadline">Due: ${new Date(activity.deadline).toLocaleDateString('en-US', { dateStyle: 'medium' })}</span>
                `;
                group.appendChild(row);
            });

            container.appendChild(group);
        }

        if (container.children.length === 0) {
            container.innerHTML = '<p class="empty-state">No hay actividades en los cursos disponibles.</p>';
        }

    } catch (err) {
        container.innerHTML = '<p class="error-msg">Error cargando las actividades.</p>';
    }
}
